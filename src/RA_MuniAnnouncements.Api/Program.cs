using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RA_MuniAnnouncements.Api.Clients;
using RA_MuniAnnouncements.Api.Controllers;
using RA_MuniAnnouncements.Api.ErrorHandling;
using RA_MuniAnnouncements.Api.Repositories;
using RA_MuniAnnouncements.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails(); // שגיאות בפורמט אחיד, בלי חשיפת stack trace
builder.Services.AddExceptionHandler<FormsUnavailableExceptionHandler>(); // כשל בטפסים ← 503 (סדר ההרשמה = סדר הניסיון)

// ---------- Dependency Injection ----------
// כדי לעבור למימוש אמיתי מחליפים רק את השורות האלה (ראה README).
builder.Services.AddScoped<IFormsClient, MockFormsClient>();
builder.Services.AddScoped<IServiceCatalogRepository, MockServiceCatalogRepository>();
builder.Services.AddScoped<IMyRequestsService, MyRequestsService>();
builder.Services.AddSingleton(TimeProvider.System); // שעון מוזרק (בבדיקות: שעון מקובע)

// ---------- Authentication (JWT) ----------
// בייצור: Authority של האזור האישי. כאן: מפתח סימטרי מהקונפיגורציה (פיתוח בלבד).
var jwt = builder.Configuration.GetSection("Jwt");
var signingKey = jwt["SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");
var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = jwt["Issuer"],
            ValidateAudience = true, ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true, IssuerSigningKey = securityKey,
            ValidateLifetime = true,
        };
    });
builder.Services.AddAuthorization();

// ---------- CORS (פיתוח: Angular על 4200) ----------
builder.Services.AddCors(o => o.AddPolicy("Angular", p =>
    p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

// ---------- Swagger עם כפתור Authorize ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        Description = "הדבק טוקן שהתקבל מ-POST /dev/token",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
    });
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("Angular");

    // מחולל טוקן לפיתוח בלבד: מחליף את האזור האישי שאין לנו כאן. לא קיים בייצור.
    app.MapPost("/dev/token", (string idNumber) =>
    {
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"], audience: jwt["Audience"],
            claims: [new Claim(MyRequestsController.IdNumberClaim, idNumber)],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256));
        return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    }).AllowAnonymous();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

// נדרש כדי שפרויקט הבדיקות יוכל להשתמש ב-WebApplicationFactory בעתיד
public partial class Program { }
