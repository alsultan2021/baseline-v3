using System.Security.Cryptography;
using CMS.Core;
using CMS.DataEngine;
using CMS.Membership;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UAParser;

namespace Baseline.Account;

/// <summary>
/// Implementation of device authorization flow (Netflix / Google TV-style sign-in with code).
/// </summary>
public class DeviceAuthorizationService(
    IHttpContextAccessor httpContextAccessor,
    IOptions<DeviceAuthorizationOptions> options,
    ILogger<DeviceAuthorizationService> logger) : IDeviceAuthorizationService
{
    private readonly DeviceAuthorizationOptions _options = options.Value;
    private static readonly Parser s_uaParser = Parser.GetDefault();

    private IInfoProvider<DeviceAuthorizationInfo>? _provider;
    private IInfoProvider<DeviceAuthorizationInfo> Provider =>
        _provider ??= Service.Resolve<IInfoProvider<DeviceAuthorizationInfo>>();

    private bool IsTableAvailable()
    {
        try
        {
            return DataClassInfoProvider.GetDataClassInfo(DeviceAuthorizationInfo.OBJECT_TYPE) != null;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<DeviceAuthorizationResult> CreateAuthorizationRequestAsync()
    {
        var httpContext = httpContextAccessor.HttpContext;
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString() ?? "";
        var ipAddress = GetClientIpAddress(httpContext);
        var clientInfo = s_uaParser.Parse(userAgent);

        var userCode = GenerateUserCode();
        var deviceCode = GenerateDeviceCode();
        var expiresAt = DateTime.UtcNow.Add(_options.CodeLifetime);

        var deviceName = $"{clientInfo.UA.Family ?? "Unknown"} on {clientInfo.OS.Family ?? "Unknown"}";

        if (IsTableAvailable())
        {
            var authInfo = new DeviceAuthorizationInfo
            {
                UserCode = userCode,
                DeviceCode = deviceCode,
                Status = DeviceAuthorizationStatus.Pending,
                RequestingIpAddress = ipAddress,
                RequestingUserAgent = userAgent.Length > 500 ? userAgent[..500] : userAgent,
                RequestingDeviceFingerprint = GenerateFingerprint(userAgent, ipAddress),
                RequestingDeviceName = deviceName.Length > 200 ? deviceName[..200] : deviceName,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };

            Provider.Set(authInfo);

            logger.LogInformation("Device authorization request created: {UserCode} from {IpAddress}", userCode, ipAddress);
        }

        var baseUrl = GetBaseUrl(httpContext);

        return new DeviceAuthorizationResult
        {
            UserCode = userCode,
            DeviceCode = deviceCode,
            VerificationUrl = $"{baseUrl}/Account/DeviceAuthorize",
            VerificationUrlComplete = $"{baseUrl}/Account/DeviceAuthorize?code={userCode}",
            IntervalSeconds = _options.PollingIntervalSeconds,
            ExpiresAt = expiresAt
        };
    }

    /// <inheritdoc/>
    public async Task<DeviceAuthorizationPollResult> PollAuthorizationAsync(string deviceCode)
    {
        if (!IsTableAvailable())
            return new DeviceAuthorizationPollResult { Error = "Service unavailable" };

        var auth = (await Provider.Get()
            .WhereEquals(nameof(DeviceAuthorizationInfo.DeviceCode), deviceCode)
            .TopN(1)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (auth == null)
            return new DeviceAuthorizationPollResult { Error = "Invalid device code" };

        // Check expiration
        if (auth.ExpiresAt < DateTime.UtcNow)
        {
            auth.Status = DeviceAuthorizationStatus.Expired;
            Provider.Set(auth);
            return new DeviceAuthorizationPollResult { Expired = true };
        }

        return auth.Status switch
        {
            DeviceAuthorizationStatus.Pending => new DeviceAuthorizationPollResult { Pending = true },
            DeviceAuthorizationStatus.Approved => new DeviceAuthorizationPollResult
            {
                Authorized = true,
                ApprovedByMemberID = auth.ApprovedByMemberID,
                ApprovedByUsername = GetMemberUsername(auth.ApprovedByMemberID)
            },
            DeviceAuthorizationStatus.Denied => new DeviceAuthorizationPollResult { Denied = true },
            DeviceAuthorizationStatus.Expired => new DeviceAuthorizationPollResult { Expired = true },
            _ => new DeviceAuthorizationPollResult { Error = "Unknown status" }
        };
    }

    /// <inheritdoc/>
    public async Task<DeviceAuthorizationInfo?> GetPendingByUserCodeAsync(string userCode)
    {
        if (!IsTableAvailable()) return null;

        var normalizedCode = NormalizeUserCode(userCode);

        var auth = (await Provider.Get()
            .WhereEquals(nameof(DeviceAuthorizationInfo.UserCode), normalizedCode)
            .WhereEquals(nameof(DeviceAuthorizationInfo.Status), DeviceAuthorizationStatus.Pending)
            .WhereGreaterThan(nameof(DeviceAuthorizationInfo.ExpiresAt), DateTime.UtcNow)
            .TopN(1)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        return auth;
    }

    /// <inheritdoc/>
    public async Task<bool> ApproveAuthorizationAsync(string userCode, int memberId)
    {
        var auth = await GetPendingByUserCodeAsync(userCode);
        if (auth == null) return false;

        auth.Status = DeviceAuthorizationStatus.Approved;
        auth.ApprovedByMemberID = memberId;
        Provider.Set(auth);

        logger.LogInformation("Device authorization approved: {UserCode} by member {MemberId}", userCode, memberId);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> DenyAuthorizationAsync(string userCode)
    {
        var auth = await GetPendingByUserCodeAsync(userCode);
        if (auth == null) return false;

        auth.Status = DeviceAuthorizationStatus.Denied;
        Provider.Set(auth);

        logger.LogInformation("Device authorization denied: {UserCode}", userCode);
        return true;
    }

    /// <inheritdoc/>
    public async Task CleanupExpiredAsync()
    {
        if (!IsTableAvailable()) return;

        var cutoff = DateTime.UtcNow.AddDays(-1); // Keep for 24h after expiry for debugging
        var expired = await Provider.Get()
            .WhereLessThan(nameof(DeviceAuthorizationInfo.ExpiresAt), cutoff)
            .GetEnumerableTypedResultAsync();

        foreach (var item in expired)
        {
            Provider.Delete(item);
        }
    }

    /// <summary>
    /// Generate a user-friendly code like "ABCD-1234".
    /// </summary>
    private static string GenerateUserCode()
    {
        const string letters = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // No I/O to avoid confusion
        const string digits = "0123456789";
        Span<byte> randomBytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(randomBytes);

        var part1 = new char[4];
        var part2 = new char[4];

        for (int i = 0; i < 4; i++)
        {
            part1[i] = letters[randomBytes[i] % letters.Length];
            part2[i] = digits[randomBytes[i + 4] % digits.Length];
        }

        return $"{new string(part1)}-{new string(part2)}";
    }

    /// <summary>
    /// Generate a cryptographically secure device code for polling.
    /// </summary>
    private static string GenerateDeviceCode()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string NormalizeUserCode(string userCode)
    {
        return userCode.Trim().ToUpperInvariant().Replace(" ", "-");
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

    private static string? GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null) return null;

        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            return forwardedFor.Split(',').First().Trim();

        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private static string GetBaseUrl(HttpContext? httpContext)
    {
        if (httpContext == null) return "";
        var request = httpContext.Request;
        return $"{request.Scheme}://{request.Host}";
    }

    private static string? GetMemberUsername(int? memberId)
    {
        if (!memberId.HasValue || memberId.Value == 0) return null;
        try
        {
            var member = MemberInfo.Provider.Get(memberId.Value);
            return member?.MemberName;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Configuration options for device authorization flow.
/// </summary>
public class DeviceAuthorizationOptions
{
    /// <summary>How long the user code is valid. Default: 10 minutes.</summary>
    public TimeSpan CodeLifetime { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>How often the device should poll for approval (seconds). Default: 5.</summary>
    public int PollingIntervalSeconds { get; set; } = 5;

    /// <summary>Whether device authorization is enabled. Default: true.</summary>
    public bool Enabled { get; set; } = true;
}
