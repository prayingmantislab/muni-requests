using RA_MuniAnnouncements.Api.Models;

namespace RA_MuniAnnouncements.Api.Repositories;

/// <summary>מימוש דמה של הקטלוג. המימוש האמיתי ישתמש ב-IMongoCollection.</summary>
public class MockServiceCatalogRepository : IServiceCatalogRepository
{
    private static readonly IReadOnlyDictionary<string, ServiceDefinition> Catalog =
        new Dictionary<string, ServiceDefinition>
        {
            ["ARNONA_DISCOUNT"] = new()
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
                        SlaDays = 30,
                    },
                },
            },
            ["PARKING_PERMIT"] = new()
            {
                FormCode = "PARKING_PERMIT",
                ServiceName = "תו חניה",
                Authorities = new Dictionary<string, AuthorityDefinition>
                {
                    ["6500"] = new()
                    {
                        AuthorityName = "עיריית חדרה",
                        StatusMap = new Dictionary<string, string>
                        {
                            ["1"] = "התקבלה", ["2"] = "בטיפול", ["3"] = "נדרש תשלום", ["4"] = "הונפק",
                        },
                        ActionRequiredStatuses = ["3"],
                        FinalStatuses = ["4"],
                        SlaDays = 14,
                    },
                },
            },
        };

    public Task<IReadOnlyDictionary<string, ServiceDefinition>> GetByFormCodesAsync(
        IEnumerable<string> formCodes, CancellationToken ct = default)
    {
        var result = formCodes.Distinct()
            .Where(Catalog.ContainsKey)
            .ToDictionary(code => code, code => Catalog[code]);
        return Task.FromResult<IReadOnlyDictionary<string, ServiceDefinition>>(result);
    }
}
