namespace Baseline.EmailMarketing.Configuration;

/// <summary>
/// Configuration options for Baseline Email Marketing module.
/// </summary>
public class BaselineEmailMarketingOptions
{
    /// <summary>
    /// Gets or sets whether to enable double opt-in for subscriptions.
    /// </summary>
    public bool EnableDoubleOptIn { get; set; } = true;

    /// <summary>
    /// Gets or sets the confirmation email template code name.
    /// </summary>
    public string? ConfirmationEmailTemplate { get; set; }

    /// <summary>
    /// Gets or sets the welcome email template code name.
    /// </summary>
    public string? WelcomeEmailTemplate { get; set; }

    /// <summary>
    /// Gets or sets the unsubscribe confirmation template.
    /// </summary>
    public string? UnsubscribeConfirmationTemplate { get; set; }

    /// <summary>
    /// Gets or sets the preference center page path.
    /// </summary>
    public string? PreferenceCenterPath { get; set; } = "/email-preferences";

    /// <summary>
    /// Gets or sets the unsubscribe page path.
    /// </summary>
    public string? UnsubscribePagePath { get; set; } = "/unsubscribe";

    /// <summary>
    /// Gets or sets the default from email address.
    /// </summary>
    public string? DefaultFromEmail { get; set; }

    /// <summary>
    /// Gets or sets the default from name.
    /// </summary>
    public string? DefaultFromName { get; set; }

    /// <summary>
    /// Gets or sets the confirmation link expiration in hours.
    /// </summary>
    public int ConfirmationLinkExpirationHours { get; set; } = 72;

    /// <summary>
    /// Gets or sets whether to track email opens.
    /// </summary>
    public bool TrackOpens { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to track email clicks.
    /// </summary>
    public bool TrackClicks { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to require consent before subscription.
    /// </summary>
    public bool RequireConsentBeforeSubscription { get; set; } = true;

    /// <summary>
    /// Gets or sets the consent code name required for subscription.
    /// </summary>
    public string? SubscriptionConsentCodeName { get; set; } = "MarketingConsent";

    /// <summary>
    /// Gets or sets the default email format.
    /// </summary>
    public EmailFormat DefaultEmailFormat { get; set; } = EmailFormat.Html;

    /// <summary>
    /// Gets or sets newsletter categories for organization.
    /// </summary>
    public List<NewsletterCategory> NewsletterCategories { get; set; } = new();
}

/// <summary>
/// Newsletter category for organization.
/// </summary>
public class NewsletterCategory
{
    /// <summary>
    /// Gets or sets the category code name.
    /// </summary>
    public string CodeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the display order.
    /// </summary>
    public int Order { get; set; }
}

/// <summary>
/// Email format preference (duplicated from interfaces for configuration).
/// </summary>
public enum EmailFormat
{
    Html,
    PlainText,
    Both
}
