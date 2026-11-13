using RA_MuniAnnouncements.Api.Models;

namespace RA_MuniAnnouncements.Api.Clients;

/// <summary>מימוש דמה: נתונים קשיחים שמכסים את המקרים המעניינים של הלוגיקה.</summary>
public class MockFormsClient : IFormsClient
{
    public Task<IReadOnlyList<FormRequest>> GetRequestsByIdNumberAsync(string idNumber, CancellationToken ct = default)
    {
        // המוק מחזיר אותן בקשות לכל ת"ז. במימוש האמיתי ה-API יסנן לפי ת"ז.
        IReadOnlyList<FormRequest> data =
        [
            // דורשת פעולה (סטטוס 3), הוגשה לפני יותר מ-30 יום ← גם חורגת
            Make("F-88213", "ARNONA_DISCOUNT", "6500", "3", "2026-09-02T10:15:00Z", "2026-09-20T08:00:00Z", idNumber),
            // סטטוס סופי (אושרה): אסור שתסומן כחורגת
            Make("F-88100", "ARNONA_DISCOUNT", "6500", "5", "2026-06-01T09:00:00Z", "2026-06-20T09:00:00Z", idNumber),
            // בטיפול, הוגשה לאחרונה: לא חורגת
            Make("F-88400", "PARKING_PERMIT", "6500", "2", "2026-10-05T09:00:00Z", "2026-10-10T12:00:00Z", idNumber),
            // קוד סטטוס לא מוכר ← "בטיפול" + לוג
            Make("F-88500", "PARKING_PERMIT", "6500", "99", "2026-10-01T09:00:00Z", "2026-10-12T12:00:00Z", idNumber),
            // קוד טופס שלא בקטלוג ← לא מוחזרת + לוג
            Make("F-88600", "UNKNOWN_FORM", "6500", "1", "2026-10-01T09:00:00Z", "2026-10-02T12:00:00Z", idNumber),
        ];
        return Task.FromResult(data);
    }

    private static FormRequest Make(string id, string form, string authority, string status,
        string submitted, string updated, string applicantId) => new()
    {
        RequestId = id,
        FormCode = form,
        AuthorityId = authority,
        ApplicantId = applicantId,
        StatusCode = status,
        SubmittedAt = DateTimeOffset.Parse(submitted),
        LastUpdate = DateTimeOffset.Parse(updated),
    };
}
