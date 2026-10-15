using RA_MuniAnnouncements.Api.Models;

namespace RA_MuniAnnouncements.Api.Clients;

/// <summary>
/// חוזה מול מערכת הטפסים (שירות חיצוני). ה-Service תלוי בממשק בלבד,
/// כך שאפשר להחליף Mock במימוש HTTP אמיתי בלי לשנות את הלוגיקה.
/// </summary>
public interface IFormsClient
{
    Task<IReadOnlyList<FormRequest>> GetRequestsByIdNumberAsync(string idNumber, CancellationToken ct = default);
}
