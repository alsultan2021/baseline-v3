using Baseline.Experiments.Interfaces;
using CMS.Activities;
using CMS.ContactManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Baseline.Experiments.Services;

/// <summary>
/// Bridges experiment conversions/assignments to XbK custom activities
/// and resolves user identity from the XbK contact management context.
/// </summary>
/// <remarks>
/// Activity types <c>experiment_assignment</c> and <c>experiment_conversion</c>
/// must be created in Xperience admin → Contact management → Activity types.
/// </remarks>
public class XbkConversionBridgeService(
    ICustomActivityLogger customActivityLogger,
    ICurrentContactProvider currentContactProvider,
    IHttpContextAccessor httpContextAccessor,
    ILogger<XbkConversionBridgeService> logger) : IXbkConversionBridgeService
{
    private const string AssignmentActivityType = "experiment_assignment";
    private const string ConversionActivityType = "experiment_conversion";

    private readonly ICustomActivityLogger _customActivityLogger = customActivityLogger;
    private readonly ICurrentContactProvider _currentContactProvider = currentContactProvider;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ILogger<XbkConversionBridgeService> _logger = logger;

    /// <inheritdoc />
    public string GetCurrentUserId()
    {
        try
        {
            var contact = _currentContactProvider.GetCurrentContact();
            if (contact != null)
            {
                return contact.ContactGUID.ToString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not retrieve current XbK contact, falling back to anonymous ID");
        }

        // Fallback: use a cookie-based anonymous identifier
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return Guid.NewGuid().ToString();
        }

        const string anonymousCookieName = "baseline_exp_anon";
        if (context.Request.Cookies.TryGetValue(anonymousCookieName, out var anonId)
            && !string.IsNullOrEmpty(anonId))
        {
            return anonId;
        }

        var newAnonId = Guid.NewGuid().ToString();
        context.Response.Cookies.Append(anonymousCookieName, newAnonId, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(365),
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = false
        });

        return newAnonId;
    }

    /// <inheritdoc />
    public Task LogConversionActivityAsync(
        string experimentName,
        string variantName,
        string goalCodeName,
        decimal? value = null)
    {
        try
        {
            var activityData = new CustomActivityData
            {
                ActivityTitle = $"Experiment conversion: {experimentName} / {goalCodeName}",
                ActivityValue = $"{variantName}|{goalCodeName}" + (value.HasValue ? $"|{value.Value}" : "")
            };

            _customActivityLogger.Log(ConversionActivityType, activityData);

            _logger.LogDebug(
                "Logged XbK conversion activity for experiment {Experiment}, variant {Variant}, goal {Goal}",
                experimentName, variantName, goalCodeName);
        }
        catch (Exception ex)
        {
            // Don't let activity logging failures break the experiment flow
            _logger.LogWarning(ex,
                "Failed to log XbK conversion activity for experiment {Experiment}",
                experimentName);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogAssignmentActivityAsync(string experimentName, string variantName)
    {
        try
        {
            var activityData = new CustomActivityData
            {
                ActivityTitle = $"Experiment assignment: {experimentName} → {variantName}",
                ActivityValue = $"{experimentName}|{variantName}"
            };

            _customActivityLogger.Log(AssignmentActivityType, activityData);

            _logger.LogDebug(
                "Logged XbK assignment activity for experiment {Experiment}, variant {Variant}",
                experimentName, variantName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to log XbK assignment activity for experiment {Experiment}",
                experimentName);
        }

        return Task.CompletedTask;
    }
}
