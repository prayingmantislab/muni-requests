namespace RA_MuniAnnouncements.Api.Models;

/// <summary>מסמך אחד באוסף ServiceCatalog (MongoDB): הגדרת שירות לפי קוד טופס.</summary>
public record ServiceDefinition
{
    public required string FormCode { get; init; }
    public required string ServiceName { get; init; }

    /// <summary>מפתח = authorityId. כל רשות מגדירה סטטוסים ו-SLA בעצמה.</summary>
    public required IReadOnlyDictionary<string, AuthorityDefinition> Authorities { get; init; }
}

/// <summary>ההגדרות של רשות מקומית אחת עבור שירות מסוים.</summary>
public record AuthorityDefinition
{
    public required string AuthorityName { get; init; }

    /// <summary>קוד סטטוס ← שם סטטוס כפי שהרשות הגדירה.</summary>
    public required IReadOnlyDictionary<string, string> StatusMap { get; init; }

    /// <summary>קודי סטטוס שדורשים פעולה מהתושב.</summary>
    public required IReadOnlyCollection<string> ActionRequiredStatuses { get; init; }

    /// <summary>קודי סטטוס סופיים (הבקשה סגורה, לא יכולה להיות "חורגת").</summary>
    public required IReadOnlyCollection<string> FinalStatuses { get; init; }

    /// <summary>זמן תקן לטיפול, בימים קלנדריים (הנחה #1).</summary>
    public required int SlaDays { get; init; }
}
