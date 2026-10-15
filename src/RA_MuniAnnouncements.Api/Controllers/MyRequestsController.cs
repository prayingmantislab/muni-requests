using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RA_MuniAnnouncements.Api.Models;
using RA_MuniAnnouncements.Api.Services;

namespace RA_MuniAnnouncements.Api.Controllers;

/// <summary>שכבת HTTP בלבד: מזהה את המשתמש מהטוקן וקורא ל-Service. אין כאן לוגיקה עסקית.</summary>
[ApiController]
[Authorize]
[Route("api/my-requests")]
public class MyRequestsController : ControllerBase
{
    /// <summary>שם ה-Claim שהאזור האישי מכניס לטוקן.</summary>
    public const string IdNumberClaim = "idNumber";

    private readonly IMyRequestsService _service;

    public MyRequestsController(IMyRequestsService service) => _service = service;

    [HttpGet]
    [ProducesResponseType<MyRequestsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<MyRequestsResponse>> Get(CancellationToken ct)
    {
        // הזהות נלקחת מהטוקן בלבד: אין פרמטר ת"ז ב-URL (דרישה 1).
        var idNumber = User.FindFirstValue(IdNumberClaim);
        if (string.IsNullOrWhiteSpace(idNumber))
            return Unauthorized(); // הנחה #8

        return Ok(await _service.GetMyRequestsAsync(idNumber, ct));
    }
}
