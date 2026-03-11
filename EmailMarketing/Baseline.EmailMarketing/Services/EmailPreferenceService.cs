using Baseline.EmailMarketing.Configuration;
using Baseline.EmailMarketing.Interfaces;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.EmailMarketing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.EmailMarketing.Services;

/// <summary>
/// Manages email preferences: per-list subscription toggles and global unsubscribe.
/// </summary>
public class EmailPreferenceService(
    INewsletterSubscriptionService subscriptionService,
    INewsletterRetrievalService retrievalService,
    IInfoProvider<ContactInfo> contactInfoProvider,
    IInfoProvider<EmailSubscriptionConfirmationInfo> confirmationInfoProvider,
    IOptions<BaselineEmailMarketingOptions> options,
    ILogger<EmailPreferenceService> logger) : IEmailPreferenceService
{
    /// <inheritdoc />
    public async Task<EmailPreferences> GetPreferencesAsync(string email)
    {
        var subscribed = await subscriptionService.GetSubscribedNewslettersAsync(email);
        bool globalUnsub = await IsGloballyUnsubscribedAsync(email);

        return new EmailPreferences(
            Email: email,
            SubscribedNewsletters: subscribed,
            EmailFormat: (Interfaces.EmailFormat)options.Value.DefaultEmailFormat,
            GlobalUnsubscribe: globalUnsub);
    }

    /// <inheritdoc />
    public async Task UpdatePreferencesAsync(string email, EmailPreferences preferences)
    {
        if (preferences.GlobalUnsubscribe)
        {
            await subscriptionService.UnsubscribeFromAllAsync(email);
            logger.LogInformation("Global unsubscribe applied for {Email}", email);
            return;
        }

        var allNewsletters = await retrievalService.GetAllNewslettersAsync();
        var currentlySubscribed = (await subscriptionService.GetSubscribedNewslettersAsync(email)).ToHashSet();
        var targetSubscribed = preferences.SubscribedNewsletters.ToHashSet();

        foreach (var newsletter in allNewsletters)
        {
            bool isSubbed = currentlySubscribed.Contains(newsletter.CodeName);
            bool shouldBe = targetSubscribed.Contains(newsletter.CodeName);

            if (!isSubbed && shouldBe)
            {
                await subscriptionService.SubscribeAsync(email, newsletter.CodeName);
            }
            else if (isSubbed && !shouldBe)
            {
                await subscriptionService.UnsubscribeAsync(email, newsletter.CodeName);
            }
        }

        logger.LogInformation("Preferences updated for {Email}", email);
    }

    /// <inheritdoc />
    public Task<string?> GetPreferenceCenterUrlAsync(string email)
    {
        string? path = options.Value.PreferenceCenterPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.FromResult<string?>(null);
        }

        return Task.FromResult<string?>($"{path}?email={Uri.EscapeDataString(email)}");
    }

    private async Task<bool> IsGloballyUnsubscribedAsync(string email)
    {
        var contact = (await contactInfoProvider.Get()
            .WhereEquals(nameof(ContactInfo.ContactEmail), email)
            .TopN(1)
            .GetEnumerableTypedResultAsync()).FirstOrDefault();

        if (contact is null)
        {
            return false;
        }

        // Check if there are any active (approved) subscription confirmations
        var anyApproved = (await confirmationInfoProvider.Get()
            .WhereEquals(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationContactID), contact.ContactID)
            .WhereTrue(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationIsApproved))
            .TopN(1)
            .GetEnumerableTypedResultAsync()).Any();

        // If no approved subscriptions, contact is effectively globally unsubscribed
        return !anyApproved;
    }
}
