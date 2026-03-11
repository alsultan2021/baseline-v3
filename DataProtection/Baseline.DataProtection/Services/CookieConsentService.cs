using Baseline.DataProtection.Configuration;
using Baseline.DataProtection.Interfaces;
using Baseline.DataProtection.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Baseline.DataProtection.Services;

/// <summary>
/// Default implementation of cookie consent service.
/// </summary>
public class CookieConsentService : ICookieConsentService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<BaselineDataProtectionOptions> _options;
    private readonly ILogger<CookieConsentService> _logger;

    public CookieConsentService(
        IHttpContextAccessor httpContextAccessor,
        IOptions<BaselineDataProtectionOptions> options,
        ILogger<CookieConsentService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CookieConsentLevel> GetConsentLevelAsync()
    {
        var preferences = await GetPreferencesAsync();
        
        if (preferences.MarketingCookies)
            return CookieConsentLevel.All;
        if (preferences.AnalyticsCookies)
            return CookieConsentLevel.Analytics;
        if (preferences.FunctionalCookies)
            return CookieConsentLevel.Functional;
        if (preferences.NecessaryCookies)
            return CookieConsentLevel.Necessary;
        
        return CookieConsentLevel.None;
    }

    /// <inheritdoc />
    public async Task SetConsentLevelAsync(CookieConsentLevel level)
    {
        var preferences = level switch
        {
            CookieConsentLevel.All => new CookieConsentPreferences(true, true, true, true, DateTime.UtcNow),
            CookieConsentLevel.Analytics => new CookieConsentPreferences(true, true, true, false, DateTime.UtcNow),
            CookieConsentLevel.Functional => new CookieConsentPreferences(true, true, false, false, DateTime.UtcNow),
            CookieConsentLevel.Necessary => new CookieConsentPreferences(true, false, false, false, DateTime.UtcNow),
            _ => new CookieConsentPreferences(false, false, false, false, null)
        };

        await SetPreferencesAsync(preferences);
    }

    /// <inheritdoc />
    public bool IsConsentRequired()
    {
        return _options.Value.RequireConsentBeforeTracking;
    }

    /// <inheritdoc />
    public async Task<bool> IsCategoryAllowedAsync(CookieCategory category)
    {
        var preferences = await GetPreferencesAsync();
        
        return category switch
        {
            CookieCategory.Necessary => true, // Always allowed
            CookieCategory.Functional => preferences.FunctionalCookies,
            CookieCategory.Analytics => preferences.AnalyticsCookies,
            CookieCategory.Marketing => preferences.MarketingCookies,
            _ => false
        };
    }

    /// <inheritdoc />
    public Task<CookieConsentPreferences> GetPreferencesAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return Task.FromResult(new CookieConsentPreferences(false, false, false, false, null));
        }

        var cookieName = _options.Value.ConsentCookieName;
        if (context.Request.Cookies.TryGetValue(cookieName, out var cookieValue))
        {
            try
            {
                var data = JsonSerializer.Deserialize<CookieConsentCookieData>(cookieValue);
                if (data != null)
                {
                    var timestamp = DateTimeOffset.FromUnixTimeSeconds(data.Timestamp).UtcDateTime;
                    return Task.FromResult(new CookieConsentPreferences(
                        data.Necessary,
                        data.Functional,
                        data.Analytics,
                        data.Marketing,
                        timestamp
                    ));
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse consent cookie");
            }
        }

        return Task.FromResult(new CookieConsentPreferences(false, false, false, false, null));
    }

    /// <inheritdoc />
    public Task SetPreferencesAsync(CookieConsentPreferences preferences)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            _logger.LogWarning("No HTTP context available to set consent cookie");
            return Task.CompletedTask;
        }

        var data = new CookieConsentCookieData(
            preferences.NecessaryCookies,
            preferences.FunctionalCookies,
            preferences.AnalyticsCookies,
            preferences.MarketingCookies,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            "1.0"
        );

        var cookieOptions = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(_options.Value.ConsentCookieExpirationDays),
            HttpOnly = false, // Needs to be readable by JavaScript
            Secure = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true
        };

        var cookieValue = JsonSerializer.Serialize(data);
        context.Response.Cookies.Append(_options.Value.ConsentCookieName, cookieValue, cookieOptions);

        _logger.LogDebug("Set consent preferences: Functional={Functional}, Analytics={Analytics}, Marketing={Marketing}",
            preferences.FunctionalCookies, preferences.AnalyticsCookies, preferences.MarketingCookies);

        return Task.CompletedTask;
    }
}
