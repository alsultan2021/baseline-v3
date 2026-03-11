using Baseline.DigitalMarketing.Configuration;
using Baseline.DigitalMarketing.Interfaces;
using CMS.ContactManagement;
using CMS.DataEngine;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.DigitalMarketing.Services;

/// <summary>
/// Default implementation of contact group service.
/// </summary>
public class ContactGroupService : IContactGroupService
{
    private readonly IContactTrackingService _contactTrackingService;
    private readonly IInfoProvider<ContactGroupInfo> _contactGroupInfoProvider;
    private readonly IInfoProvider<ContactGroupMemberInfo> _contactGroupMemberInfoProvider;
    private readonly IMemoryCache _cache;
    private readonly IOptions<BaselineDigitalMarketingOptions> _options;
    private readonly ILogger<ContactGroupService> _logger;

    public ContactGroupService(
        IContactTrackingService contactTrackingService,
        IInfoProvider<ContactGroupInfo> contactGroupInfoProvider,
        IInfoProvider<ContactGroupMemberInfo> contactGroupMemberInfoProvider,
        IMemoryCache cache,
        IOptions<BaselineDigitalMarketingOptions> options,
        ILogger<ContactGroupService> logger)
    {
        _contactTrackingService = contactTrackingService;
        _contactGroupInfoProvider = contactGroupInfoProvider;
        _contactGroupMemberInfoProvider = contactGroupMemberInfoProvider;
        _cache = cache;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> IsInContactGroupAsync(string contactGroupCodeName)
    {
        var contact = await _contactTrackingService.GetCurrentContactAsync();
        if (contact == null)
        {
            return false;
        }

        var cacheKey = $"ContactGroup_{contact.ContactID}_{contactGroupCodeName}";

        var result = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.Value.ContactGroupCacheDurationMinutes);
            entry.Size = 1;

            var contactGroup = _contactGroupInfoProvider.Get()
                .WhereEquals(nameof(ContactGroupInfo.ContactGroupName), contactGroupCodeName)
                .FirstOrDefault();

            if (contactGroup == null)
            {
                _logger.LogWarning("Contact group {ContactGroupCodeName} not found", contactGroupCodeName);
                return false;
            }

            var membership = _contactGroupMemberInfoProvider.Get()
                .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberContactGroupID), contactGroup.ContactGroupID)
                .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID), contact.ContactID)
                .FirstOrDefault();

            return membership != null;
        });

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> IsInAnyContactGroupAsync(params string[] contactGroupCodeNames)
    {
        foreach (var codeName in contactGroupCodeNames)
        {
            if (await IsInContactGroupAsync(codeName))
            {
                return true;
            }
        }
        return false;
    }

    /// <inheritdoc />
    public async Task<bool> IsInAllContactGroupsAsync(params string[] contactGroupCodeNames)
    {
        foreach (var codeName in contactGroupCodeNames)
        {
            if (!await IsInContactGroupAsync(codeName))
            {
                return false;
            }
        }
        return true;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetContactGroupsAsync()
    {
        var contact = await _contactTrackingService.GetCurrentContactAsync();
        if (contact == null)
        {
            return Enumerable.Empty<string>();
        }

        var cacheKey = $"ContactGroups_{contact.ContactID}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.Value.ContactGroupCacheDurationMinutes);
            entry.Size = 1;

            var memberships = _contactGroupMemberInfoProvider.Get()
                .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID), contact.ContactID)
                .ToList();

            var groupIds = memberships.Select(m => m.ContactGroupMemberContactGroupID).Distinct();

            var groups = _contactGroupInfoProvider.Get()
                .WhereIn(nameof(ContactGroupInfo.ContactGroupID), groupIds.ToList())
                .ToList();

            return groups.Select(g => g.ContactGroupName).ToList();
        }) ?? Enumerable.Empty<string>();
    }

    /// <inheritdoc />
    public async Task<int> GetContactGroupMemberCountAsync(string contactGroupCodeName)
    {
        var contactGroup = _contactGroupInfoProvider.Get()
            .WhereEquals(nameof(ContactGroupInfo.ContactGroupName), contactGroupCodeName)
            .FirstOrDefault();

        if (contactGroup == null)
        {
            return 0;
        }

        var count = _contactGroupMemberInfoProvider.Get()
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberContactGroupID), contactGroup.ContactGroupID)
            .Count;

        return await Task.FromResult(count);
    }
}
