using CMS.ContentEngine;
using CMS.DataEngine;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Baseline.AI.Admin.DataProviders;

/// <summary>
/// Dropdown options provider for website channels.
/// Used in KB path forms to select which channel to index.
/// </summary>
internal class WebsiteChannelOptionsProvider(
    IInfoProvider<ChannelInfo> channelProvider)
    : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
        Task.FromResult(
            channelProvider.Get()
                .WhereEquals(nameof(ChannelInfo.ChannelType), ChannelType.Website.ToString())
                .GetEnumerableTypedResult()
                .Select(c => new DropDownOptionItem
                {
                    Value = c.ChannelName,
                    Text = c.ChannelDisplayName
                }));
}
