namespace Baseline.Account;

/// <summary>
/// Service for managing persistent trusted devices.
/// Replaces the ephemeral ASP.NET Identity "RememberTwoFactorClient" cookie with 
/// a database-backed solution that users can view and revoke.
/// </summary>
public interface ITrustedDeviceService
{
    /// <summary>
    /// Trust the current device for a member. Sets a persistent cookie and stores the device in DB.
    /// </summary>
    Task<TrustedDeviceInfo> TrustCurrentDeviceAsync(int memberId, string? deviceName = null);

    /// <summary>
    /// Check if the current device is trusted for a member (via cookie + DB lookup).
    /// If trusted, updates LastUsedAt.
    /// </summary>
    Task<bool> IsCurrentDeviceTrustedAsync(int memberId);

    /// <summary>
    /// Get all trusted devices for a member.
    /// </summary>
    Task<IReadOnlyList<TrustedDeviceInfo>> GetTrustedDevicesAsync(int memberId);

    /// <summary>
    /// Revoke trust for a specific device.
    /// </summary>
    Task<bool> RevokeDeviceAsync(int memberId, int trustedDeviceId);

    /// <summary>
    /// Revoke trust for all devices of a member (e.g. after password change).
    /// </summary>
    Task RevokeAllDevicesAsync(int memberId);

    /// <summary>
    /// Clean up expired trusted device records.
    /// </summary>
    Task CleanupExpiredAsync();
}

/// <summary>
/// View model for displaying a trusted device to the user.
/// </summary>
public record TrustedDeviceViewModel
{
    public int TrustedDeviceID { get; init; }
    public string DeviceName { get; init; } = "";
    public string? DeviceType { get; init; }
    public string? Browser { get; init; }
    public string? OperatingSystem { get; init; }
    public string? IpAddress { get; init; }
    public DateTime TrustedAt { get; init; }
    public DateTime LastUsedAt { get; init; }
    public bool IsCurrentDevice { get; init; }
}
