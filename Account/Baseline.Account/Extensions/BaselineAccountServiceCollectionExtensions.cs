using Fido2NetLib;
using Kentico.Membership;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Baseline.Account;

/// <summary>
/// Extension methods for registering Baseline v3 Account services.
/// </summary>
public static class BaselineAccountServiceCollectionExtensions
{
    /// <summary>
    /// Adds Baseline v3 Account services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for Account options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Minimal setup with defaults
    /// services.AddBaselineAccount();
    /// 
    /// // With custom configuration
    /// services.AddBaselineAccount(options =>
    /// {
    ///     options.RequireEmailConfirmation = true;
    ///     options.PasswordPolicy.MinimumLength = 12;
    ///     options.Cookie.ExpirationDays = 30;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddBaselineAccount(
        this IServiceCollection services,
        Action<BaselineAccountOptions>? configure = null)
    {
        return services.AddBaselineAccount<ApplicationUser>(configure);
    }

    /// <summary>
    /// Adds Baseline v3 Account services to the service collection with generic user support.
    /// Use this overload when using a custom ApplicationUser type.
    /// </summary>
    /// <typeparam name="TUser">The application user type (must inherit from ApplicationUser).</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for Account options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBaselineAccount<TUser>(
        this IServiceCollection services,
        Action<BaselineAccountOptions>? configure = null)
        where TUser : ApplicationUser, new()
    {
        // Register options using the Options pattern
        services.AddOptions<BaselineAccountOptions>()
            .Configure(opt => configure?.Invoke(opt));

        // Build options for conditional registration
        var options = new BaselineAccountOptions();
        configure?.Invoke(options);

        // Register authentication services with generic TUser support
        services.AddScoped<IAuthenticationService, AuthenticationService<TUser>>();
        services.AddScoped<IPasswordService, PasswordService<TUser>>();
        services.AddScoped<IProfileService, ProfileService<TUser>>();
        services.AddScoped<IEmailConfirmationService, EmailConfirmationService<TUser>>();

        // Register legacy services that don't require TUser generic
        services.AddScoped<IAccountSettingsRepository, AccountSettingsRepository>();
        services.AddScoped<IModelStateService, ModelStateService>();
        services.AddScoped<IAuthenticationConfigurations, AuthenticationConfigurations>();
        // Note: CMS.EmailEngine.IEmailService is registered by Xperience by Kentico

        // Register login audit services
        services.AddOptions<LoginAuditOptions>()
            .Configure(opt =>
            {
                opt.Enabled = options.EnableLoginAuditing;
                opt.EnableNewDeviceAlerts = options.EnableNewDeviceAlerts;
            });
        services.AddScoped<ILoginAuditService, LoginAuditService>();
        services.AddScoped<INewDeviceAlertService, NewDeviceAlertService>();
        services.AddScoped<ISessionManagementService, SessionManagementService<TUser>>();

        // Register device authorization services
        services.AddOptions<DeviceAuthorizationOptions>()
            .Configure(opt =>
            {
                opt.Enabled = options.DeviceAuthorization.Enabled;
                opt.CodeLifetime = options.DeviceAuthorization.CodeLifetime;
                opt.PollingIntervalSeconds = options.DeviceAuthorization.PollingIntervalSeconds;
            });
        services.AddScoped<IDeviceAuthorizationService, DeviceAuthorizationService>();

        // Register trusted device services
        services.AddOptions<TrustedDeviceOptions>()
            .Configure(opt =>
            {
                opt.Enabled = options.TrustedDevices.Enabled;
                opt.TrustDuration = options.TrustedDevices.TrustDuration;
            });
        services.AddScoped<ITrustedDeviceService, TrustedDeviceService>();

        // Register passkey/WebAuthn services for biometric authentication
        if (options.Passkey.Enabled)
        {
            services.AddFido2(fido2Options =>
            {
                fido2Options.ServerName = options.Passkey.RelyingPartyName;
                fido2Options.ServerDomain = options.Passkey.RelyingPartyId;
                if (options.Passkey.Origins.Count > 0)
                {
                    fido2Options.Origins = new HashSet<string>(options.Passkey.Origins);
                }
                fido2Options.TimestampDriftTolerance = 300000; // 5 minutes
            });
            services.AddScoped<IPasskeyService, PasskeyService>();
        }

        // Register external authentication if enabled
        if (options.EnableExternalAuthentication)
        {
            services.AddScoped<IExternalAuthenticationService, ExternalAuthenticationService<TUser>>();
            services.ConfigureExternalProviders(options);
        }

        return services;
    }

    /// <summary>
    /// Adds Baseline v3 Account services with typed user support.
    /// Call this after AddIdentity to register user-specific services.
    /// </summary>
    /// <typeparam name="TUser">The application user type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBaselineAccountForUser<TUser>(this IServiceCollection services)
        where TUser : ApplicationUser, new()
    {
        // Register legacy services that require TUser generic
        services.AddScoped<IUserRepository, UserRepository<TUser>>();
        services.AddScoped<IUserService, UserService<TUser>>();
        services.AddScoped<ISignInManagerService, SignInManagerService<TUser>>();
        services.AddScoped<IUserManagerService, UserManagerService<TUser>>();

        return services;
    }

    /// <summary>
    /// Configures Identity with Xperience by Kentico settings and default token providers.
    /// Call this instead of manually configuring AddIdentity for full password reset support.
    /// </summary>
    /// <typeparam name="TUser">The application user type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional action to configure Identity options.</param>
    /// <returns>The IdentityBuilder for further configuration.</returns>
    /// <example>
    /// <code>
    /// // Use this instead of AddIdentity for full feature support:
    /// services.AddBaselineIdentity&lt;ApplicationUser&gt;();
    /// 
    /// // Or with custom options:
    /// services.AddBaselineIdentity&lt;ApplicationUser&gt;(options =&gt;
    /// {
    ///     options.Password.RequiredLength = 12;
    ///     options.Lockout.MaxFailedAccessAttempts = 3;
    /// });
    /// </code>
    /// </example>
    public static IdentityBuilder AddBaselineIdentity<TUser>(
        this IServiceCollection services,
        Action<IdentityOptions>? configureOptions = null)
        where TUser : ApplicationUser, new()
    {
        return services.AddIdentity<TUser, NoOpApplicationRole>(options =>
            {
                // Xperience requirement: enables ApplicationUser.Enabled check
                options.SignIn.RequireConfirmedAccount = true;
                // Xperience requirement: unique emails for members
                options.User.RequireUniqueEmail = true;

                // Apply custom options
                configureOptions?.Invoke(options);
            })
            .AddUserStore<ApplicationUserStore<TUser>>()
            .AddRoleStore<NoOpApplicationRoleStore>()
            .AddUserManager<UserManager<TUser>>()
            .AddSignInManager<SignInManager<TUser>>()
            // Required for password reset and email confirmation tokens
            .AddDefaultTokenProviders();
    }

    private static IServiceCollection ConfigureExternalProviders(
        this IServiceCollection services,
        BaselineAccountOptions options)
    {
        var authBuilder = services.AddAuthentication();

        if (options.ExternalAuth.Microsoft is not null)
        {
            authBuilder.AddMicrosoftAccount(microsoftOptions =>
            {
                microsoftOptions.ClientId = options.ExternalAuth.Microsoft.ClientId;
                microsoftOptions.ClientSecret = options.ExternalAuth.Microsoft.ClientSecret;
                foreach (var scope in options.ExternalAuth.Microsoft.Scopes)
                {
                    microsoftOptions.Scope.Add(scope);
                }
            });
        }

        if (options.ExternalAuth.Google is not null)
        {
            authBuilder.AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = options.ExternalAuth.Google.ClientId;
                googleOptions.ClientSecret = options.ExternalAuth.Google.ClientSecret;
                foreach (var scope in options.ExternalAuth.Google.Scopes)
                {
                    googleOptions.Scope.Add(scope);
                }
            });
        }

        if (options.ExternalAuth.Facebook is not null)
        {
            authBuilder.AddFacebook(facebookOptions =>
            {
                facebookOptions.AppId = options.ExternalAuth.Facebook.ClientId;
                facebookOptions.AppSecret = options.ExternalAuth.Facebook.ClientSecret;
                foreach (var scope in options.ExternalAuth.Facebook.Scopes)
                {
                    facebookOptions.Scope.Add(scope);
                }
            });
        }

        foreach (var oidc in options.ExternalAuth.CustomOidc)
        {
            authBuilder.AddOpenIdConnect(oidc.Scheme, oidc.DisplayName, oidcOptions =>
            {
                oidcOptions.Authority = oidc.Authority;
                oidcOptions.ClientId = oidc.ClientId;
                oidcOptions.ClientSecret = oidc.ClientSecret;
                foreach (var scope in oidc.Scopes)
                {
                    oidcOptions.Scope.Add(scope);
                }
            });
        }

        return services;
    }
}
