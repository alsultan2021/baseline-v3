using CMS.ContentEngine;
using CMS.DataEngine;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using XperienceCommunity.ChannelSettings.Admin.UI.ChannelCustomSettings;
using XperienceCommunity.ChannelSettings.Repositories;

[assembly: UIPage(
    parentType: typeof(Kentico.Xperience.Admin.Base.UIPages.ChannelEditSection),
    slug: "commerce-channel-settings",
    uiPageType: typeof(Baseline.Ecommerce.Admin.CommerceChannelSettingsExtender),
    name: "Commerce Settings",
    templateName: TemplateNames.EDIT,
    order: 500)]

namespace Baseline.Ecommerce.Admin;

/// <summary>
/// Admin UI page for configuring Commerce channel settings.
/// Provides page path configuration for store, cart, checkout, and account pages.
/// </summary>
public class CommerceChannelSettingsExtender(
    Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IChannelCustomSettingsRepository customChannelSettingsRepository,
    IChannelSettingsInternalHelper channelCustomSettingsInfoHandler,
    IInfoProvider<ChannelInfo> channelInfoProvider)
    : ChannelCustomSettingsPage<CommerceChannelSettings>(
        formItemCollectionProvider,
        formDataBinder,
        customChannelSettingsRepository,
        channelCustomSettingsInfoHandler,
        channelInfoProvider)
{
}
