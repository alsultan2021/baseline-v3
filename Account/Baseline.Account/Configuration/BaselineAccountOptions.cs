namespace Baseline.Account;

/// <summary>
/// Configuration options for Baseline v3 Account module.
/// </summary>
public class BaselineAccountOptions
{
    /// <summary>
    /// Enable ASP.NET Core Identity integration.
    /// Default: true
    /// </summary>
    public bool EnableIdentity { get; set; } = true;

    /// <summary>
    /// Enable external authentication providers (OAuth, OIDC).
    /// Default: true
    /// </summary>
    public bool EnableExternalAuthentication { get; set; } = true;

    /// <summary>
    /// Enable two-factor authentication.
    /// Default: false
    /// </summary>
    public bool EnableTwoFactorAuthentication { get; set; } = false;

    /// <summary>
    /// Enable account confirmation via email.
    /// Default: true
    /// </summary>
    public bool RequireEmailConfirmation { get; set; } = true;

    /// <summary>
    /// Enable password recovery functionality.
    /// Default: true
    /// </summary>
    public bool EnablePasswordRecovery { get; set; } = true;

    /// <summary>
    /// Enable user registration.
    /// Default: true
    /// </summary>
    public bool EnableRegistration { get; set; } = true;

    /// <summary>
    /// Default URL to redirect after login.
    /// </summary>
    public string DefaultLoginRedirectUrl { get; set; } = "/";

    /// <summary>
    /// Default URL to redirect after logout.
    /// </summary>
    public string DefaultLogoutRedirectUrl { get; set; } = "/";

    /// <summary>
    /// Login page URL.
    /// </summary>
    public string LoginUrl { get; set; } = "/account/login";

    /// <summary>
    /// Access denied page URL.
    /// </summary>
    public string AccessDeniedUrl { get; set; } = "/account/access-denied";

    /// <summary>
    /// Password reset page URL (for generating reset links).
    /// </summary>
    public string PasswordResetUrl { get; set; } = "/account/reset-password";

    /// <summary>
    /// System email sender address (from address for password reset emails).
    /// </summary>
    public string? SystemEmailFrom { get; set; }

    /// <summary>
    /// Password policy options.
    /// </summary>
    public PasswordPolicyOptions PasswordPolicy { get; set; } = new();

    /// <summary>
    /// Cookie options for authentication.
    /// </summary>
    public AuthCookieOptions Cookie { get; set; } = new();

    /// <summary>
    /// External authentication provider configurations.
    /// </summary>
    public ExternalAuthOptions ExternalAuth { get; set; } = new();

    /// <summary>
    /// Lockout configuration.
    /// </summary>
    public LockoutOptions Lockout { get; set; } = new();

    /// <summary>
    /// URL configuration for account pages.
    /// </summary>
    public AccountUrlOptions Urls { get; set; } = new();

    /// <summary>
    /// Enable login auditing (track login attempts, devices, IP addresses).
    /// Default: true
    /// </summary>
    public bool EnableLoginAuditing { get; set; } = true;

    /// <summary>
    /// Enable email alerts when login from new/unrecognized device.
    /// Requires EnableLoginAuditing to be true.
    /// Default: true
    /// </summary>
    public bool EnableNewDeviceAlerts { get; set; } = true;

    /// <summary>
    /// Login audit configuration.
    /// </summary>
    public LoginAuditOptions LoginAudit { get; set; } = new();

    /// <summary>
    /// Device authorization (device code flow) configuration.
    /// </summary>
    public DeviceAuthorizationOptions DeviceAuthorization { get; set; } = new();

    /// <summary>
    /// Trusted device management configuration.
    /// </summary>
    public TrustedDeviceOptions TrustedDevices { get; set; } = new();

    /// <summary>
    /// WebAuthn/Passkey configuration for biometric authentication (fingerprint/Face ID).
    /// </summary>
    public PasskeyOptions Passkey { get; set; } = new();
}

/// <summary>
/// Account lockout configuration.
/// </summary>
public class LockoutOptions
{
    /// <summary>
    /// Maximum failed access attempts before lockout.
    /// Default: 5
    /// </summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>
    /// Lockout duration in minutes.
    /// Default: 15
    /// </summary>
    public int DurationMinutes { get; set; } = 15;
}

/// <summary>
/// Account URL configuration.
/// </summary>
public class AccountUrlOptions
{
    public string? LoginUrl { get; set; } = "/Account/LogIn";
    public string? RegistrationUrl { get; set; } = "/Account/Register";
    public string? ProfileUrl { get; set; } = "/Account/Profile";
    public string? ForgotPasswordUrl { get; set; } = "/Account/ForgotPassword";
    public string? ResetPasswordUrl { get; set; } = "/Account/ResetPassword";
    public string? ConfirmationUrl { get; set; } = "/Account/Confirmation";
    public string? TwoFactorUrl { get; set; } = "/Account/TwoFactorAuthentication";
    public string? AfterLoginRedirectUrl { get; set; } = "/";
    public string? AfterRegistrationRedirectUrl { get; set; } = "/";
}

