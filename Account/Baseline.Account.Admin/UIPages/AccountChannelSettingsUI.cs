using Baseline.Account.Models;
using CMS.ContentEngine;
using CMS.DataEngine;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Admin.Websites.FormAnnotations;
using XperienceCommunity.ChannelSettings.Admin.UI.ChannelCustomSettings;
using XperienceCommunity.ChannelSettings.Repositories;

[assembly: UIPage(parentType: typeof(Kentico.Xperience.Admin.Base.UIPages.ChannelEditSection),
    slug: "account-channel-custom-settings",
    uiPageType: typeof(Baseline.Account.Admin.AccountChannelSettingsExtender),
    name: "Account URL Settings",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Account.Admin;

/// <summary>
/// Admin UI page for configuring Account channel settings.
/// </summary>
public class AccountChannelSettingsExtender(
    Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IChannelCustomSettingsRepository customChannelSettingsRepository,
    IChannelSettingsInternalHelper channelCustomSettingsInfoHandler,
    IInfoProvider<ChannelInfo> channelInfoProvider)
    : ChannelCustomSettingsPage<AccountChannelSettingsFormAnnotated>(
        formItemCollectionProvider,
        formDataBinder,
        customChannelSettingsRepository,
        channelCustomSettingsInfoHandler,
        channelInfoProvider)
{
}

/// <summary>
/// Form-annotated version of AccountChannelSettings for admin UI.
/// Uses UrlSelectorComponent to allow page selection from content tree.
/// </summary>
public class AccountChannelSettingsFormAnnotated : AccountChannelSettings
{
    [CheckBoxComponent(Label = "Redirect To Account After Login", Order = 1, ExplanationText = "If the user should be directed to the \"My Account\" page upon login (unless logging in after hitting a restricted page).")]
    public override bool AccountRedirectToAccountAfterLogin { get; set; } = false;

    [UrlSelectorComponent(Label = "Registration URL", Order = 2, ExplanationText = "Select the registration page from the content tree or enter a URL manually")]
    public override string AccountRegistrationUrl { get; set; } = string.Empty;

    [UrlSelectorComponent(Label = "Registration Confirmation URL", Order = 3, ExplanationText = "Select the registration confirmation page from the content tree or enter a URL manually")]
    public override string AccountConfirmationUrl { get; set; } = string.Empty;

    [UrlSelectorComponent(Label = "Log In URL", Order = 4, ExplanationText = "Select the login page from the content tree or enter a URL manually")]
    public override string AccountLoginUrl { get; set; } = string.Empty;

    [UrlSelectorComponent(Label = "Two Factor Authentication URL", Order = 5, ExplanationText = "Select the two-factor authentication page from the content tree or enter a URL manually")]
    public override string AccountTwoFormAuthenticationUrl { get; set; } = string.Empty;

    [UrlSelectorComponent(Label = "My Account URL", Order = 6, ExplanationText = "Select the My Account page from the content tree or enter a URL manually")]
    public override string AccountMyAccountUrl { get; set; } = string.Empty;

    [UrlSelectorComponent(Label = "Reset Password URL", Order = 7, ExplanationText = "Select the reset password page from the content tree or enter a URL manually")]
    public override string AccountResetPassword { get; set; } = string.Empty;

    [UrlSelectorComponent(Label = "Log Out URL", Order = 8, ExplanationText = "Select the logout page from the content tree or enter a URL manually")]
    public override string AccountLogOutUrl { get; set; } = string.Empty;

    [UrlSelectorComponent(Label = "Forgot Password URL", Order = 9, ExplanationText = "Select the forgot password page from the content tree or enter a URL manually")]
    public override string AccountForgotPasswordUrl { get; set; } = string.Empty;

    [UrlSelectorComponent(Label = "Forgotten Password Reset URL", Order = 10, ExplanationText = "Select the password reset page from the content tree or enter a URL manually")]
    public override string AccountForgottenPasswordResetUrl { get; set; } = string.Empty;

    [UrlSelectorComponent(Label = "Access Denied URL", Order = 11, ExplanationText = "Select the access denied/403 page from the content tree or enter a URL manually")]
    public override string AccessDeniedUrl { get; set; } = string.Empty;
}
