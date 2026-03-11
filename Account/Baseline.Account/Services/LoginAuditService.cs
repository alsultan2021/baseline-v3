using System.Security.Cryptography;
using System.Text;
using CMS.Core;
using CMS.DataEngine;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UAParser;

namespace Baseline.Account;

/// <summary>
/// Implementation of login audit service.
/// </summary>
public class LoginAuditService(
    IHttpContextAccessor httpContextAccessor,
    IOptions<LoginAuditOptions> options,
    ILogger<LoginAuditService> logger) : ILoginAuditService
{
    private readonly LoginAuditOptions _options = options.Value;
    private IInfoProvider<LoginAuditLogInfo>? _auditLogProvider;

    // Lazy-load the provider to avoid DI issues before table is created
    private IInfoProvider<LoginAuditLogInfo> AuditLogProvider =>
        _auditLogProvider ??= Service.Resolve<IInfoProvider<LoginAuditLogInfo>>();

    private bool IsTableAvailable()
    {
        try
        {
            // Check if the table exists by attempting to access the provider
            return DataClassInfoProvider.GetDataClassInfo(LoginAuditLogInfo.OBJECT_TYPE) != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task LogLoginAttemptAsync(LoginAttemptContext context)
    {
        if (!IsTableAvailable())
        {
            logger.LogDebug("Login audit table not yet installed, skipping log");
            return;
        }

        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            var deviceInfo = ParseDeviceInfo(httpContext);
            var ipAddress = GetClientIpAddress(httpContext);

            bool isNewDevice = context.MemberId.HasValue
                && await IsNewDeviceAsync(context.MemberId.Value, deviceInfo.DeviceFingerprint);

            var auditLog = new LoginAuditLogInfo
            {
                MemberID = context.MemberId,
                Username = TruncateString(context.Username, 200) ?? context.Username,
                ActionType = context.ActionType,
                IsSuccess = context.IsSuccess,
                IpAddress = ipAddress,
                UserAgent = TruncateString(httpContext?.Request.Headers.UserAgent.ToString(), 500),
                DeviceType = deviceInfo.DeviceType,
                Browser = deviceInfo.Browser,
                OperatingSystem = deviceInfo.OperatingSystem,
                DeviceFingerprint = deviceInfo.DeviceFingerprint,
                FailureReason = context.FailureReason,
                IsNewDevice = isNewDevice,
                AlertSent = false,
                AttemptedAt = DateTime.UtcNow
            };

            AuditLogProvider.Set(auditLog);

            logger.LogDebug(
                "Login audit: {ActionType} for {Username} from {IpAddress} - Success: {IsSuccess}",
                context.ActionType, context.Username, ipAddress, context.IsSuccess);
        }
        catch (Exception ex)
        {
            // Don't let audit logging break the login flow
            logger.LogError(ex, "Failed to log login attempt for {Username}", context.Username);
        }
    }

    public Task LogLogoutAsync(int memberId, string username)
    {
        return LogLoginAttemptAsync(new LoginAttemptContext
        {
            MemberId = memberId,
            Username = username,
            ActionType = LoginAuditActionType.Logout,
            IsSuccess = true
        });
    }

    public Task LogPasswordChangedAsync(int memberId, string username)
    {
        return LogLoginAttemptAsync(new LoginAttemptContext
        {
            MemberId = memberId,
            Username = username,
            ActionType = LoginAuditActionType.PasswordChanged,
            IsSuccess = true
        });
    }

    public Task LogPasswordResetAsync(int memberId, string username)
    {
        return LogLoginAttemptAsync(new LoginAttemptContext
        {
            MemberId = memberId,
            Username = username,
            ActionType = LoginAuditActionType.PasswordReset,
            IsSuccess = true
        });
    }

    public Task LogTwoFactorAttemptAsync(int memberId, string username, bool success)
    {
        return LogLoginAttemptAsync(new LoginAttemptContext
        {
            MemberId = memberId,
            Username = username,
            ActionType = success ? LoginAuditActionType.TwoFactorSuccess : LoginAuditActionType.TwoFactorFailed,
            IsSuccess = success,
            FailureReason = success ? null : "InvalidCode"
        });
    }

    public async Task<IReadOnlyList<LoginAuditLogInfo>> GetLoginHistoryAsync(int memberId, int count = 10)
    {
        if (!IsTableAvailable()) return [];

        var query = AuditLogProvider.Get()
            .WhereEquals(nameof(LoginAuditLogInfo.MemberID), memberId)
            .OrderByDescending(nameof(LoginAuditLogInfo.AttemptedAt))
            .TopN(count);

        var results = await query.GetEnumerableTypedResultAsync();
        return results.ToList();
    }

    public async Task<IReadOnlyList<LoginAuditLogInfo>> GetActiveSessionsAsync(int memberId)
    {
        // Get successful logins in the last 30 days where ActionType is LoginSuccess
        var since = DateTime.UtcNow.AddDays(-30);

        if (!IsTableAvailable()) return [];

        var query = AuditLogProvider.Get()
            .WhereEquals(nameof(LoginAuditLogInfo.MemberID), memberId)
            .WhereEquals(nameof(LoginAuditLogInfo.ActionType), LoginAuditActionType.LoginSuccess)
            .WhereEquals(nameof(LoginAuditLogInfo.IsSuccess), true)
            .WhereGreaterThan(nameof(LoginAuditLogInfo.AttemptedAt), since)
            .OrderByDescending(nameof(LoginAuditLogInfo.AttemptedAt));

        var results = await query.GetEnumerableTypedResultAsync();
        return results.ToList();
    }

    public async Task<bool> IsNewDeviceAsync(int memberId, string deviceFingerprint)
    {
        if (string.IsNullOrEmpty(deviceFingerprint))
            return true;

        if (!IsTableAvailable()) return true;

        // Check if we've seen this device fingerprint for this member
        var existingDevice = await AuditLogProvider.Get()
            .WhereEquals(nameof(LoginAuditLogInfo.MemberID), memberId)
            .WhereEquals(nameof(LoginAuditLogInfo.DeviceFingerprint), deviceFingerprint)
            .WhereEquals(nameof(LoginAuditLogInfo.IsSuccess), true)
            .TopN(1)
            .GetEnumerableTypedResultAsync();

        return !existingDevice.Any();
    }

    public async Task<bool> HasSuspiciousActivityAsync(string ipAddress, TimeSpan window, int maxFailures)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return false;

        if (!IsTableAvailable()) return false;

        var since = DateTime.UtcNow - window;

        var failedAttempts = await AuditLogProvider.Get()
            .WhereEquals(nameof(LoginAuditLogInfo.IpAddress), ipAddress)
            .WhereEquals(nameof(LoginAuditLogInfo.IsSuccess), false)
            .WhereGreaterThan(nameof(LoginAuditLogInfo.AttemptedAt), since)
            .GetEnumerableTypedResultAsync();

        return failedAttempts.Count() >= maxFailures;
    }

    /// <inheritdoc/>
    public async Task<bool> IsTwoFactorIpTrustedAsync(int memberId, string ipAddress, TimeSpan trustWindow)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return false;

        if (!IsTableAvailable())
            return false;

        var since = DateTime.UtcNow - trustWindow;

        // Check if there was a successful 2FA from this IP within the trust window
        var trustedLogins = await AuditLogProvider.Get()
            .WhereEquals(nameof(LoginAuditLogInfo.MemberID), memberId)
            .WhereEquals(nameof(LoginAuditLogInfo.IpAddress), ipAddress)
            .WhereEquals(nameof(LoginAuditLogInfo.ActionType), LoginAuditActionType.TwoFactorSuccess)
            .WhereEquals(nameof(LoginAuditLogInfo.IsSuccess), true)
            .WhereGreaterThan(nameof(LoginAuditLogInfo.AttemptedAt), since)
            .TopN(1)
            .GetEnumerableTypedResultAsync();

        return trustedLogins.Any();
    }

    /// <inheritdoc/>
    public string? GetCurrentClientIpAddress() => GetClientIpAddress(httpContextAccessor.HttpContext);

    private static readonly Parser s_uaParser = Parser.GetDefault();

    private DeviceInfo ParseDeviceInfo(HttpContext? httpContext)
    {
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty;
        var ipAddress = GetClientIpAddress(httpContext);
        var clientInfo = s_uaParser.Parse(userAgent);

        return new DeviceInfo
        {
            DeviceType = ParseDeviceType(clientInfo.Device.Family, userAgent),
            Browser = clientInfo.UA.Family ?? "Unknown",
            OperatingSystem = FormatOS(clientInfo.OS),
            DeviceFingerprint = GenerateDeviceFingerprint(userAgent, ipAddress)
        };
    }

    private static string ParseDeviceType(string? deviceFamily, string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown";

        var ua = userAgent.ToLowerInvariant();

        if (ua.Contains("mobile") || ua.Contains("android") && !ua.Contains("tablet") ||
            ua.Contains("iphone") || ua.Contains("ipod"))
            return "Mobile";

        if (ua.Contains("tablet") || ua.Contains("ipad"))
            return "Tablet";

        // UAParser device family "Spider" = bot/crawler
        if (string.Equals(deviceFamily, "Spider", StringComparison.OrdinalIgnoreCase))
            return "Bot";

        if (ua.Contains("windows") || ua.Contains("macintosh") || ua.Contains("linux"))
            return "Desktop";

        return "Unknown";
    }

    private static string FormatOS(UAParser.OS os)
    {
        if (os == null || string.IsNullOrEmpty(os.Family))
            return "Unknown";

        return string.IsNullOrEmpty(os.Major)
            ? os.Family
            : $"{os.Family} {os.Major}";
    }

    private static string GenerateDeviceFingerprint(string userAgent, string? ipAddress)
    {
        // Simple fingerprint based on user agent and IP prefix
        // In production, consider using a more sophisticated client-side fingerprinting library
        var ipPrefix = !string.IsNullOrEmpty(ipAddress) && ipAddress.Contains('.')
            ? string.Join(".", ipAddress.Split('.').Take(3)) + ".x"
            : ipAddress ?? "";

        var data = $"{userAgent}|{ipPrefix}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string? GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null)
            return null;

        // Check for forwarded headers (when behind proxy/load balancer)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take the first IP (original client)
            return forwardedFor.Split(',').First().Trim();
        }

        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        var ip = httpContext.Connection.RemoteIpAddress;
        if (ip == null)
            return null;

        // Normalize IPv6 loopback and IPv4-mapped-to-IPv6 addresses
        if (ip.IsIPv4MappedToIPv6)
            return ip.MapToIPv4().ToString();

        if (System.Net.IPAddress.IsLoopback(ip))
            return "127.0.0.1";

        return ip.ToString();
    }

    private static string? TruncateString(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value.Length <= maxLength ? value : value[..maxLength];
    }

}

/// <summary>
/// Options for login audit service.
/// </summary>
public class LoginAuditOptions
{
    /// <summary>Whether login auditing is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Whether to send email alerts for new device logins.</summary>
    public bool EnableNewDeviceAlerts { get; set; } = true;

    /// <summary>Number of days to retain audit logs.</summary>
    public int RetentionDays { get; set; } = 90;

    /// <summary>Time window for suspicious activity detection.</summary>
    public TimeSpan SuspiciousActivityWindow { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Number of failed attempts in window to consider suspicious.</summary>
    public int SuspiciousActivityThreshold { get; set; } = 5;

    /// <summary>
    /// Time window for trusting an IP after successful 2FA.
    /// When a user completes 2FA from an IP, that IP is trusted for this duration,
    /// allowing login from different browsers on the same network without re-triggering 2FA.
    /// </summary>
    public TimeSpan TwoFactorIpTrustWindow { get; set; } = TimeSpan.FromHours(24);
}
