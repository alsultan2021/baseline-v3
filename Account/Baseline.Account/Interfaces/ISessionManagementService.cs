namespace Baseline.Account;

/// <summary>
/// Service for managing user sessions and connected devices.
/// </summary>
public interface ISessionManagementService
{
    /// <summary>
    /// Gets active sessions for the current user.
    /// </summary>
    Task<IReadOnlyList<UserSession>> GetActiveSessionsAsync(int memberId);

    /// <summary>
    /// Gets recent login history for the current user.
    /// </summary>
    Task<IReadOnlyList<LoginHistoryEntry>> GetLoginHistoryAsync(int memberId, int count = 10);

    /// <summary>
    /// Gets linked external accounts for the current user.
    /// </summary>
    Task<IReadOnlyList<LinkedAccount>> GetLinkedAccountsAsync(int memberId);

    /// <summary>
    /// Revokes a specific session by its fingerprint/ID.
    /// </summary>
    Task<RevokeSessionResult> RevokeSessionAsync(int memberId, string sessionId);

    /// <summary>
    /// Revokes all sessions except the current one.
    /// </summary>
    Task<RevokeSessionResult> RevokeAllOtherSessionsAsync(int memberId, string? currentSessionId);

    /// <summary>
    /// Gets the current session identifier from the request context.
    /// </summary>
    string GetCurrentSessionId();
}

/// <summary>
/// Represents an active user session.
/// </summary>
public record UserSession
{
    /// <summary>Unique session identifier (device fingerprint).</summary>
    public required string SessionId { get; init; }

    /// <summary>Device type (Desktop, Mobile, Tablet).</summary>
    public string DeviceType { get; init; } = "Unknown";

    /// <summary>Browser name and version.</summary>
    public string Browser { get; init; } = "Unknown";

    /// <summary>Operating system.</summary>
    public string OperatingSystem { get; init; } = "Unknown";

    /// <summary>IP address of the session.</summary>
    public string IpAddress { get; init; } = string.Empty;

    /// <summary>Geographic location if available.</summary>
    public string? Location { get; init; }

    /// <summary>When this session was first created.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Last activity time.</summary>
    public DateTime LastActivityAt { get; init; }

    /// <summary>Whether this is the current session.</summary>
    public bool IsCurrent { get; init; }
}

/// <summary>
/// Represents a login history entry.
/// </summary>
public record LoginHistoryEntry
{
    /// <summary>When the login occurred.</summary>
    public DateTime Timestamp { get; init; }

    /// <summary>Whether the login was successful.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>Type of action (LoginSuccess, LoginFailed, etc.).</summary>
    public string ActionType { get; init; } = string.Empty;

    /// <summary>Device type used.</summary>
    public string DeviceType { get; init; } = "Unknown";

    /// <summary>Browser used.</summary>
    public string Browser { get; init; } = "Unknown";

    /// <summary>Operating system.</summary>
    public string OperatingSystem { get; init; } = "Unknown";

    /// <summary>IP address.</summary>
    public string IpAddress { get; init; } = string.Empty;

    /// <summary>Geographic location if available.</summary>
    public string? Location { get; init; }

    /// <summary>Whether this was from a new device.</summary>
    public bool IsNewDevice { get; init; }

    /// <summary>Failure reason if applicable.</summary>
    public string? FailureReason { get; init; }
}

/// <summary>
/// Represents a linked external account.
/// </summary>
public record LinkedAccount
{
    /// <summary>Provider name (Google, Microsoft, Facebook).</summary>
    public required string Provider { get; init; }

    /// <summary>Display name for the provider.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Email associated with the external account.</summary>
    public string? Email { get; init; }

    /// <summary>Provider-specific user ID.</summary>
    public string ProviderKey { get; init; } = string.Empty;

    /// <summary>When the account was linked.</summary>
    public DateTime? LinkedAt { get; init; }

    /// <summary>Icon class for display (e.g., "fab fa-google").</summary>
    public string IconClass { get; init; } = string.Empty;

    /// <summary>Brand color for display.</summary>
    public string BrandColor { get; init; } = "#333";
}

/// <summary>
/// Result of a session revocation operation.
/// </summary>
public record RevokeSessionResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int RevokedCount { get; init; }

    public static RevokeSessionResult Succeeded(int count = 1) => new() { Success = true, RevokedCount = count };
    public static RevokeSessionResult Failed(string message) => new() { Success = false, ErrorMessage = message };
}
