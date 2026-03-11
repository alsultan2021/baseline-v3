using Microsoft.Extensions.Options;

namespace Baseline.Account;

/// <summary>
/// v3 implementation of IAuthenticationConfigurations.
/// </summary>
public sealed class AuthenticationConfigurations : IAuthenticationConfigurations
{
    private readonly BaselineAccountOptions _options;

    public AuthenticationConfigurations(IOptions<BaselineAccountOptions> options)
    {
        _options = options.Value;
    }

    // For direct instantiation (backwards compatibility)
    public AuthenticationConfigurations()
    {
        _options = new BaselineAccountOptions();
    }

    /// <inheritdoc/>
    public bool AllowPasswordSignIn => true;

    /// <inheritdoc/>
    public bool RequireConfirmedEmail => _options.RequireEmailConfirmation;

    /// <inheritdoc/>
    public bool RequireConfirmedAccount => _options.RequireEmailConfirmation;

    /// <inheritdoc/>
    public int LockoutMaxFailedAttempts => _options.Lockout.MaxFailedAttempts;

    /// <inheritdoc/>
    public TimeSpan LockoutDuration => TimeSpan.FromMinutes(_options.Lockout.DurationMinutes);

    /// <inheritdoc/>
    public IEnumerable<string> ExternalAuthenticationProviders
    {
        get
        {
            var providers = new List<string>();

            if (_options.ExternalAuth.Microsoft is not null)
                providers.Add("Microsoft");

            if (_options.ExternalAuth.Google is not null)
                providers.Add("Google");

            if (_options.ExternalAuth.Facebook is not null)
                providers.Add("Facebook");

            foreach (var oidc in _options.ExternalAuth.CustomOidc)
                providers.Add(oidc.Scheme);

            return providers;
        }
    }

    /// <inheritdoc/>
    public bool UseTwoFormAuthentication() => _options.EnableTwoFactorAuthentication;

    // Additional configuration properties used by v2 code
    public IEnumerable<string> AllExternalUserRoles { get; set; } = ["external-user"];
}
