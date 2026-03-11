using Microsoft.AspNetCore.Http;

namespace Baseline.Account;

/// <summary>
/// Service for logging authentication events and managing login audit trail.
/// </summary>
public interface ILoginAuditService
{
    /// <summary>
    /// Log a login attempt (success or failure).
    /// </summary>
    Task LogLoginAttemptAsync(LoginAttemptContext context);

    /// <summary>
    /// Log a logout event.
    /// </summary>
    Task LogLogoutAsync(int memberId, string username);

    /// <summary>
    /// Log a password change event.
    /// </summary>
    Task LogPasswordChangedAsync(int memberId, string username);

    /// <summary>
    /// Log a password reset request.
    /// </summary>
    Task LogPasswordResetAsync(int memberId, string username);

    /// <summary>
    /// Log a 2FA verification attempt.
    /// </summary>
    Task LogTwoFactorAttemptAsync(int memberId, string username, bool success);

    /// <summary>
    /// Get recent login history for a member.
    /// </summary>
    Task<IReadOnlyList<LoginAuditLogInfo>> GetLoginHistoryAsync(int memberId, int count = 10);

    /// <summary>
    /// Get active sessions for a member (successful logins without corresponding logout).
    /// </summary>
    Task<IReadOnlyList<LoginAuditLogInfo>> GetActiveSessionsAsync(int memberId);

    /// <summary>
    /// Check if this is a new/unknown device for the user.
    /// </summary>
    Task<bool> IsNewDeviceAsync(int memberId, string deviceFingerprint);

    /// <summary>
    /// Check if IP has suspicious activity (too many failed attempts).
    /// </summary>
    Task<bool> HasSuspiciousActivityAsync(string ipAddress, TimeSpan window, int maxFailures);

    /// <summary>
    /// Check if an IP address has successfully completed 2FA recently for a specific user.
    /// This allows skipping 2FA when switching browsers on the same network.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="ipAddress">The IP address to check.</param>
    /// <param name="trustWindow">How long an IP remains trusted after successful 2FA.</param>
    /// <returns>True if the IP has completed 2FA within the trust window.</returns>
    Task<bool> IsTwoFactorIpTrustedAsync(int memberId, string ipAddress, TimeSpan trustWindow);

    /// <summary>
    /// Gets the client IP address from the current HTTP context.
    /// </summary>
    string? GetCurrentClientIpAddress();
}

/// <summary>
/// Context for logging a login attempt.
/// </summary>
public record LoginAttemptContext
{
    /// <summary>Member ID if known (null for failed attempts with unknown user).</summary>
    public int? MemberId { get; init; }

    /// <summary>Username or email used in the attempt.</summary>
    public required string Username { get; init; }

    /// <summary>Type of login action.</summary>
    public required string ActionType { get; init; }

    /// <summary>Whether the login was successful.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>Failure reason code if failed.</summary>
    public string? FailureReason { get; init; }

    /// <summary>Whether this was from an external provider (OAuth).</summary>
    public bool IsExternalLogin { get; init; }

    /// <summary>External provider name if applicable.</summary>
    public string? ExternalProvider { get; init; }

    /// <summary>
    /// Extract login context from HTTP request.
    /// </summary>
    public static LoginAttemptContext FromRequest(
        HttpContext httpContext,
        string username,
        string actionType,
        bool isSuccess,
        int? memberId = null,
        string? failureReason = null)
    {
        return new LoginAttemptContext
        {
            MemberId = memberId,
            Username = username,
            ActionType = actionType,
            IsSuccess = isSuccess,
            FailureReason = failureReason
        };
    }
}

/// <summary>
/// Parsed device information from User-Agent.
/// </summary>
public record DeviceInfo
{
    public string DeviceType { get; init; } = "Unknown";
    public string Browser { get; init; } = "Unknown";
    public string OperatingSystem { get; init; } = "Unknown";
    public string DeviceFingerprint { get; init; } = string.Empty;
}
