using XperienceCommunity.ChannelSettings.Attributes;

namespace Baseline.Account.Models;

/// <summary>
/// Channel settings for Account module URLs.
/// Configure web page paths for account-related pages.
/// </summary>
public class AccountChannelSettings
{
    /// <summary>
    /// If the user should be directed to the "My Account" page upon login (unless logging in after hitting a restricted page).
    /// </summary>
    [XperienceSettingsData("Account.RedirectToAccountAfterLogin", false)]
    public virtual bool AccountRedirectToAccountAfterLogin { get; set; } = false;

    /// <summary>
    /// Url to the Account page with "Registration" template.
    /// </summary>
    [XperienceSettingsData("Account.RegistrationUrl", "/Account/Registration")]
    public virtual string AccountRegistrationUrl { get; set; } = "/Account/Registration";

    /// <summary>
    /// Url to the Account page with "Registration Confirmation" template.
    /// </summary>
    [XperienceSettingsData("Account.ConfirmationUrl", "/Account/Confirmation")]
    public virtual string AccountConfirmationUrl { get; set; } = "/Account/Confirmation";

    /// <summary>
    /// Url to the Account page with "Log In" template.
    /// </summary>
    [XperienceSettingsData("Account.LoginUrl", "/Account/LogIn")]
    public virtual string AccountLoginUrl { get; set; } = "/Account/LogIn";

    /// <summary>
    /// Url to the Account page with "Two Form Authentication" template.
    /// </summary>
    [XperienceSettingsData("Account.TwoFormAuthenticationUrl", "/Account/TwoFormAuthentication")]
    public virtual string AccountTwoFormAuthenticationUrl { get; set; } = "/Account/TwoFormAuthentication";

    /// <summary>
    /// Url to the Account page with "My Account" template.
    /// </summary>
    [XperienceSettingsData("Account.MyAccountUrl", "/Account/MyAccount")]
    public virtual string AccountMyAccountUrl { get; set; } = "/Account/MyAccount";

    /// <summary>
    /// Url to the Account page with "Reset Password" template.
    /// </summary>
    [XperienceSettingsData("Account.ResetPassword", "/Account/ResetPassword")]
    public virtual string AccountResetPassword { get; set; } = "/Account/ResetPassword";

    /// <summary>
    /// Url to the Account page with "Log Out" template.
    /// </summary>
    [XperienceSettingsData("Account.LogOutUrl", "/Account/LogOut")]
    public virtual string AccountLogOutUrl { get; set; } = "/Account/LogOut";

    /// <summary>
    /// Url to the Account page with "Forgot Password" template.
    /// </summary>
    [XperienceSettingsData("Account.ForgotPasswordUrl", "/Account/ForgotPassword")]
    public virtual string AccountForgotPasswordUrl { get; set; } = "/Account/ForgotPassword";

    /// <summary>
    /// Url to the Account page with "Forgotten Password Reset" template.
    /// </summary>
    [XperienceSettingsData("Account.ForgottenPasswordResetUrl", "/Account/ForgottenPasswordReset")]
    public virtual string AccountForgottenPasswordResetUrl { get; set; } = "/Account/ForgottenPasswordReset";

    /// <summary>
    /// Url when someone is logged in but is denied access to the page.
    /// </summary>
    [XperienceSettingsData("Account.AccessDeniedUrl", "/error/403")]
    public virtual string AccessDeniedUrl { get; set; } = "/error/403";
}

/// <summary>
/// Channel settings for Member Password policies.
/// </summary>
public class MemberPasswordChannelSettings
{
    /// <summary>
    /// If checked, each new password defined anywhere in the system will be checked against the specified password policy rules.
    /// </summary>
    [XperienceSettingsData("MemberPassword.UsePasswordPolicy", false)]
    public virtual bool UsePasswordPolicy { get; set; } = false;

    /// <summary>
    /// Allowed length (in characters) of the user password.
    /// </summary>
    [XperienceSettingsData("MemberPassword.MinLength", 8)]
    public virtual int MinLength { get; set; } = 8;

    /// <summary>
    /// Minimal number of non alphanumeric characters which password has to contain.
    /// </summary>
    [XperienceSettingsData("MemberPassword.NumNonAlphanumericChars", 1)]
    public virtual int NumNonAlphanumericChars { get; set; } = 1;

    /// <summary>
    /// If set, passwords input by users need to meet the given regular expression.
    /// </summary>
    [XperienceSettingsData("MemberPassword.Regex", "")]
    public virtual string Regex { get; set; } = string.Empty;

    /// <summary>
    /// Custom message displayed to users who attempt to enter a password which does not fulfill the requirements.
    /// </summary>
    [XperienceSettingsData("MemberPassword.ViolationMessage", "")]
    public virtual string ViolationMessage { get; set; } = string.Empty;
}
