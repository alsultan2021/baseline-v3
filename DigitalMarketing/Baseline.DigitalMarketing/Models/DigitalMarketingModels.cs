namespace Baseline.DigitalMarketing.Models;

/// <summary>
/// Represents information about a custom activity.
/// </summary>
public record CustomActivity(
    string ActivityType,
    string Title,
    string? Value = null,
    IDictionary<string, string>? AdditionalData = null,
    DateTime? Timestamp = null
);

/// <summary>
/// Represents a personalization condition.
/// </summary>
public record PersonalizationCondition(
    string ConditionType,
    string DisplayName,
    IDictionary<string, object>? Parameters = null
);

/// <summary>
/// Represents a personalization variant.
/// </summary>
public record PersonalizationVariant(
    string VariantKey,
    string DisplayName,
    PersonalizationCondition Condition,
    int Priority = 0
);

/// <summary>
/// Result of a personalization evaluation.
/// </summary>
public record PersonalizationResult(
    bool IsPersonalized,
    string? MatchedVariantKey,
    string? MatchedConditionType
);

/// <summary>
/// Contact summary information.
/// </summary>
public record ContactSummary(
    int ContactId,
    string? Email,
    string? FirstName,
    string? LastName,
    DateTime? LastActive,
    IEnumerable<string> ContactGroups
);

/// <summary>
/// Activity log entry.
/// </summary>
public record ActivityLogEntry(
    int ActivityId,
    string ActivityType,
    string Title,
    string? Value,
    DateTime Timestamp,
    string? Url,
    int? ContactId
);

/// <summary>
/// Contact group summary.
/// </summary>
public record ContactGroupSummary(
    int ContactGroupId,
    string CodeName,
    string DisplayName,
    string? Description,
    int MemberCount,
    bool IsDynamic
);

/// <summary>
/// Represents a visitor tracking context.
/// </summary>
public record TrackingContext(
    bool IsTrackingEnabled,
    bool HasConsent,
    int? ContactId,
    string? SessionId,
    string? VisitorId
);

/// <summary>
/// Configuration for a custom activity type registration.
/// </summary>
public record CustomActivityTypeRegistration(
    string CodeName,
    string DisplayName,
    string? Description = null,
    bool TrackValue = false,
    bool EnableLogging = true
);
