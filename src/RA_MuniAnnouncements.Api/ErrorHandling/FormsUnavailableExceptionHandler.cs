using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RA_MuniAnnouncements.Api.Exceptions;

namespace RA_MuniAnnouncements.Api.ErrorHandling;

/// <summary>
/// ממפה כשל במערכת הטפסים ל-503 בפורמט ProblemDetails.
/// בכוונה לא מחזירים רשימה ריקה: התושב היה חושב שאין לו בקשות.
/// הלוג כולל רק את סוג החריגה ולא את ההודעה המלאה, כדי לא לדלוף מידע אישי.
/// </summary>
public class FormsUnavailableExceptionHandler : IExceptionHandler
{
    private readonly ILogger<FormsUnavailableExceptionHandler> _logger;

    public FormsUnavailableExceptionHandler(ILogger<FormsUnavailableExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        // false = "לא שלי": החריגה עוברת לטיפול הגנרי (500).
        if (exception is not FormsServiceUnavailableException)
            return false;

        _logger.LogWarning("Forms service unavailable ({Cause})", exception.InnerException?.GetType().Name ?? "unknown");

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.RetryAfter = "30"; // רמז ללקוח מתי לנסות שוב
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Title = "השירות אינו זמין זמנית",
            Detail = "לא ניתן לאחזר את הבקשות כרגע. נסו שוב בעוד מספר רגעים.",
        }, ct);
        return true;
    }
}
