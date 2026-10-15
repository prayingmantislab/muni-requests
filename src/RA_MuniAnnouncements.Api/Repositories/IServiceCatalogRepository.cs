using RA_MuniAnnouncements.Api.Models;

namespace RA_MuniAnnouncements.Api.Repositories;

/// <summary>גישה לאוסף ServiceCatalog (MongoDB).</summary>
public interface IServiceCatalogRepository
{
    /// <summary>
    /// שליפה אחת לכל קודי הטפסים הנדרשים (במקום שאילתה לכל בקשה).
    /// קוד שלא קיים בקטלוג פשוט לא יופיע במילון.
    /// </summary>
    Task<IReadOnlyDictionary<string, ServiceDefinition>> GetByFormCodesAsync(
        IEnumerable<string> formCodes, CancellationToken ct = default);
}
