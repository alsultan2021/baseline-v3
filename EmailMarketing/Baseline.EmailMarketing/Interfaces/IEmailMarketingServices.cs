namespace Baseline.EmailMarketing.Interfaces;

/// <summary>
/// Service for managing newsletter subscriptions.
/// </summary>
public interface INewsletterSubscriptionService
{
    /// <summary>
    /// Subscribes an email to a newsletter.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="newsletterCodeName">The newsletter code name.</param>
    /// <param name="firstName">Optional first name.</param>
    /// <param name="lastName">Optional last name.</param>
    /// <returns>Subscription result.</returns>
    Task<SubscriptionResult> SubscribeAsync(
        string email, 
        string newsletterCodeName, 
        string? firstName = null, 
        string? lastName = null);

    /// <summary>
    /// Unsubscribes an email from a newsletter.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="newsletterCodeName">The newsletter code name.</param>
    /// <returns>Unsubscription result.</returns>
    Task<UnsubscriptionResult> UnsubscribeAsync(string email, string newsletterCodeName);

    /// <summary>
    /// Unsubscribes an email from all newsletters.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <returns>Number of newsletters unsubscribed from.</returns>
    Task<int> UnsubscribeFromAllAsync(string email);

    /// <summary>
    /// Checks if an email is subscribed to a newsletter.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="newsletterCodeName">The newsletter code name.</param>
    /// <returns>True if subscribed.</returns>
    Task<bool> IsSubscribedAsync(string email, string newsletterCodeName);

    /// <summary>
    /// Gets all newsletters a contact is subscribed to.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <returns>Collection of newsletter code names.</returns>
    Task<IEnumerable<string>> GetSubscribedNewslettersAsync(string email);

    /// <summary>
    /// Confirms a subscription (double opt-in).
    /// </summary>
    /// <param name="confirmationHash">The confirmation hash from email link.</param>
    /// <returns>Confirmation result.</returns>
    Task<ConfirmationResult> ConfirmSubscriptionAsync(string confirmationHash);
}

/// <summary>
/// Service for retrieving newsletter information.
/// </summary>
public interface INewsletterRetrievalService
{
    /// <summary>
    /// Gets all available newsletters.
    /// </summary>
    /// <returns>Collection of newsletter summaries.</returns>
    Task<IEnumerable<NewsletterSummary>> GetAllNewslettersAsync();

    /// <summary>
    /// Gets a newsletter by code name.
    /// </summary>
    /// <param name="codeName">The newsletter code name.</param>
    /// <returns>Newsletter summary or null.</returns>
    Task<NewsletterSummary?> GetNewsletterAsync(string codeName);

    /// <summary>
    /// Gets newsletters in a specific category.
    /// </summary>
    /// <param name="category">The category name.</param>
    /// <returns>Collection of newsletter summaries.</returns>
    Task<IEnumerable<NewsletterSummary>> GetNewslettersByCategoryAsync(string category);
}

/// <summary>
/// Service for managing email preferences.
/// </summary>
public interface IEmailPreferenceService
{
    /// <summary>
    /// Gets email preferences for a contact.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <returns>Email preferences.</returns>
    Task<EmailPreferences> GetPreferencesAsync(string email);

    /// <summary>
    /// Updates email preferences for a contact.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="preferences">The new preferences.</param>
    Task UpdatePreferencesAsync(string email, EmailPreferences preferences);

    /// <summary>
    /// Gets the preference center URL for a contact.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <returns>The preference center URL.</returns>
    Task<string?> GetPreferenceCenterUrlAsync(string email);
}

/// <summary>
/// Subscription result.
/// </summary>
/// <param name="Success">Whether subscription succeeded.</param>
/// <param name="Status">Current subscription status.</param>
/// <param name="Message">Result message.</param>
/// <param name="RequiresConfirmation">Whether confirmation email was sent.</param>
public record SubscriptionResult(
    bool Success,
    SubscriptionStatus Status,
    string? Message,
    bool RequiresConfirmation
);

/// <summary>
/// Unsubscription result.
/// </summary>
/// <param name="Success">Whether unsubscription succeeded.</param>
/// <param name="Message">Result message.</param>
public record UnsubscriptionResult(
    bool Success,
    string? Message
);

/// <summary>
/// Confirmation result.
/// </summary>
/// <param name="Success">Whether confirmation succeeded.</param>
/// <param name="Email">The confirmed email.</param>
/// <param name="NewsletterCodeName">The confirmed newsletter.</param>
/// <param name="Message">Result message.</param>
public record ConfirmationResult(
    bool Success,
    string? Email,
    string? NewsletterCodeName,
    string? Message
);

/// <summary>
/// Newsletter summary.
/// </summary>
/// <param name="CodeName">Newsletter code name.</param>
/// <param name="DisplayName">Display name.</param>
/// <param name="Description">Description.</param>
/// <param name="SubscriptionType">Type of subscription.</param>
/// <param name="Frequency">Email frequency.</param>
/// <param name="Category">Category.</param>
/// <param name="IsActive">Whether the newsletter is active.</param>
public record NewsletterSummary(
    string CodeName,
    string DisplayName,
    string? Description,
    SubscriptionType SubscriptionType,
    string? Frequency,
    string? Category,
    bool IsActive
);

/// <summary>
/// Email preferences.
/// </summary>
/// <param name="Email">The email address.</param>
/// <param name="SubscribedNewsletters">Subscribed newsletter code names.</param>
/// <param name="EmailFormat">Preferred email format.</param>
/// <param name="GlobalUnsubscribe">Whether globally unsubscribed.</param>
public record EmailPreferences(
    string Email,
    IEnumerable<string> SubscribedNewsletters,
    EmailFormat EmailFormat,
    bool GlobalUnsubscribe
);

/// <summary>
/// Subscription status.
/// </summary>
public enum SubscriptionStatus
{
    NotSubscribed,
    Pending,
    Subscribed,
    Unsubscribed
}

/// <summary>
/// Subscription type.
/// </summary>
public enum SubscriptionType
{
    OptIn,
    DoubleOptIn
}

/// <summary>
/// Email format preference.
/// </summary>
public enum EmailFormat
{
    Html,
    PlainText,
    Both
}
