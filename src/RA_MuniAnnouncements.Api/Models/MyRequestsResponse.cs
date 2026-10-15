namespace RA_MuniAnnouncements.Api.Models;

/// <summary>האובייקט שחוזר ללקוח. שים לב: אין כאן שדה ת"ז.</summary>
public record MyRequestsResponse(IReadOnlyList<MyRequestDto> Requests);

/// <summary>בקשה מנורמלת: שמות קריאים במקום קודים, ותאריכים בפורמט dd/MM/yyyy.</summary>
public record MyRequestDto
{
    public required string RequestId { get; init; }
    public required string ServiceName { get; init; }
    public required string AuthorityName { get; init; }
    public required string Status { get; init; }
    public required bool ActionRequired { get; init; }
    public required bool IsOverdue { get; init; }
    public required string SubmittedAt { get; init; }
    public required string LastUpdate { get; init; }
}
