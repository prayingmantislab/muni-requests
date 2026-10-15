using System.Globalization;
using Microsoft.Extensions.Logging;
using RA_MuniAnnouncements.Api.Clients;
using RA_MuniAnnouncements.Api.Models;
using RA_MuniAnnouncements.Api.Repositories;

namespace RA_MuniAnnouncements.Api.Services;

/// <summary>
/// הלוגיקה העסקית: שולף בקשות + קטלוג, מנרמל, מסנן וממיין.
/// כל התלויות מוזרקות כממשקים (כולל השעון), ולכן קל להחליף מימוש ולבדוק.
/// </summary>
public class MyRequestsService : IMyRequestsService
{
    // הנחה #4: תאריכים מוצגים בשעון ישראל.
    private static readonly TimeZoneInfo IsraelTime = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem");

    private const string DateFormat = "dd/MM/yyyy";
    private const string DefaultStatusName = "בטיפול"; // דרישה 4.2

    private readonly IFormsClient _formsClient;
    private readonly IServiceCatalogRepository _catalog;
    private readonly TimeProvider _clock;
    private readonly ILogger<MyRequestsService> _logger;

    public MyRequestsService(
        IFormsClient formsClient,
        IServiceCatalogRepository catalog,
        TimeProvider clock,
        ILogger<MyRequestsService> logger)
    {
        _formsClient = formsClient;
        _catalog = catalog;
        _clock = clock;
        _logger = logger;
    }

    public async Task<MyRequestsResponse> GetMyRequestsAsync(string idNumber, CancellationToken ct = default)
    {
        // 1. בקשות גולמיות. סינון הגנתי לפי ת"ז (הנחה #9).
        var raw = await _formsClient.GetRequestsByIdNumberAsync(idNumber, ct);
        var mine = raw.Where(r => r.ApplicantId == idNumber).ToList();

        if (mine.Count == 0)
            return new MyRequestsResponse([]);

        // 2. הקטלוג בשליפה אחת לכל קודי הטפסים הייחודיים.
        var catalog = await _catalog.GetByFormCodesAsync(mine.Select(r => r.FormCode).Distinct(), ct);

        // 3. נרמול. בקשות שלא ניתן לנרמל מדלגות החוצה (עם לוג).
        var now = _clock.GetUtcNow();
        var normalized = new List<(MyRequestDto Dto, DateTimeOffset LastUpdate)>();

        foreach (var request in mine)
        {
            if (TryNormalize(request, catalog, now, out var dto))
                normalized.Add((dto, request.LastUpdate));
        }

        // 4. מיון: דורשות פעולה קודם, ואז לפי עדכון אחרון מהחדש לישן (דרישה 4.6).
        var sorted = normalized
            .OrderByDescending(x => x.Dto.ActionRequired)
            .ThenByDescending(x => x.LastUpdate)
            .Select(x => x.Dto)
            .ToList();

        return new MyRequestsResponse(sorted);
    }

    private bool TryNormalize(
        FormRequest request,
        IReadOnlyDictionary<string, ServiceDefinition> catalog,
        DateTimeOffset now,
        out MyRequestDto dto)
    {
        dto = null!;

        // דרישה 4.5 + הנחה #3: טופס לא בקטלוג, או רשות לא מוגדרת בו ← לא מוחזרת.
        // שים לב: בלוג רק requestId ו-formCode. לעולם לא ת"ז.
        if (!catalog.TryGetValue(request.FormCode, out var service))
        {
            _logger.LogWarning("Request {RequestId} skipped: form code {FormCode} not in catalog",
                request.RequestId, request.FormCode);
            return false;
        }

        if (!service.Authorities.TryGetValue(request.AuthorityId, out var authority))
        {
            _logger.LogWarning("Request {RequestId} skipped: authority {AuthorityId} not defined for form {FormCode}",
                request.RequestId, request.AuthorityId, request.FormCode);
            return false;
        }

        // דרישה 4.2: סטטוס לא מוכר ← "בטיפול" + לוג.
        if (!authority.StatusMap.TryGetValue(request.StatusCode, out var statusName))
        {
            _logger.LogWarning("Request {RequestId}: unknown status code {StatusCode} for authority {AuthorityId}, showing default",
                request.RequestId, request.StatusCode, request.AuthorityId);
            statusName = DefaultStatusName;
        }

        // דרישה 4.3
        var actionRequired = authority.ActionRequiredStatuses.Contains(request.StatusCode);

        // דרישה 4.4: פתוחה (לא סופית) וחלף יותר מה-SLA מאז ההגשה.
        var isOpen = !authority.FinalStatuses.Contains(request.StatusCode);
        var isOverdue = isOpen && (now - request.SubmittedAt).TotalDays > authority.SlaDays;

        dto = new MyRequestDto
        {
            RequestId = request.RequestId,
            ServiceName = service.ServiceName,
            AuthorityName = authority.AuthorityName,
            Status = statusName,
            ActionRequired = actionRequired,
            IsOverdue = isOverdue,
            SubmittedAt = FormatDate(request.SubmittedAt),
            LastUpdate = FormatDate(request.LastUpdate),
        };
        return true;
    }

    private static string FormatDate(DateTimeOffset value) =>
        TimeZoneInfo.ConvertTime(value, IsraelTime).ToString(DateFormat, CultureInfo.InvariantCulture);
}
