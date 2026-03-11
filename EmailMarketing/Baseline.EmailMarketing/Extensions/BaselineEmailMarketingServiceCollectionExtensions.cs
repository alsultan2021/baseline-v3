using Baseline.EmailMarketing;
using Baseline.EmailMarketing.Configuration;
using Baseline.EmailMarketing.Interfaces;
using Baseline.EmailMarketing.Services;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering Baseline Email Marketing services.
/// </summary>
public static class BaselineEmailMarketingServiceCollectionExtensions
{
    /// <summary>
    /// Adds Baseline Email Marketing services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBaselineEmailMarketing(
        this IServiceCollection services,
        Action<BaselineEmailMarketingOptions>? configureOptions = null)
    {
        var options = new BaselineEmailMarketingOptions();
        configureOptions?.Invoke(options);

        services.Configure<BaselineEmailMarketingOptions>(opt =>
        {
            opt.EnableDoubleOptIn = options.EnableDoubleOptIn;
            opt.ConfirmationEmailTemplate = options.ConfirmationEmailTemplate;
            opt.WelcomeEmailTemplate = options.WelcomeEmailTemplate;
            opt.UnsubscribeConfirmationTemplate = options.UnsubscribeConfirmationTemplate;
            opt.PreferenceCenterPath = options.PreferenceCenterPath;
            opt.UnsubscribePagePath = options.UnsubscribePagePath;
            opt.DefaultFromEmail = options.DefaultFromEmail;
            opt.DefaultFromName = options.DefaultFromName;
            opt.ConfirmationLinkExpirationHours = options.ConfirmationLinkExpirationHours;
            opt.TrackOpens = options.TrackOpens;
            opt.TrackClicks = options.TrackClicks;
            opt.RequireConsentBeforeSubscription = options.RequireConsentBeforeSubscription;
            opt.SubscriptionConsentCodeName = options.SubscriptionConsentCodeName;
            opt.DefaultEmailFormat = options.DefaultEmailFormat;
            opt.NewsletterCategories = options.NewsletterCategories;
        });

        // Register services
        services.AddScoped<INewsletterSubscriptionService, NewsletterSubscriptionService>();
        services.AddScoped<INewsletterRetrievalService, NewsletterRetrievalService>();
        services.AddScoped<IEmailPreferenceService, EmailPreferenceService>();
        services.AddSingleton<IEmailAutomationService, EmailAutomationService>();

        return services;
    }

    /// <summary>
    /// Adds Baseline Email Marketing services using configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The configuration section name. Default: "Baseline:EmailMarketing".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBaselineEmailMarketing(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Baseline:EmailMarketing")
    {
        services.Configure<BaselineEmailMarketingOptions>(configuration.GetSection(sectionName));

        // Register services
        services.AddScoped<INewsletterSubscriptionService, NewsletterSubscriptionService>();
        services.AddScoped<INewsletterRetrievalService, NewsletterRetrievalService>();
        services.AddScoped<IEmailPreferenceService, EmailPreferenceService>();
        services.AddSingleton<IEmailAutomationService, EmailAutomationService>();

        return services;
    }
}
