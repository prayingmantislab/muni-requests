namespace RA_MuniAnnouncements.Api.Models;

/// <summary>
/// בקשה "גולמית" כפי שהיא חוזרת ממערכת הטפסים (ממשק חיצוני).
/// מכילה קודים בלבד. מודל פנימי: לעולם לא מוחזר ללקוח (כי יש בו ApplicantId = ת"ז).
/// </summary>
public record FormRequest
{
    public required string RequestId { get; init; }
    public required string FormCode { get; init; }
    public required string AuthorityId { get; init; }
    public required string ApplicantId { get; init; }
    public required string StatusCode { get; init; }
    public required DateTimeOffset SubmittedAt { get; init; }
    public required DateTimeOffset LastUpdate { get; init; }
}
