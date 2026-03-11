using Baseline.Core.Admin.Sms;
using Baseline.Core.Admin.Sms.Modules;
using Baseline.Core.Interfaces;
using Baseline.Core.Services.Sms;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baseline.Core.Extensions;

/// <summary>
/// Extension methods for registering SMS services.
/// </summary>
public static class SmsServiceCollectionExtensions
{
    /// <summary>
    /// Adds Twilio SMS service with admin UI support.
    /// Settings are managed through the Kentico admin UI.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTwilioSmsAdmin(this IServiceCollection services)
    {
        // Register module installer for database schema
        services.AddSingleton<TwilioSmsModuleInstaller>();
        
        // Register notification email extension installer
        services.AddSingleton<TwilioNotificationExtensionInstaller>();

        // Register database-backed SMS service
        services.AddScoped<ISmsService, DatabaseTwilioSmsService>();

        return services;
    }

    /// <summary>
    /// Adds Twilio SMS service to the service collection.
    /// Reads configuration from "Twilio" section in appsettings.json.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTwilioSmsService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<TwilioSmsOptions>(configuration.GetSection(TwilioSmsOptions.SectionName));

        var twilioConfig = configuration.GetSection(TwilioSmsOptions.SectionName).Get<TwilioSmsOptions>();

        if (twilioConfig?.IsValid() == true && twilioConfig.Enabled)
        {
            services.AddSingleton<ISmsService, TwilioSmsService>();
        }
        else
        {
            services.AddSingleton<ISmsService, NoOpSmsService>();
        }

        return services;
    }

    /// <summary>
    /// Adds Twilio SMS service with custom configuration action.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure Twilio options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTwilioSmsService(
        this IServiceCollection services,
        Action<TwilioSmsOptions> configureOptions)
    {
        var options = new TwilioSmsOptions();
        configureOptions(options);

        services.Configure(configureOptions);

        if (options.IsValid() && options.Enabled)
        {
            services.AddSingleton<ISmsService, TwilioSmsService>();
        }
        else
        {
            services.AddSingleton<ISmsService, NoOpSmsService>();
        }

        return services;
    }

    /// <summary>
    /// Adds a no-op SMS service (for when SMS is not needed).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNoOpSmsService(this IServiceCollection services)
    {
        services.AddSingleton<ISmsService, NoOpSmsService>();
        return services;
    }
}
