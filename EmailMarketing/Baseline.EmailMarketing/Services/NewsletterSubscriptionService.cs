using Baseline.EmailMarketing.Configuration;
using Baseline.EmailMarketing.Interfaces;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.EmailMarketing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.EmailMarketing.Services;

/// <summary>
/// Manages newsletter subscriptions via XbK recipient list APIs.
/// A "newsletter" maps to a <see cref="ContactGroupInfo"/> where
/// <see cref="ContactGroupInfo.ContactGroupIsRecipientList"/> is true.
/// </summary>
public class NewsletterSubscriptionService(
    IInfoProvider<ContactInfo> contactInfoProvider,
    IInfoProvider<ContactGroupInfo> contactGroupInfoProvider,
    IInfoProvider<ContactGroupMemberInfo> contactGroupMemberInfoProvider,
    IInfoProvider<EmailSubscriptionConfirmationInfo> subscriptionConfirmationInfoProvider,
    IOptions<BaselineEmailMarketingOptions> options,
    ILogger<NewsletterSubscriptionService> logger) : INewsletterSubscriptionService
{
    /// <inheritdoc />
    public async Task<SubscriptionResult> SubscribeAsync(
        string email,
        string newsletterCodeName,
        string? firstName = null,
        string? lastName = null)
    {
        try
        {
            var contact = await GetOrCreateContactAsync(email, firstName, lastName);
            var recipientList = await GetRecipientListAsync(newsletterCodeName);

            if (recipientList is null)
            {
                logger.LogWarning("Recipient list {List} not found", newsletterCodeName);
                return new SubscriptionResult(false, SubscriptionStatus.NotSubscribed,
                    "Newsletter not found", false);
            }

            // Check existing membership
            bool alreadyMember = await IsContactInGroupAsync(contact.ContactID, recipientList.ContactGroupID);
            if (alreadyMember)
            {
                return new SubscriptionResult(true, SubscriptionStatus.Subscribed,
                    "Already subscribed", false);
            }

            // Add contact to recipient list
            var member = new ContactGroupMemberInfo
            {
                ContactGroupMemberRelatedID = contact.ContactID,
                ContactGroupMemberType = ContactGroupMemberTypeEnum.Contact,
                ContactGroupMemberContactGroupID = recipientList.ContactGroupID,
                ContactGroupMemberFromManual = true,
            };
            contactGroupMemberInfoProvider.Set(member);

            // Create subscription confirmation
            var confirmation = new EmailSubscriptionConfirmationInfo
            {
                EmailSubscriptionConfirmationContactID = contact.ContactID,
                EmailSubscriptionConfirmationRecipientListID = recipientList.ContactGroupID,
                EmailSubscriptionConfirmationIsApproved = !options.Value.EnableDoubleOptIn,
                EmailSubscriptionConfirmationDate = DateTime.Now,
            };
            subscriptionConfirmationInfoProvider.Set(confirmation);

            logger.LogInformation("Contact {Email} subscribed to {List}", email, newsletterCodeName);

            return new SubscriptionResult(
                Success: true,
                Status: options.Value.EnableDoubleOptIn
                    ? SubscriptionStatus.Pending
                    : SubscriptionStatus.Subscribed,
                Message: options.Value.EnableDoubleOptIn
                    ? "Please check your email to confirm your subscription"
                    : "Successfully subscribed",
                RequiresConfirmation: options.Value.EnableDoubleOptIn);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error subscribing {Email} to {List}", email, newsletterCodeName);
            return new SubscriptionResult(false, SubscriptionStatus.NotSubscribed,
                "An error occurred while subscribing", false);
        }
    }

    /// <inheritdoc />
    public async Task<UnsubscriptionResult> UnsubscribeAsync(string email, string newsletterCodeName)
    {
        try
        {
            var contact = await GetContactByEmailAsync(email);
            if (contact is null)
            {
                return new UnsubscriptionResult(true, "No subscription found");
            }

            var recipientList = await GetRecipientListAsync(newsletterCodeName);
            if (recipientList is null)
            {
                return new UnsubscriptionResult(false, "Newsletter not found");
            }

            // Remove contact group membership
            var member = (await contactGroupMemberInfoProvider.Get()
                .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID), contact.ContactID)
                .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberContactGroupID), recipientList.ContactGroupID)
                .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberType), ContactGroupMemberTypeEnum.Contact)
                .GetEnumerableTypedResultAsync()).FirstOrDefault();

            if (member is not null)
            {
                contactGroupMemberInfoProvider.Delete(member);
            }

            // Mark subscription confirmation as not approved
            var confirmation = (await subscriptionConfirmationInfoProvider.Get()
                .WhereEquals(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationContactID), contact.ContactID)
                .WhereEquals(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationRecipientListID), recipientList.ContactGroupID)
                .GetEnumerableTypedResultAsync()).FirstOrDefault();

            if (confirmation is not null)
            {
                confirmation.EmailSubscriptionConfirmationIsApproved = false;
                subscriptionConfirmationInfoProvider.Set(confirmation);
            }

            logger.LogInformation("Contact {Email} unsubscribed from {List}", email, newsletterCodeName);
            return new UnsubscriptionResult(true, "Successfully unsubscribed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error unsubscribing {Email} from {List}", email, newsletterCodeName);
            return new UnsubscriptionResult(false, "An error occurred while unsubscribing");
        }
    }

    /// <inheritdoc />
    public async Task<int> UnsubscribeFromAllAsync(string email)
    {
        var contact = await GetContactByEmailAsync(email);
        if (contact is null)
        {
            return 0;
        }

        // Find all recipient-list memberships for this contact
        var memberships = await contactGroupMemberInfoProvider.Get()
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID), contact.ContactID)
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberType), ContactGroupMemberTypeEnum.Contact)
            .GetEnumerableTypedResultAsync();

        int count = 0;
        foreach (var m in memberships)
        {
            // Verify the group is actually a recipient list
            var group = await contactGroupInfoProvider.GetAsync(m.ContactGroupMemberContactGroupID);
            if (group?.ContactGroupIsRecipientList != true)
            {
                continue;
            }

            contactGroupMemberInfoProvider.Delete(m);

            // Mark confirmation as not approved
            var confirmation = (await subscriptionConfirmationInfoProvider.Get()
                .WhereEquals(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationContactID), contact.ContactID)
                .WhereEquals(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationRecipientListID), group.ContactGroupID)
                .GetEnumerableTypedResultAsync()).FirstOrDefault();

            if (confirmation is not null)
            {
                confirmation.EmailSubscriptionConfirmationIsApproved = false;
                subscriptionConfirmationInfoProvider.Set(confirmation);
            }

            count++;
        }

        logger.LogInformation("Contact {Email} unsubscribed from {Count} lists", email, count);
        return count;
    }

    /// <inheritdoc />
    public async Task<bool> IsSubscribedAsync(string email, string newsletterCodeName)
    {
        var contact = await GetContactByEmailAsync(email);
        if (contact is null)
        {
            return false;
        }

        var recipientList = await GetRecipientListAsync(newsletterCodeName);
        if (recipientList is null)
        {
            return false;
        }

        return await IsContactInGroupAsync(contact.ContactID, recipientList.ContactGroupID);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetSubscribedNewslettersAsync(string email)
    {
        var contact = await GetContactByEmailAsync(email);
        if (contact is null)
        {
            return [];
        }

        var memberships = await contactGroupMemberInfoProvider.Get()
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID), contact.ContactID)
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberType), ContactGroupMemberTypeEnum.Contact)
            .GetEnumerableTypedResultAsync();

        var result = new List<string>();
        foreach (var m in memberships)
        {
            var group = await contactGroupInfoProvider.GetAsync(m.ContactGroupMemberContactGroupID);
            if (group?.ContactGroupIsRecipientList == true)
            {
                result.Add(group.ContactGroupName);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ConfirmationResult> ConfirmSubscriptionAsync(string confirmationHash)
    {
        // Double opt-in confirmation: hash encodes contactId:recipientListId
        // Format: base64("{contactId}:{recipientListId}")
        try
        {
            string decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(confirmationHash));
            string[] parts = decoded.Split(':');

            if (parts.Length != 2 ||
                !int.TryParse(parts[0], out int contactId) ||
                !int.TryParse(parts[1], out int recipientListId))
            {
                return new ConfirmationResult(false, null, null, "Invalid confirmation link");
            }

            var confirmation = (await subscriptionConfirmationInfoProvider.Get()
                .WhereEquals(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationContactID), contactId)
                .WhereEquals(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationRecipientListID), recipientListId)
                .GetEnumerableTypedResultAsync()).FirstOrDefault();

            if (confirmation is null)
            {
                return new ConfirmationResult(false, null, null, "Subscription not found");
            }

            confirmation.EmailSubscriptionConfirmationIsApproved = true;
            confirmation.EmailSubscriptionConfirmationDate = DateTime.Now;
            subscriptionConfirmationInfoProvider.Set(confirmation);

            var contact = await contactInfoProvider.GetAsync(contactId);
            var group = await contactGroupInfoProvider.GetAsync(recipientListId);

            logger.LogInformation("Subscription confirmed for contact {ContactId} to list {ListId}",
                contactId, recipientListId);

            return new ConfirmationResult(
                Success: true,
                Email: contact?.ContactEmail,
                NewsletterCodeName: group?.ContactGroupName,
                Message: "Subscription confirmed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error confirming subscription with hash {Hash}", confirmationHash);
            return new ConfirmationResult(false, null, null, "An error occurred during confirmation");
        }
    }

    // ── Helpers ───────────────────────────────────────────────

    private async Task<ContactInfo> GetOrCreateContactAsync(string email, string? firstName, string? lastName)
    {
        var contact = await GetContactByEmailAsync(email);
        if (contact is not null)
        {
            return contact;
        }

        contact = new ContactInfo
        {
            ContactEmail = email,
            ContactFirstName = firstName ?? string.Empty,
            ContactLastName = lastName ?? string.Empty,
        };
        contactInfoProvider.Set(contact);
        logger.LogInformation("Created contact for {Email}", email);
        return contact;
    }

    private async Task<ContactInfo?> GetContactByEmailAsync(string email) =>
        (await contactInfoProvider.Get()
            .WhereEquals(nameof(ContactInfo.ContactEmail), email)
            .TopN(1)
            .GetEnumerableTypedResultAsync()).FirstOrDefault();

    private async Task<ContactGroupInfo?> GetRecipientListAsync(string codeName) =>
        (await contactGroupInfoProvider.Get()
            .WhereEquals(nameof(ContactGroupInfo.ContactGroupName), codeName)
            .WhereTrue(nameof(ContactGroupInfo.ContactGroupIsRecipientList))
            .TopN(1)
            .GetEnumerableTypedResultAsync()).FirstOrDefault();

    private async Task<bool> IsContactInGroupAsync(int contactId, int groupId) =>
        (await contactGroupMemberInfoProvider.Get()
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID), contactId)
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberContactGroupID), groupId)
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberType), ContactGroupMemberTypeEnum.Contact)
            .GetEnumerableTypedResultAsync()).Any();
}
