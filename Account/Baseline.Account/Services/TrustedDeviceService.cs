using System.Security.Cryptography;
using CMS.Core;
using CMS.DataEngine;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UAParser;

namespace Baseline.Account;

/// <summary>
/// Persistent trusted device management. Stores device tokens in DB with a
/// long-lived cookie so users can view and revoke trusted devices.
/// </summary>
public class TrustedDeviceService(
    IHttpContextAccessor httpContextAccessor,
    IOptions<TrustedDeviceOptions> options,
    ILogger<TrustedDeviceService> logger) : ITrustedDeviceService
{
    public const string CookieName = "Baseline.TrustedDevice";
    private readonly TrustedDeviceOptions _options = options.Value;
    private static readonly Parser s_uaParser = Parser.GetDefault();

    private IInfoProvider<TrustedDeviceInfo>? _provider;
    private IInfoProvider<TrustedDeviceInfo> Provider =>
        _provider ??= Service.Resolve<IInfoProvider<TrustedDeviceInfo>>();

    private bool IsTableAvailable()
    {
        try
        {
            return DataClassInfoProvider.GetDataClassInfo(TrustedDeviceInfo.OBJECT_TYPE) != null;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<TrustedDeviceInfo> TrustCurrentDeviceAsync(int memberId, string? deviceName = null)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString() ?? "";
        var ipAddress = GetClientIpAddress(httpContext);
        var clientInfo = s_uaParser.Parse(userAgent);
        var fingerprint = GenerateFingerprint(userAgent, ipAddress);

        // Check if device already trusted (same fingerprint)
        var existing = await FindExistingTrustedDevice(memberId, fingerprint);
        if (existing != null)
        {
            // Update last used and refresh cookie
            existing.LastUsedAt = DateTime.UtcNow;
            existing.IsActive = true;
            Provider.Set(existing);
            SetDeviceCookie(existing.DeviceToken);
            return existing;
        }

        var token = GenerateDeviceToken();
        var autoName = deviceName ?? $"{clientInfo.UA.Family ?? "Unknown"} on {clientInfo.OS.Family ?? "Unknown"}";

        var device = new TrustedDeviceInfo
        {
            MemberID = memberId,
            DeviceToken = token,
            DeviceFingerprint = fingerprint,
            DeviceName = autoName.Length > 200 ? autoName[..200] : autoName,
            DeviceType = ParseDeviceType(userAgent),
            Browser = clientInfo.UA.Family ?? "Unknown",
            OperatingSystem = FormatOS(clientInfo.OS),
            IpAddress = ipAddress,
            TrustedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow,
            ExpiresAt = _options.TrustDuration.HasValue
                ? DateTime.UtcNow.Add(_options.TrustDuration.Value)
                : null,
            IsActive = true
        };

        if (IsTableAvailable())
        {
            Provider.Set(device);
        }

        SetDeviceCookie(token);

        logger.LogInformation(
            "Device trusted for member {MemberId}: {DeviceName} from {IpAddress}",
            memberId, autoName, ipAddress);

        return device;
    }

    /// <inheritdoc/>
    public async Task<bool> IsCurrentDeviceTrustedAsync(int memberId)
    {
        if (!IsTableAvailable()) return false;

        var token = GetDeviceCookieToken();
        if (string.IsNullOrEmpty(token)) return false;

        var device = (await Provider.Get()
            .WhereEquals(nameof(TrustedDeviceInfo.MemberID), memberId)
            .WhereEquals(nameof(TrustedDeviceInfo.DeviceToken), token)
            .WhereEquals(nameof(TrustedDeviceInfo.IsActive), true)
            .TopN(1)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (device == null) return false;

        // Check expiration
        if (device.ExpiresAt.HasValue && device.ExpiresAt.Value < DateTime.UtcNow)
        {
            device.IsActive = false;
            Provider.Set(device);
            ClearDeviceCookie();
            return false;
        }

        // Update last used timestamp
        device.LastUsedAt = DateTime.UtcNow;
        Provider.Set(device);

        return true;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TrustedDeviceInfo>> GetTrustedDevicesAsync(int memberId)
    {
        if (!IsTableAvailable()) return [];

        var devices = await Provider.Get()
            .WhereEquals(nameof(TrustedDeviceInfo.MemberID), memberId)
            .WhereEquals(nameof(TrustedDeviceInfo.IsActive), true)
            .OrderByDescending(nameof(TrustedDeviceInfo.LastUsedAt))
            .GetEnumerableTypedResultAsync();

        return devices.ToList();
    }

    /// <inheritdoc/>
    public async Task<bool> RevokeDeviceAsync(int memberId, int trustedDeviceId)
    {
        if (!IsTableAvailable()) return false;

        var device = (await Provider.Get()
            .WhereEquals(nameof(TrustedDeviceInfo.TrustedDeviceID), trustedDeviceId)
            .WhereEquals(nameof(TrustedDeviceInfo.MemberID), memberId)
            .TopN(1)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (device == null) return false;

        device.IsActive = false;
        Provider.Set(device);

        // If revoking the current device, clear cookie
        var currentToken = GetDeviceCookieToken();
        if (currentToken == device.DeviceToken)
        {
            ClearDeviceCookie();
        }

        logger.LogInformation(
            "Trusted device revoked: {DeviceName} for member {MemberId}",
            device.DeviceName, memberId);

        return true;
    }

    /// <inheritdoc/>
    public async Task RevokeAllDevicesAsync(int memberId)
    {
        if (!IsTableAvailable()) return;

        var devices = await Provider.Get()
            .WhereEquals(nameof(TrustedDeviceInfo.MemberID), memberId)
            .WhereEquals(nameof(TrustedDeviceInfo.IsActive), true)
            .GetEnumerableTypedResultAsync();

        foreach (var device in devices)
        {
            device.IsActive = false;
            Provider.Set(device);
        }

        ClearDeviceCookie();

        logger.LogInformation("All trusted devices revoked for member {MemberId}", memberId);
    }

    /// <inheritdoc/>
    public async Task CleanupExpiredAsync()
    {
        if (!IsTableAvailable()) return;

        // Delete inactive devices older than 90 days
        var cutoff = DateTime.UtcNow.AddDays(-90);
        var expired = await Provider.Get()
            .Where(w => w
                .WhereEquals(nameof(TrustedDeviceInfo.IsActive), false)
                .Or()
                .Where(inner => inner
                    .WhereNotNull(nameof(TrustedDeviceInfo.ExpiresAt))
                    .WhereLessThan(nameof(TrustedDeviceInfo.ExpiresAt), DateTime.UtcNow)))
            .WhereLessThan(nameof(TrustedDeviceInfo.LastUsedAt), cutoff)
            .GetEnumerableTypedResultAsync();

        foreach (var item in expired)
        {
            Provider.Delete(item);
        }
    }

    /// <summary>
    /// Get current device token from cookie (for external use, e.g., to detect current device).
    /// </summary>
    public string? GetCurrentDeviceToken() => GetDeviceCookieToken();

    private async Task<TrustedDeviceInfo?> FindExistingTrustedDevice(int memberId, string fingerprint)
    {
        if (!IsTableAvailable()) return null;

        return (await Provider.Get()
            .WhereEquals(nameof(TrustedDeviceInfo.MemberID), memberId)
            .WhereEquals(nameof(TrustedDeviceInfo.DeviceFingerprint), fingerprint)
            .WhereEquals(nameof(TrustedDeviceInfo.IsActive), true)
            .TopN(1)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();
    }

    private void SetDeviceCookie(string token)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = _options.TrustDuration ?? TimeSpan.FromDays(365),
            IsEssential = true
        };

        httpContext.Response.Cookies.Append(CookieName, token, cookieOptions);
    }

    private string? GetDeviceCookieToken()
    {
        return httpContextAccessor.HttpContext?.Request.Cookies[CookieName];
    }

    private void ClearDeviceCookie()
    {
        httpContextAccessor.HttpContext?.Response.Cookies.Delete(CookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
    }

    private static string GenerateDeviceToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GenerateFingerprint(string userAgent, string? ipAddress)
    {
        var ipPrefix = !string.IsNullOrEmpty(ipAddress) && ipAddress.Contains('.')
            ? string.Join(".", ipAddress.Split('.').Take(3)) + ".x"
            : ipAddress ?? "";

        var data = $"{userAgent}|{ipPrefix}";
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string ParseDeviceType(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown";
        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("mobile") || (ua.Contains("android") && !ua.Contains("tablet")) ||
            ua.Contains("iphone") || ua.Contains("ipod"))
            return "Mobile";
        if (ua.Contains("tablet") || ua.Contains("ipad"))
            return "Tablet";
        if (ua.Contains("windows") || ua.Contains("macintosh") || ua.Contains("linux"))
            return "Desktop";
        return "Unknown";
    }

    private static string FormatOS(UAParser.OS os)
    {
        if (os == null || string.IsNullOrEmpty(os.Family)) return "Unknown";
        return string.IsNullOrEmpty(os.Major) ? os.Family : $"{os.Family} {os.Major}";
    }

    private static string? GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null) return null;
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            return forwardedFor.Split(',').First().Trim();
        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp)) return realIp;
        return httpContext.Connection.RemoteIpAddress?.ToString();
    }
}

/// <summary>
/// Configuration options for trusted device management.
/// </summary>
public class TrustedDeviceOptions
{
    /// <summary>
    /// How long a device remains trusted. Default: 90 days.
    /// Set to null for indefinite trust (until manually revoked).
    /// </summary>
    public TimeSpan? TrustDuration { get; set; } = TimeSpan.FromDays(90);

    /// <summary>Whether trusted device management is enabled. Default: true.</summary>
    public bool Enabled { get; set; } = true;
}
