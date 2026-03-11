using Baseline.DigitalMarketing.Configuration;
using Baseline.DigitalMarketing.Interfaces;
using CMS.Activities;
using CMS.ContactManagement;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.DigitalMarketing.Services;

/// <summary>
/// Default implementation of activity logging service.
/// </summary>
public class ActivityLoggingService : IActivityLoggingService
{
    private readonly IActivityLogService _activityLogService;
    private readonly IContactTrackingService _contactTrackingService;
    private readonly IOptions<BaselineDigitalMarketingOptions> _options;
    private readonly ILogger<ActivityLoggingService> _logger;

    public ActivityLoggingService(
        IActivityLogService activityLogService,
        IContactTrackingService contactTrackingService,
        IOptions<BaselineDigitalMarketingOptions> options,
        ILogger<ActivityLoggingService> logger)
    {
        _activityLogService = activityLogService;
        _contactTrackingService = contactTrackingService;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task LogActivityAsync(string activityType, string title, string? value = null)
    {
        if (!_options.Value.EnableActivityLogging)
        {
            _logger.LogDebug("Activity logging is disabled, skipping {ActivityType}", activityType);
            return;
        }

        if (_options.Value.ExcludedActivityTypes.Contains(activityType))
        {
            _logger.LogDebug("Activity type {ActivityType} is excluded", activityType);
            return;
        }

        try
        {
            var contact = await _contactTrackingService.GetCurrentContactAsync();
            if (contact == null)
            {
                _logger.LogDebug("No contact available, skipping activity logging");
                return;
            }

            var activityInitializer = new ActivityInitializer(activityType, title)
            {
                ContactID = contact.ContactID,
                Value = value
            };

            _activityLogService.Log(activityInitializer);
            _logger.LogDebug("Logged activity {ActivityType}: {Title}", activityType, title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging activity {ActivityType}", activityType);
        }
    }

    /// <inheritdoc />
    public async Task LogPageVisitAsync(string pageUrl, string pageTitle)
    {
        if (!_options.Value.LogPageVisitActivities)
        {
            return;
        }

        // Check if the page should be excluded
        if (_options.Value.ExcludedPagePaths.Any(p => pageUrl.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogDebug("Page {PageUrl} is excluded from tracking", pageUrl);
            return;
        }

        await LogActivityAsync("pagevisit", pageTitle, pageUrl);
    }

    /// <inheritdoc />
    public async Task LogCustomActivityAsync(string activityType, IDictionary<string, string>? data = null)
    {
        if (!_options.Value.EnableActivityLogging)
        {
            return;
        }

        try
        {
            var contact = await _contactTrackingService.GetCurrentContactAsync();
            if (contact == null)
            {
                return;
            }

            var title = data?.TryGetValue("title", out var titleValue) == true ? titleValue : activityType;
            var value = data != null ? string.Join("; ", data.Select(kv => $"{kv.Key}={kv.Value}")) : null;

            await LogActivityAsync(activityType, title, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging custom activity {ActivityType}", activityType);
        }
    }

    /// <inheritdoc />
    public async Task LogFormSubmissionAsync(string formCodeName, string formDisplayName)
    {
        await LogActivityAsync("bizformsubmit", formDisplayName, formCodeName);
    }

    /// <inheritdoc />
    public async Task LogNewsletterSubscriptionAsync(string recipientListCodeName)
    {
        await LogActivityAsync("newsletter_subscribe", $"Subscribed to {recipientListCodeName}", recipientListCodeName);
    }
}

/// <summary>
/// Simple activity initializer for logging activities.
/// </summary>
internal class ActivityInitializer : IActivityInitializer
{
    public ActivityInitializer(string activityType, string title)
    {
        ActivityType = activityType;
        Title = title;
    }

    public string ActivityType { get; }
    public string? Title { get; set; }
    public string? Value { get; set; }
    public int ContactID { get; set; }

    public string SettingsKeyName => string.Empty;

    public void Initialize(IActivityInfo activity)
    {
        activity.ActivityType = ActivityType;
        activity.ActivityTitle = Title ?? string.Empty;
        activity.ActivityValue = Value ?? string.Empty;
        activity.ActivityContactID = ContactID;
    }
}
