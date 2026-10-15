# RA_MuniAnnouncements: "הבקשות שלי"

מיקרו-שירות (.NET 8) + רכיב Angular שמציגים לתושב, באזור האישי, את כל הבקשות שהגיש לרשויות השונות בתצוגה אחודה:
שמות קריאים, סטטוס עדכני, סימון "נדרשת פעולה" וסימון "חריגה מזמן הטיפול".

## ארכיטקטורה

```
 אזור אישי (Angular)                    מיקרו-שירות RA_MuniAnnouncements (.NET 8)
┌───────────────────┐  GET /api/my-requests   ┌──────────────────────────────────────────────┐
│ <app-my-requests> │ ──── Bearer JWT ──────▶ │ Controller   (HTTP + זהות מהטוקן בלבד)       │
└───────────────────┘                         │     │                                         │
                                              │ Service      (נרמול, SLA, מיון: הלוגיקה)      │
                                              │     ├──▶ IFormsClient             ──▶ מערכת הטפסים (חיצוני)
                                              │     └──▶ IServiceCatalogRepository ──▶ MongoDB: ServiceCatalog
                                              └──────────────────────────────────────────────┘
```

| שכבה | תיקייה | אחריות |
|---|---|---|
| Controller | `Controllers/` | HTTP בלבד: שולף `idNumber` מה-Claim ב-JWT וקורא ל-Service. אין פרמטר ת"ז ב-URL. |
| Service | `Services/` | הלוגיקה העסקית: נרמול, `actionRequired`, `isOverdue`, מיון. אינו מכיר HTTP או MongoDB. |
| Client / Repository | `Clients/`, `Repositories/` | גישה למערכות חיצוניות מאחורי ממשקים. כרגע מימושי Mock עם נתונים קשיחים. |
| Models | `Models/` | `FormRequest` (פנימי, כולל ת"ז) נפרד מ-`MyRequestDto` (חיצוני, **בלי ת"ז**). |

### Dependency Injection והחלפת Mock במימוש אמיתי
ה-Service מקבל `IFormsClient`, `IServiceCatalogRepository` ו-`TimeProvider` בבנאי. החיווט ב-`Program.cs`:

```csharp
builder.Services.AddScoped<IFormsClient, MockFormsClient>();                      // ← HttpFormsClient
builder.Services.AddScoped<IServiceCatalogRepository, MockServiceCatalogRepository>(); // ← MongoServiceCatalogRepository
```

החלפת מימוש היא שינוי של שורה אחת, בלי נגיעה בלוגיקה.

### פרטיות
- הת"ז נלקחת מהטוקן בלבד, ולעולם לא חוזרת ללקוח (המבנה של ה-DTO לא מאפשר זאת).
- בלוגים מופיעים רק `requestId` ו-`formCode`. יש בדיקה שמוודאת זאת.

## הרצה

דרישות: .NET SDK 8+, Node 22+, pnpm או npm.

```bash
# 1. Backend (פורט 5115)
dotnet run --project src/RA_MuniAnnouncements.Api --launch-profile http
#    Swagger: http://localhost:5115/swagger
#    טוקן לפיתוח: POST /dev/token?idNumber=012345678  (קיים במצב Development בלבד)

# 2. Frontend (ה-proxy מעביר /api ו-/dev ל-5115)
cd client
pnpm install        # או: npm install
pnpm exec ng serve --port 4301
#    http://localhost:4301
```

## בדיקות

```bash
dotnet test                                  # 9 בדיקות יחידה ללוגיקת הנרמול (xUnit + Moq, שעון מקובע)
cd client && pnpm exec ng test --watch=false # 5 בדיקות Angular
```

הבדיקות כוללות: סטטוס לא מוכר, חריגה מ-SLA, בקשה סגורה שאינה חורגת, טופס שלא בקטלוג, מיון,
והבטחה שהת"ז לא מופיעה בתגובה או בלוגים.

## החלטות והנחות עיקריות

| נושא | הנחה |
|---|---|
| SLA | ימים קלנדריים מ-`submittedAt`; בקשה בסטטוס סופי אינה חורגת |
| סטטוס לא מוכר | מוצג "בטיפול", נכתב ללוג, נחשב פתוח, `actionRequired=false` |
| טופס/רשות שלא בקטלוג | הבקשה לא מוחזרת ונכתבת ללוג |
| תאריכים | נשמרים כ-UTC, מוצגים `dd/MM/yyyy` בשעון ישראל |
| קריאה לפעולה | הרכיב משדר אירוע `actionRequested`; האזור האישי קובע לאן לנווט |
| טוקן | JWT Bearer. בייצור יש להחליף את המפתח הסימטרי ב-`Authority` של האזור האישי |

## מה שלא כלול (בכוונה)
- פנייה אמיתית למערכת הטפסים ול-MongoDB (Mock לפי הנחיות המבחן).
- Timeout / Retry / Circuit Breaker ו-Cache (מתוארים בחלק ג' במסמך הנלווה).
- עדכונים בזמן אמת משדירת המידע (גרסה הבאה).

## שימוש בכלי בינה מלאכותית
Claude Code שימש ללימוד ולליווי: בניית שלד הפרויקט, הלוגיקה, הבדיקות ורכיב ה-Angular, וכן ניסוח חלקים ב'-ג'
במסמך הנלווה. הקוד נסקר ונבדק, ואני מוכן להסביר כל חלק בו.
