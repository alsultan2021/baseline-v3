using CMS.ContentEngine;
using CMS.DataEngine;
using Baseline.Core.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using XperienceCommunity.ChannelSettings.Admin.UI.ChannelCustomSettings;
using XperienceCommunity.ChannelSettings.Repositories;

[assembly: UIPage(
    parentType: typeof(Kentico.Xperience.Admin.Base.UIPages.ChannelEditSection),
    slug: "core-channel-settings",
    uiPageType: typeof(Baseline.Core.Admin.CoreChannelSettingsExtender),
    name: "Core Settings",
    templateName: TemplateNames.EDIT,
    order: 100)]

namespace Baseline.Core.Admin;

/// <summary>
/// Admin UI page for configuring Baseline Core channel settings.
/// Provides CDN, caching, security headers, and SEO configuration per channel.
/// </summary>
public class CoreChannelSettingsExtender(
    Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IChannelCustomSettingsRepository customChannelSettingsRepository,
    IChannelSettingsInternalHelper channelCustomSettingsInfoHandler,
    IInfoProvider<ChannelInfo> channelInfoProvider)
    : ChannelCustomSettingsPage<CoreChannelSettings>(
        formItemCollectionProvider,
        formDataBinder,
        customChannelSettingsRepository,
        channelCustomSettingsInfoHandler,
        channelInfoProvider)
{
}
