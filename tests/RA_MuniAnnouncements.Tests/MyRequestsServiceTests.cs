using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using Moq;
using RA_MuniAnnouncements.Api.Clients;
using RA_MuniAnnouncements.Api.Models;
using RA_MuniAnnouncements.Api.Repositories;
using RA_MuniAnnouncements.Api.Services;

namespace RA_MuniAnnouncements.Tests;

/// <summary>
/// בדיקות יחידה ללוגיקת הנרמול. התלויות החיצוניות (טפסים, קטלוג) הן Mock,
/// והשעון מקובע, ולכן התוצאות דטרמיניסטיות ואינן תלויות בתאריך הריצה.
/// </summary>
public class MyRequestsServiceTests
{
    private const string IdNumber = "012345678";

    // "עכשיו" קבוע לכל הבדיקות.
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IFormsClient> _forms = new();
    private readonly Mock<IServiceCatalogRepository> _catalog = new();
    private readonly FakeLogger<MyRequestsService> _logger = new();

    private MyRequestsService CreateSut(params ServiceDefinition[] catalog)
    {
        _catalog
            .Setup(c => c.GetByFormCodesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(catalog.ToDictionary(s => s.FormCode) as IReadOnlyDictionary<string, ServiceDefinition>);

        var clock = new FakeTimeProvider(Now);
        return new MyRequestsService(_forms.Object, _catalog.Object, clock, _logger);
    }

    private void GivenRequests(params FormRequest[] requests) =>
        _forms
            .Setup(f => f.GetRequestsByIdNumberAsync(IdNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(requests);

    // ---------- בניית נתוני בדיקה ----------

    private static ServiceDefinition ArnonaCatalog(int slaDays = 30) => new()
    {
        FormCode = "ARNONA_DISCOUNT",
        ServiceName = "הנחה בארנונה",
        Authorities = new Dictionary<string, AuthorityDefinition>
        {
            ["6500"] = new()
            {
                AuthorityName = "עיריית חדרה",
                StatusMap = new Dictionary<string, string>
                {
                    ["1"] = "התקבלה", ["2"] = "בטיפול", ["3"] = "נדרשים מסמכים",
                    ["4"] = "נדחתה", ["5"] = "אושרה",
                },
                ActionRequiredStatuses = ["3"],
                FinalStatuses = ["4", "5"],
                SlaDays = slaDays,
            },
        },
    };

    private static FormRequest Request(
        string id = "F-1", string status = "2", string form = "ARNONA_DISCOUNT",
        string authority = "6500", DateTimeOffset? submitted = null, DateTimeOffset? updated = null) => new()
    {
        RequestId = id,
        FormCode = form,
        AuthorityId = authority,
        ApplicantId = IdNumber,
        StatusCode = status,
        SubmittedAt = submitted ?? Now.AddDays(-1),
        LastUpdate = updated ?? Now.AddDays(-1),
    };

    // ---------- הבדיקות ----------

    [Fact]
    public async Task Normalizes_codes_to_names_and_formats_dates()
    {
        GivenRequests(Request(status: "3",
            submitted: new DateTimeOffset(2026, 9, 2, 10, 15, 0, TimeSpan.Zero),
            updated: new DateTimeOffset(2026, 9, 20, 8, 0, 0, TimeSpan.Zero)));
        var sut = CreateSut(ArnonaCatalog());

        var result = await sut.GetMyRequestsAsync(IdNumber);

        var dto = Assert.Single(result.Requests);
        Assert.Equal("הנחה בארנונה", dto.ServiceName);
        Assert.Equal("עיריית חדרה", dto.AuthorityName);
        Assert.Equal("נדרשים מסמכים", dto.Status);
        Assert.Equal("02/09/2026", dto.SubmittedAt);
        Assert.Equal("20/09/2026", dto.LastUpdate);
    }

    [Fact]
    public async Task Marks_action_required_only_for_configured_statuses()
    {
        GivenRequests(Request(id: "needs-action", status: "3"), Request(id: "in-progress", status: "2"));
        var sut = CreateSut(ArnonaCatalog());

        var result = await sut.GetMyRequestsAsync(IdNumber);

        Assert.True(result.Requests.Single(r => r.RequestId == "needs-action").ActionRequired);
        Assert.False(result.Requests.Single(r => r.RequestId == "in-progress").ActionRequired);
    }

    [Fact]
    public async Task Open_request_past_SLA_is_overdue()
    {
        // SLA = 30 ימים, ההגשה לפני 31 ימים, והסטטוס "בטיפול" (פתוח).
        GivenRequests(Request(status: "2", submitted: Now.AddDays(-31)));
        var sut = CreateSut(ArnonaCatalog(slaDays: 30));

        var result = await sut.GetMyRequestsAsync(IdNumber);

        Assert.True(Assert.Single(result.Requests).IsOverdue);
    }

    [Fact]
    public async Task Request_within_SLA_is_not_overdue()
    {
        GivenRequests(Request(status: "2", submitted: Now.AddDays(-29)));
        var sut = CreateSut(ArnonaCatalog(slaDays: 30));

        var result = await sut.GetMyRequestsAsync(IdNumber);

        Assert.False(Assert.Single(result.Requests).IsOverdue);
    }

    [Fact]
    public async Task Final_status_is_never_overdue_even_past_SLA()
    {
        // "אושרה" (5) היא סטטוס סופי: בקשה סגורה אינה יכולה לחרוג.
        GivenRequests(Request(status: "5", submitted: Now.AddDays(-200)));
        var sut = CreateSut(ArnonaCatalog(slaDays: 30));

        var result = await sut.GetMyRequestsAsync(IdNumber);

        Assert.False(Assert.Single(result.Requests).IsOverdue);
    }

    [Fact]
    public async Task Unknown_status_is_shown_as_in_progress_and_logged()
    {
        GivenRequests(Request(id: "F-99", status: "99"));
        var sut = CreateSut(ArnonaCatalog());

        var result = await sut.GetMyRequestsAsync(IdNumber);

        var dto = Assert.Single(result.Requests);
        Assert.Equal("בטיפול", dto.Status);
        Assert.False(dto.ActionRequired);
        Assert.Contains(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning && r.Message.Contains("F-99"));
    }

    [Fact]
    public async Task Request_with_form_code_missing_from_catalog_is_dropped_and_logged()
    {
        GivenRequests(Request(id: "ok"), Request(id: "orphan", form: "UNKNOWN_FORM"));
        var sut = CreateSut(ArnonaCatalog());

        var result = await sut.GetMyRequestsAsync(IdNumber);

        Assert.Equal("ok", Assert.Single(result.Requests).RequestId);
        Assert.Contains(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning && r.Message.Contains("orphan"));
    }

    [Fact]
    public async Task Sorts_action_required_first_then_by_last_update_descending()
    {
        GivenRequests(
            Request(id: "old",        status: "2", updated: Now.AddDays(-10)),
            Request(id: "new",        status: "2", updated: Now.AddDays(-1)),
            Request(id: "action-old", status: "3", updated: Now.AddDays(-20)));
        var sut = CreateSut(ArnonaCatalog());

        var result = await sut.GetMyRequestsAsync(IdNumber);

        Assert.Equal(["action-old", "new", "old"], result.Requests.Select(r => r.RequestId));
    }

    [Fact]
    public async Task Response_and_logs_never_contain_the_id_number()
    {
        GivenRequests(Request(id: "F-99", status: "99"), Request(id: "orphan", form: "UNKNOWN_FORM"));
        var sut = CreateSut(ArnonaCatalog());

        var result = await sut.GetMyRequestsAsync(IdNumber);

        var json = System.Text.Json.JsonSerializer.Serialize(result);
        Assert.DoesNotContain(IdNumber, json);
        Assert.All(_logger.Collector.GetSnapshot(), r => Assert.DoesNotContain(IdNumber, r.Message));
    }
}
