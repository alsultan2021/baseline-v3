using System.Security.Cryptography;
using System.Text;
using CMS.Core;
using CMS.DataEngine;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Kentico.Membership;

namespace Baseline.Account;

/// <summary>
/// Implementation of session management service.
/// </summary>
public class SessionManagementService<TUser>(
    IHttpContextAccessor httpContextAccessor,
    UserManager<TUser> userManager,
    IExternalAuthenticationService externalAuthService) : ISessionManagementService
    where TUser : ApplicationUser
{
    // Lazy resolution to avoid DI issues with Info classes that use Kentico's internal registration
    private IInfoProvider<LoginAuditLogInfo>? _auditLogProvider;
    private bool _auditLogUnavailable;

    private IInfoProvider<LoginAuditLogInfo>? AuditLogProvider
    {
        get
        {
            if (_auditLogUnavailable)
                return null;

            if (_auditLogProvider is not null)
                return _auditLogProvider;

            try
            {
                _auditLogProvider = Service.Resolve<IInfoProvider<LoginAuditLogInfo>>();
                return _auditLogProvider;
            }
            catch (ServiceResolutionException)
            {
                // LoginAuditLog table/class not yet installed - mark as unavailable
                _auditLogUnavailable = true;
                return null;
            }
            catch (Exception)
            {
                _auditLogUnavailable = true;
                return null;
            }
        }
    }

    public async Task<IReadOnlyList<UserSession>> GetActiveSessionsAsync(int memberId)
    {
        var provider = AuditLogProvider;
        if (provider is null)
            return [];

        var currentFingerprint = GetCurrentSessionId();

        // Get recent successful logins grouped by device fingerprint
        var recentLogins = await provider.Get()
            .WhereEquals(nameof(LoginAuditLogInfo.MemberID), memberId)
            .WhereIn(nameof(LoginAuditLogInfo.ActionType), [
                LoginAuditActionType.LoginSuccess,
                LoginAuditActionType.TwoFactorSuccess,
                LoginAuditActionType.ExternalLogin,
                "PasskeyLogin"
            ])
            .WhereEquals(nameof(LoginAuditLogInfo.IsSuccess), true)
            .WhereNotNull(nameof(LoginAuditLogInfo.DeviceFingerprint))
            .OrderByDescending(nameof(LoginAuditLogInfo.AttemptedAt))
            .TopN(50) // Limit to recent sessions
            .GetEnumerableTypedResultAsync();

        // Get terminated/logged out sessions
        var terminatedSessions = await provider.Get()
            .WhereEquals(nameof(LoginAuditLogInfo.MemberID), memberId)
            .WhereIn(nameof(LoginAuditLogInfo.ActionType), [
                LoginAuditActionType.Logout,
                LoginAuditActionType.SessionTerminated
            ])
            .WhereNotNull(nameof(LoginAuditLogInfo.DeviceFingerprint))
            .GetEnumerableTypedResultAsync();

        // Build a set of fingerprints that have been terminated after their last login
        var terminatedFingerprints = new HashSet<string>();
        var loginsByFingerprint = recentLogins.GroupBy(l => l.DeviceFingerprint).ToDictionary(g => g.Key!, g => g.First());

        foreach (var termination in terminatedSessions)
        {
            var fp = termination.DeviceFingerprint!;
            if (loginsByFingerprint.TryGetValue(fp, out var lastLogin) &&
                termination.AttemptedAt >= lastLogin.AttemptedAt)
            {
                terminatedFingerprints.Add(fp);
            }
        }

        // Group by device fingerprint, taking the most recent login for each device
        // Filter out terminated sessions
        var sessions = recentLogins
            .GroupBy(l => l.DeviceFingerprint)
            .Select(g => g.First()) // Most recent login per device
            .Where(log => !terminatedFingerprints.Contains(log.DeviceFingerprint!))
            .Take(10) // Limit displayed sessions
            .Select(log => new UserSession
            {
                SessionId = log.DeviceFingerprint!,
                DeviceType = log.DeviceType ?? "Unknown",
                Browser = log.Browser ?? "Unknown",
                OperatingSystem = log.OperatingSystem ?? "Unknown",
                IpAddress = log.IpAddress ?? string.Empty,
                Location = log.Location,
                CreatedAt = DateTime.SpecifyKind(log.AttemptedAt, DateTimeKind.Utc),
                LastActivityAt = DateTime.SpecifyKind(log.AttemptedAt, DateTimeKind.Utc),
                IsCurrent = log.DeviceFingerprint == currentFingerprint
            })
            .ToList();

        return sessions;
    }

    public async Task<IReadOnlyList<LoginHistoryEntry>> GetLoginHistoryAsync(int memberId, int count = 10)
    {
        var provider = AuditLogProvider;
        if (provider is null)
            return [];

        var logs = await provider.Get()
            .WhereEquals(nameof(LoginAuditLogInfo.MemberID), memberId)
            .OrderByDescending(nameof(LoginAuditLogInfo.AttemptedAt))
            .TopN(count)
            .GetEnumerableTypedResultAsync();

        return logs.Select(log => new LoginHistoryEntry
        {
            Timestamp = DateTime.SpecifyKind(log.AttemptedAt, DateTimeKind.Utc),
            IsSuccess = log.IsSuccess,
            ActionType = log.ActionType,
            DeviceType = log.DeviceType ?? "Unknown",
            Browser = log.Browser ?? "Unknown",
            OperatingSystem = log.OperatingSystem ?? "Unknown",
            IpAddress = log.IpAddress ?? string.Empty,
            Location = log.Location,
            IsNewDevice = log.IsNewDevice,
            FailureReason = log.FailureReason
        }).ToList();
    }

    public async Task<IReadOnlyList<LinkedAccount>> GetLinkedAccountsAsync(int memberId)
    {
        // Get the user and their external logins from Identity
        var user = await userManager.FindByIdAsync(memberId.ToString());
        if (user is null)
            return [];

        var logins = await userManager.GetLoginsAsync(user);
        var availableProviders = externalAuthService.GetProviders().ToList();

        return logins.Select(login =>
        {
            var providerInfo = availableProviders.FirstOrDefault(p =>
                p.Name.Equals(login.LoginProvider, StringComparison.OrdinalIgnoreCase));

            return new LinkedAccount
            {
                Provider = login.LoginProvider,
                DisplayName = providerInfo?.DisplayName ?? login.LoginProvider,
                ProviderKey = login.ProviderKey,
                Email = null, // Could be extracted from claims if stored
                LinkedAt = null, // Identity doesn't track this by default
                IconClass = GetProviderIcon(login.LoginProvider),
                BrandColor = GetProviderColor(login.LoginProvider)
            };
        }).ToList();
    }

    public async Task<RevokeSessionResult> RevokeSessionAsync(int memberId, string sessionId)
    {
        var provider = AuditLogProvider;
        if (provider is null)
            return RevokeSessionResult.Failed("Session tracking not available");

        // Get HTTP context info for the termination log
        var httpContext = httpContextAccessor.HttpContext;
        var userAgent = httpContext?.Request.Headers.UserAgent.FirstOrDefault() ?? "Unknown";
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var username = httpContext?.User?.Identity?.Name ?? "Unknown";

        // Log a session termination event for this device fingerprint
        var terminationLog = new LoginAuditLogInfo
        {
            MemberID = memberId,
            Username = username,
            ActionType = LoginAuditActionType.SessionTerminated,
            IsSuccess = true,
            IsNewDevice = false,
            AlertSent = false,
            DeviceFingerprint = sessionId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            AttemptedAt = DateTime.UtcNow
        };

        provider.Set(terminationLog);

        return RevokeSessionResult.Succeeded(1);
    }

    public async Task<RevokeSessionResult> RevokeAllOtherSessionsAsync(int memberId, string? currentSessionId)
    {
        var user = await userManager.FindByIdAsync(memberId.ToString());
        if (user is null)
            return RevokeSessionResult.Failed("User not found");

        // Update security stamp - this invalidates all existing auth cookies
        var result = await userManager.UpdateSecurityStampAsync(user);
        if (!result.Succeeded)
            return RevokeSessionResult.Failed(string.Join(", ", result.Errors.Select(e => e.Description)));

        // The current user will need to re-authenticate after this
        // In a more sophisticated implementation, we'd exclude the current session
        return RevokeSessionResult.Succeeded();
    }

    public string GetCurrentSessionId()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
            return string.Empty;

        var userAgent = httpContext.Request.Headers.UserAgent.FirstOrDefault() ?? "Unknown";
        var ipAddress = GetClientIpAddress(httpContext);

        // Must match LoginAuditService.GenerateDeviceFingerprint exactly
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

        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            return forwardedFor.Split(',').First().Trim();

        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        var ip = httpContext.Connection.RemoteIpAddress;
        if (ip != null && ip.IsIPv4MappedToIPv6)
            return ip.MapToIPv4().ToString();

        return ip?.ToString();
    }

    private static string GetProviderIcon(string provider) => provider.ToLowerInvariant() switch
    {
        "google" => "fab fa-google",
        "microsoft" => "fab fa-microsoft",
        "facebook" => "fab fa-facebook",
        "twitter" => "fab fa-twitter",
        "apple" => "fab fa-apple",
        "github" => "fab fa-github",
        "linkedin" => "fab fa-linkedin",
        _ => "fas fa-link"
    };

    private static string GetProviderColor(string provider) => provider.ToLowerInvariant() switch
    {
        "google" => "#4285F4",
        "microsoft" => "#00A4EF",
        "facebook" => "#1877F2",
        "twitter" => "#1DA1F2",
        "apple" => "#000000",
        "github" => "#333333",
        "linkedin" => "#0A66C2",
        _ => "#6c757d"
    };
}
