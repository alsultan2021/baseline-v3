using Baseline.EmailMarketing.Configuration;
using Baseline.EmailMarketing.Interfaces;
using CMS.ContactManagement;
using CMS.DataEngine;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.EmailMarketing.Services;

/// <summary>
/// Retrieves recipient lists (newsletters) from XbK contact groups
/// where <see cref="ContactGroupInfo.ContactGroupIsRecipientList"/> is true.
/// </summary>
public class NewsletterRetrievalService(
    IInfoProvider<ContactGroupInfo> contactGroupInfoProvider,
    IMemoryCache cache,
    IOptions<BaselineEmailMarketingOptions> options,
    ILogger<NewsletterRetrievalService> logger) : INewsletterRetrievalService
{
    private const string CacheKeyAllNewsletters = "baseline_newsletters_all";

    /// <inheritdoc />
    public async Task<IEnumerable<NewsletterSummary>> GetAllNewslettersAsync()
    {
        return await cache.GetOrCreateAsync(CacheKeyAllNewsletters, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            entry.Size = 1;
            return await FetchAllRecipientListsAsync();
        }) ?? [];
    }

    private async Task<IEnumerable<NewsletterSummary>> FetchAllRecipientListsAsync()
    {
        var recipientLists = await contactGroupInfoProvider.Get()
            .WhereTrue(nameof(ContactGroupInfo.ContactGroupIsRecipientList))
            .GetEnumerableTypedResultAsync();

        var summaries = recipientLists.Select(g => new NewsletterSummary(
            CodeName: g.ContactGroupName,
            DisplayName: g.ContactGroupDisplayName,
            Description: g.ContactGroupDescription,
            SubscriptionType: options.Value.EnableDoubleOptIn
                ? SubscriptionType.DoubleOptIn
                : SubscriptionType.OptIn,
            Frequency: null,
            Category: null,
            IsActive: g.ContactGroupEnabled
        )).ToList();

        logger.LogDebug("Retrieved {Count} recipient lists from XbK", summaries.Count);
        return summaries;
    }

    /// <inheritdoc />
    public async Task<NewsletterSummary?> GetNewsletterAsync(string codeName)
    {
        var newsletters = await GetAllNewslettersAsync();
        return newsletters.FirstOrDefault(n =>
            n.CodeName.Equals(codeName, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NewsletterSummary>> GetNewslettersByCategoryAsync(string category)
    {
        var newsletters = await GetAllNewslettersAsync();
        return newsletters.Where(n =>
            n.Category?.Equals(category, StringComparison.OrdinalIgnoreCase) == true);
    }
}
