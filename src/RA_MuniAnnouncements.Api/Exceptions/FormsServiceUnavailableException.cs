namespace RA_MuniAnnouncements.Api.Exceptions;

/// <summary>
/// מערכת הטפסים אינה זמינה (Timeout, ניתוק, 5xx או Circuit Breaker פתוח).
/// מימוש אמיתי של IFormsClient אחראי לתרגם לכאן את שגיאות ה-HTTP,
/// כך ששאר השכבות לא תלויות בפרטי התקשורת.
/// </summary>
public class FormsServiceUnavailableException : Exception
{
    public FormsServiceUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}
