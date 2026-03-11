using Microsoft.Extensions.Options;

namespace Baseline.Account;

/// <summary>
/// v3 implementation of IAccountSettingsRepository.
/// Retrieves account-related settings from configuration options.
/// </summary>
public sealed class AccountSettingsRepository(
    IOptions<BaselineAccountOptions> accountOptions) : IAccountSettingsRepository
{
    private readonly BaselineAccountOptions _options = accountOptions.Value;

    /// <inheritdoc/>
    public Task<AccountSettings> GetSettingsAsync()
    {
        return Task.FromResult(new AccountSettings
        {
            RegistrationEnabled = _options.EnableRegistration,
            EmailConfirmationRequired = _options.RequireEmailConfirmation,
            PasswordRecoveryEnabled = _options.EnablePasswordRecovery,
            LoginPageUrl = _options.Urls.LoginUrl,
            RegistrationPageUrl = _options.Urls.RegistrationUrl,
            ProfilePageUrl = _options.Urls.ProfileUrl,
            ForgotPasswordPageUrl = _options.Urls.ForgotPasswordUrl,
            ResetPasswordPageUrl = _options.Urls.ResetPasswordUrl,
            ConfirmationPageUrl = _options.Urls.ConfirmationUrl,
            AfterLoginRedirectUrl = _options.Urls.AfterLoginRedirectUrl,
            AfterRegistrationRedirectUrl = _options.Urls.AfterRegistrationRedirectUrl
        });
    }

    /// <inheritdoc/>
    public Task<bool> IsRegistrationEnabledAsync()
        => Task.FromResult(_options.EnableRegistration);

    /// <inheritdoc/>
    public Task<bool> IsEmailConfirmationRequiredAsync()
        => Task.FromResult(_options.RequireEmailConfirmation);

    /// <inheritdoc/>
    public Task<string> GetAccountLoginUrlAsync(string defaultUrl)
        => Task.FromResult(_options.Urls.LoginUrl ?? defaultUrl);

    /// <inheritdoc/>
    public Task<string> GetAccountRegistrationUrlAsync(string defaultUrl)
        => Task.FromResult(_options.Urls.RegistrationUrl ?? defaultUrl);

    /// <inheritdoc/>
    public Task<string> GetAccountForgotPasswordUrlAsync(string defaultUrl)
        => Task.FromResult(_options.Urls.ForgotPasswordUrl ?? defaultUrl);

    /// <inheritdoc/>
    public Task<string> GetAccountForgottenPasswordResetUrlAsync(string defaultUrl)
        => Task.FromResult(_options.Urls.ForgotPasswordUrl ?? defaultUrl);

    /// <inheritdoc/>
    public Task<string> GetAccountResetPasswordUrlAsync(string defaultUrl)
        => Task.FromResult(_options.Urls.ResetPasswordUrl ?? defaultUrl);

    /// <inheritdoc/>
    public Task<string> GetAccountConfirmationUrlAsync(string defaultUrl)
        => Task.FromResult(_options.Urls.ConfirmationUrl ?? defaultUrl);

    /// <inheritdoc/>
    public Task<string> GetAccountMyAccountUrlAsync(string defaultUrl)
        => Task.FromResult(_options.Urls.ProfileUrl ?? defaultUrl);

    /// <inheritdoc/>
    public Task<string> GetAccountTwoFormAuthenticationUrlAsync(string defaultUrl)
        => Task.FromResult(_options.Urls.TwoFactorUrl ?? defaultUrl);
}
