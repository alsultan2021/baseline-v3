using CMS.ContentEngine;
using CMS.DataEngine;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Admin.Base.Forms.Internal;
using XperienceCommunity.ChannelSettings.Admin.UI.ChannelCustomSettings;
using XperienceCommunity.ChannelSettings.Repositories;

namespace Account.Admin.Xperience.Models;

/// <summary>
/// Type alias for MemberPasswordChannelSettingsExtender.
/// Points to the Baseline.Account.Admin implementation.
/// </summary>
public class MemberPasswordChannelSettingsExtender(
    IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IChannelCustomSettingsRepository customChannelSettingsRepository,
    IChannelSettingsInternalHelper channelCustomSettingsInfoHandler,
    IInfoProvider<ChannelInfo> channelInfoProvider)
    : Baseline.Account.Admin.MemberPasswordChannelSettingsExtender(
        formItemCollectionProvider,
        formDataBinder,
        customChannelSettingsRepository,
        channelCustomSettingsInfoHandler,
        channelInfoProvider)
{
}
