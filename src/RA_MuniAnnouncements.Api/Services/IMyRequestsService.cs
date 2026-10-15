using RA_MuniAnnouncements.Api.Models;

namespace RA_MuniAnnouncements.Api.Services;

public interface IMyRequestsService
{
    /// <summary>מחזיר את כל בקשות התושב, מנורמלות וממוינות.</summary>
    Task<MyRequestsResponse> GetMyRequestsAsync(string idNumber, CancellationToken ct = default);
}
