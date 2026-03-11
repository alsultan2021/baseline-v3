namespace Baseline.Account;

/// <summary>
/// Service for the device authorization flow (Netflix / Google TV-style sign-in).
/// An unauthenticated device requests a short code; an authenticated user approves it.
/// </summary>
public interface IDeviceAuthorizationService
{
    /// <summary>
    /// Create a new device authorization request and return the user-facing code + device code.
    /// </summary>
    Task<DeviceAuthorizationResult> CreateAuthorizationRequestAsync();

    /// <summary>
    /// Poll for authorization status using the device code (called by the unauthenticated device).
    /// </summary>
    Task<DeviceAuthorizationPollResult> PollAuthorizationAsync(string deviceCode);

    /// <summary>
    /// Look up a pending authorization by user code (called by the authenticated user).
    /// </summary>
    Task<DeviceAuthorizationInfo?> GetPendingByUserCodeAsync(string userCode);

    /// <summary>
    /// Approve a pending authorization (called by the authenticated user).
    /// </summary>
    Task<bool> ApproveAuthorizationAsync(string userCode, int memberId);

    /// <summary>
    /// Deny a pending authorization (called by the authenticated user).
    /// </summary>
    Task<bool> DenyAuthorizationAsync(string userCode);

    /// <summary>
    /// Clean up expired authorization requests.
    /// </summary>
    Task CleanupExpiredAsync();
}

/// <summary>
/// Result of creating a device authorization request.
/// </summary>
public record DeviceAuthorizationResult
{
    /// <summary>Short code shown to user, e.g. "ABCD-1234".</summary>
    public required string UserCode { get; init; }

    /// <summary>Secret code the device uses to poll for approval.</summary>
    public required string DeviceCode { get; init; }

    /// <summary>URL the user should visit to enter the code.</summary>
    public required string VerificationUrl { get; init; }

    /// <summary>URL with the code pre-filled (for QR codes).</summary>
    public required string VerificationUrlComplete { get; init; }

    /// <summary>How often the device should poll (seconds).</summary>
    public int IntervalSeconds { get; init; } = 5;

    /// <summary>When the code expires.</summary>
    public required DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Result of polling for device authorization status.
/// </summary>
public record DeviceAuthorizationPollResult
{
    /// <summary>Whether authorization was granted.</summary>
    public bool Authorized { get; init; }

    /// <summary>Whether the code is still pending (keep polling).</summary>
    public bool Pending { get; init; }

    /// <summary>Whether the code was denied or expired.</summary>
    public bool Denied { get; init; }

    /// <summary>Whether the code has expired.</summary>
    public bool Expired { get; init; }

    /// <summary>Error message if applicable.</summary>
    public string? Error { get; init; }

    /// <summary>MemberID of the user who approved (only when Authorized=true).</summary>
    public int? ApprovedByMemberID { get; init; }

    /// <summary>Username of the approving user (only when Authorized=true).</summary>
    public string? ApprovedByUsername { get; init; }
}