/// <summary>
/// Password policy configuration.
/// </summary>
public class PasswordPolicyOptions
{
    /// <summary>
    /// Minimum password length.
    /// Default: 8
    /// </summary>
    public int MinimumLength { get; set; } = 8;

    /// <summary>
    /// Require at least one digit.
    /// Default: true
    /// </summary>
    public bool RequireDigit { get; set; } = true;

    /// <summary>
    /// Require at least one lowercase letter.
    /// Default: true
    /// </summary>
    public bool RequireLowercase { get; set; } = true;

    /// <summary>
    /// Require at least one uppercase letter.
    /// Default: true
    /// </summary>
    public bool RequireUppercase { get; set; } = true;

    /// <summary>
    /// Require at least one non-alphanumeric character.
    /// Default: true
    /// </summary>
    public bool RequireNonAlphanumeric { get; set; } = true;

    /// <summary>
    /// Number of unique characters required.
    /// Default: 4
    /// </summary>
    public int RequiredUniqueChars { get; set; } = 4;
}

/// <summary>
/// Authentication cookie configuration.
/// </summary>
public class AuthCookieOptions
{
    /// <summary>
    /// Cookie name.
    /// </summary>
    public string Name { get; set; } = ".Baseline.Auth";

    /// <summary>
    /// Cookie expiration in days.
    /// Default: 14
    /// </summary>
    public int ExpirationDays { get; set; } = 14;

    /// <summary>
    /// Enable sliding expiration.
    /// Default: true
    /// </summary>
    public bool SlidingExpiration { get; set; } = true;

    /// <summary>
    /// Require HTTPS for cookies.
    /// Default: true
    /// </summary>
    public bool RequireHttps { get; set; } = true;

    /// <summary>
    /// Same-site mode for cookies.
    /// Default: "Lax"
    /// </summary>
    public string SameSite { get; set; } = "Lax";
}

/// <summary>
/// External authentication provider configuration.
/// </summary>
public class ExternalAuthOptions
{
    /// <summary>
    /// Microsoft/Azure AD authentication options.
    /// </summary>
    public OAuthProviderOptions? Microsoft { get; set; }

    /// <summary>
    /// Google authentication options.
    /// </summary>
    public OAuthProviderOptions? Google { get; set; }

    /// <summary>
    /// Facebook authentication options.
    /// </summary>
    public OAuthProviderOptions? Facebook { get; set; }

    /// <summary>
    /// Custom OpenID Connect provider options.
    /// </summary>
    public List<OidcProviderOptions> CustomOidc { get; set; } = [];
}

/// <summary>
/// OAuth provider configuration.
/// </summary>
public class OAuthProviderOptions
{
    /// <summary>
    /// Client ID from the OAuth provider.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret from the OAuth provider.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Additional scopes to request.
    /// </summary>
    public List<string> Scopes { get; set; } = [];
}

/// <summary>
/// OpenID Connect provider configuration.
/// </summary>
public class OidcProviderOptions : OAuthProviderOptions
{
    /// <summary>
    /// Display name for the provider.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Authority URL (issuer).
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Authentication scheme name.
    /// </summary>
    public string Scheme { get; set; } = string.Empty;
}

/// <summary>
/// WebAuthn/Passkey configuration for biometric authentication (fingerprint/Face ID).
/// </summary>
public class PasskeyOptions
{
    /// <summary>
    /// Enable passkey/biometric authentication.
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Relying party name (displayed to users during registration).
    /// </summary>
    public string RelyingPartyName { get; set; } = "My Application";

    /// <summary>
    /// Relying party ID (typically the domain name).
    /// If not set, will be derived from the current request host.
    /// </summary>
    public string? RelyingPartyId { get; set; }

    /// <summary>
    /// Allowed origins for WebAuthn operations.
    /// If not set, will be derived from the current request.
    /// </summary>
    public List<string> Origins { get; set; } = [];

    /// <summary>
    /// Timeout for WebAuthn operations in milliseconds.
    /// Default: 60000 (60 seconds)
    /// </summary>
    public uint Timeout { get; set; } = 60000;

    /// <summary>
    /// Allow cross-platform authenticators (security keys) in addition to platform authenticators (biometrics).
    /// Default: true
    /// </summary>
    public bool AllowCrossPlatformAuthenticators { get; set; } = true;
}
