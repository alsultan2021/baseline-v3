using CMS.ContentEngine;
using CMS.DataEngine;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Localization.Middleware;

/// <summary>
/// Extension methods for localization middleware.
/// </summary>
public static class LocalizationMiddlewareExtensions
{
    /// <summary>
    /// Configures ASP.NET Core request localization.
    /// When AutoDetectCulturesFromXbK is true, queries XbK ContentLanguageInfo
    /// to auto-populate supported cultures and default culture.
    /// </summary>
    public static IApplicationBuilder UseLocalizationBaseline(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetService<Baseline.Localization.BaselineLocalizationOptions>();
        if (options is null)
        {
            return app;
        }

        if (options.AutoDetectCulturesFromXbK)
        {
            AutoDetectCultures(app.ApplicationServices, options);
        }

        return ConfigureLocalization(app, options);
    }

    /// <summary>
    /// Configures ASP.NET Core request localization.
    /// When AutoDetectCulturesFromXbK is true, queries XbK ContentLanguageInfo
    /// to auto-populate supported cultures and default culture.
    /// </summary>
    public static WebApplication UseLocalizationBaseline(this WebApplication app)
    {
        var options = app.Services.GetService<Baseline.Localization.BaselineLocalizationOptions>();
        if (options is null)
        {
            return app;
        }

        if (options.AutoDetectCulturesFromXbK)
        {
            AutoDetectCultures(app.Services, options);
        }

        ConfigureLocalization(app, options);
        return app;
    }

    /// <summary>
    /// Queries XbK ContentLanguageInfo to auto-populate cultures from the CMS database.
    /// Per Kentico docs: languages are global and all channels see all languages.
    /// Retries up to 3 times to handle transient SQL connection failures during startup.
    /// </summary>
    private static void AutoDetectCultures(
        IServiceProvider serviceProvider,
        Baseline.Localization.BaselineLocalizationOptions options)
    {
        const int maxRetries = 3;
        const int delayMs = 500;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var langProvider = scope.ServiceProvider
                    .GetRequiredService<IInfoProvider<ContentLanguageInfo>>();

                // Force a dedicated DB connection to avoid racing with Kentico's
                // own startup initialization on the shared CMS connection.
                using var connectionScope = new CMSConnectionScope(true);
                var allLanguages = langProvider.Get().ToList();

                if (allLanguages.Count == 0)
                {
                    return;
                }

                // Auto-populate SupportedCultures from XbK
                options.SupportedCultures = allLanguages.Select(lang =>
                    new Baseline.Localization.BaselineCultureInfo(
                        lang.ContentLanguageCultureFormat,
                        lang.ContentLanguageDisplayName)
                    {
                        ShortCode = lang.ContentLanguageName
                    }).ToList();

                // Auto-detect default culture from XbK
                var defaultLang = allLanguages.FirstOrDefault(l => l.ContentLanguageIsDefault)
                    ?? allLanguages.First();

                options.DefaultCulture = defaultLang.ContentLanguageCultureFormat;
                return;
            }
            catch (InvalidOperationException) when (attempt < maxRetries)
            {
                // Transient SQL connection failure (e.g. closed/stale connection
                // from pool during startup) — wait briefly and retry.
                System.Threading.Thread.Sleep(delayMs * attempt);
            }
            catch (Exception ex)
            {
                var logger = serviceProvider.GetService<ILoggerFactory>()
                    ?.CreateLogger(typeof(LocalizationMiddlewareExtensions));
                logger?.LogWarning(ex,
                    "Failed to auto-detect cultures from XbK. Falling back to configured values.");
                return;
            }
        }
    }

    private static IApplicationBuilder ConfigureLocalization(
        IApplicationBuilder app,
        Baseline.Localization.BaselineLocalizationOptions options)
    {
        var supportedCultures = options.SupportedCultures
            .Select(c => new System.Globalization.CultureInfo(c.Code))
            .ToArray();

        var defaultCulture = new System.Globalization.CultureInfo(options.DefaultCulture);

        var requestLocalizationOptions = new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(defaultCulture),
            SupportedCultures = supportedCultures,
            SupportedUICultures = supportedCultures,
            FallBackToParentCultures = options.EnableFallbackToDefault,
            FallBackToParentUICultures = options.EnableFallbackToDefault,
            ApplyCurrentCultureToResponseHeaders = true
        };

        // Configure culture providers based on options
        requestLocalizationOptions.RequestCultureProviders.Clear();

        // Route data provider takes highest priority so URLs like /fr/Account/LogIn
        // correctly set the culture from the {language} route parameter.
        requestLocalizationOptions.RequestCultureProviders.Add(
            new RouteLanguageCultureProvider
            {
                RouteDataKey = options.LanguageNameRouteValuesKey,
                SupportedCultures = options.SupportedCultures
            });

        if (options.EnableUrlCultureProvider)
        {
            requestLocalizationOptions.RequestCultureProviders.Add(
                new QueryStringRequestCultureProvider());
        }

        if (options.EnableCookieCultureProvider)
        {
            if (options.EnableCookieConsentCheck)
            {
                // Use consent-aware provider: only reads culture cookie when consent is granted
                requestLocalizationOptions.RequestCultureProviders.Add(
                    new ConsentAwareCookieCultureProvider
                    {
                        CookieName = options.CultureCookieName,
                        ConsentCookieName = options.ConsentCookieName
                    });
            }
            else
            {
                requestLocalizationOptions.RequestCultureProviders.Add(
                    new CookieRequestCultureProvider { CookieName = options.CultureCookieName });
            }
        }

        if (options.EnableAcceptLanguageProvider)
        {
            requestLocalizationOptions.RequestCultureProviders.Add(
                new AcceptLanguageHeaderRequestCultureProvider());
        }

        return app.UseRequestLocalization(requestLocalizationOptions);
    }
}
