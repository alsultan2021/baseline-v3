using CMS.ContentEngine;
using CMS.DataEngine;
using Baseline.TabbedPages.Admin.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using XperienceCommunity.ChannelSettings.Admin.UI.ChannelCustomSettings;
using XperienceCommunity.ChannelSettings.Repositories;

[assembly: UIPage(
    parentType: typeof(Kentico.Xperience.Admin.Base.UIPages.ChannelEditSection),
    slug: "tabbed-pages-settings",
    uiPageType: typeof(Baseline.TabbedPages.Admin.TabbedPagesChannelSettingsExtender),
    name: "Tabbed Pages",
    templateName: TemplateNames.EDIT,
    order: 200)]

namespace Baseline.TabbedPages.Admin;

/// <summary>
/// Admin UI page for configuring TabbedPages channel settings.
/// Provides tab rendering, behavior, SEO, and caching configuration per channel.
/// </summary>
public class TabbedPagesChannelSettingsExtender(
    Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IChannelCustomSettingsRepository customChannelSettingsRepository,
    IChannelSettingsInternalHelper channelCustomSettingsInfoHandler,
    IInfoProvider<ChannelInfo> channelInfoProvider)
    : ChannelCustomSettingsPage<TabbedPagesChannelSettings>(
        formItemCollectionProvider,
        formDataBinder,
        customChannelSettingsRepository,
        channelCustomSettingsInfoHandler,
        channelInfoProvider)
{
}
