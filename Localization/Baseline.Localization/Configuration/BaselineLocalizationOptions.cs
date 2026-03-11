namespace Baseline.Localization;

/// <summary>
/// Configuration options for Baseline v3 Localization module.
/// </summary>
public class BaselineLocalizationOptions
{
    /// <summary>
    /// Default culture code. When AutoDetectCulturesFromXbK is true,
    /// this is ignored and the XbK default language is used instead.
    /// Default: "en-US"
    /// </summary>
    public string DefaultCulture { get; set; } = "en-US";

    /// <summary>
    /// Supported cultures. When AutoDetectCulturesFromXbK is true,
    /// this is auto-populated from XbK ContentLanguageInfo at middleware startup.
    /// </summary>
    public List<BaselineCultureInfo> SupportedCultures { get; set; } = [new("en-US")];

    /// <summary>
    /// When true, automatically detects supported cultures and default culture
    /// from Xperience by Kentico's configured languages (ContentLanguageInfo).
    /// This is the recommended approach per Kentico documentation.
    /// Default: true
    /// </summary>
    public bool AutoDetectCulturesFromXbK { get; set; } = true;

    /// <summary>
    /// Enable URL-based culture detection.
    /// Default: true
    /// </summary>
    public bool EnableUrlCultureProvider { get; set; } = true;

    /// <summary>
    /// Enable cookie-based culture persistence.
    /// Default: true
    /// </summary>
    public bool EnableCookieCultureProvider { get; set; } = true;

    /// <summary>
    /// When true, the culture cookie is only read when cookie consent has been granted.
    /// Uses <see cref="ConsentCookieName"/> to check for consent.
    /// When false, the culture cookie is always read (original behavior).
    /// Default: false
    /// </summary>
    public bool EnableCookieConsentCheck { get; set; }

    /// <summary>
    /// The name of the consent cookie to check when <see cref="EnableCookieConsentCheck"/> is true.
    /// Default matches ASP.NET Core's CookiePolicyMiddleware consent cookie.
    /// </summary>
    public string ConsentCookieName { get; set; } = ".AspNet.Consent";

    /// <summary>
    /// Enable Accept-Language header detection.
    /// Default: true
    /// </summary>
    public bool EnableAcceptLanguageProvider { get; set; } = true;

    /// <summary>
    /// Culture cookie name.
    /// </summary>
    public string CultureCookieName { get; set; } = ".Baseline.Culture";

    /// <summary>
    /// URL culture format.
    /// Default: "/en-us/path" (Prefix)
    /// </summary>
    public CultureUrlFormat UrlFormat { get; set; } = CultureUrlFormat.Prefix;

    /// <summary>
    /// Hide default culture from URL.
    /// Default: true
    /// </summary>
    public bool HideDefaultCultureInUrl { get; set; } = true;

    /// <summary>
    /// Enable automatic redirect to localized URLs.
    /// Default: true
    /// </summary>
    public bool EnableCultureRedirect { get; set; } = true;

    /// <summary>
    /// Enable fallback to default culture for missing translations.
    /// Default: true
    /// </summary>
    public bool EnableFallbackToDefault { get; set; } = true;

    /// <summary>
    /// Cache localized strings.
    /// Default: true
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Cache duration in minutes.
    /// Default: 60
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Resource string source.
    /// Default: "Database"
    /// </summary>
    public ResourceStringSource StringSource { get; set; } = ResourceStringSource.Database;

    /// <summary>
    /// Path to resource files (for file-based resources).
    /// </summary>
    public string ResourceFilePath { get; set; } = "Resources";

    /// <summary>
    /// Enable Content Hub-based string localizer.
    /// Default: false
    /// </summary>
    public bool EnableContentHubLocalizer { get; set; }

    /// <summary>
    /// Enable hreflang link generation for SEO.
    /// Default: true
    /// </summary>
    public bool EnableHreflangLinks { get; set; } = true;

    /// <summary>
    /// Enable AIRA (AI-Assisted) translation integration.
    /// Requires Xperience Advanced license tier.
    /// Default: false
    /// </summary>
    public bool EnableAIRATranslation { get; set; }

    /// <summary>
    /// Enable translation workflow event detection.
    /// When true, a publish event handler detects content items missing translations
    /// and invokes <see cref="Events.ITranslationWebhookService"/>.
    /// Default: false
    /// </summary>
    public bool EnableTranslationWorkflow { get; set; }

    /// <summary>
    /// Route values key for preserving language context across routing modes.
    /// Per Kentico docs, set on WebPageRoutingOptions.LanguageNameRouteValuesKey
    /// and matched in route templates as {language}.
    /// Default: "language"
    /// </summary>
    public string LanguageNameRouteValuesKey { get; set; } = "language";

    /// <summary>
    /// Whether to use XbK language fallbacks when retrieving content.
    /// When true, content queries fall back to the fallback language chain
    /// when a language variant doesn't exist.
    /// Per Kentico docs: configured per-language in the Languages application.
    /// Default: true
    /// </summary>
    public bool UseLanguageFallbacks { get; set; } = true;
}

/// <summary>
/// Culture URL format options.
/// </summary>
public enum CultureUrlFormat
{
    /// <summary>
    /// Culture code as URL prefix: /en-us/about
    /// </summary>
    Prefix,

    /// <summary>
    /// Culture code as subdomain: en-us.example.com/about
    /// </summary>
    Subdomain,

    /// <summary>
    /// Culture code as query parameter: /about?culture=en-us
    /// </summary>
    QueryString,

    /// <summary>
    /// No culture in URL (use cookie/header only).
    /// </summary>
    None
}

/// <summary>
/// Resource string source options.
/// </summary>
public enum ResourceStringSource
{
    /// <summary>
    /// Load from database (Xperience resource strings).
    /// </summary>
    Database,

    /// <summary>
    /// Load from .resx files.
    /// </summary>
    ResxFiles,

    /// <summary>
    /// Load from JSON files.
    /// </summary>
    JsonFiles,

    /// <summary>
    /// Hybrid: database with file fallback.
    /// </summary>
    Hybrid
}

/// <summary>
/// Culture information.
/// </summary>
public class BaselineCultureInfo
{
    /// <summary>
    /// Culture code (e.g., "en-US").
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Display name.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Native name.
    /// </summary>
    public string? NativeName { get; set; }

    /// <summary>
    /// Two-letter ISO code.
    /// </summary>
    public string ShortCode { get; set; }

    /// <summary>
    /// Flag icon CSS class or URL.
    /// </summary>
    public string? FlagIcon { get; set; }

    /// <summary>
    /// Text direction (LTR/RTL).
    /// </summary>
    public TextDirection Direction { get; set; } = TextDirection.LeftToRight;

    public BaselineCultureInfo(string code)
    {
        Code = code;
        ShortCode = code.Length >= 2 ? code[..2].ToLowerInvariant() : code;
        DisplayName = code;
    }

    public BaselineCultureInfo(string code, string displayName) : this(code)
    {
        DisplayName = displayName;
    }
}

/// <summary>
/// Text direction.
/// </summary>
public enum TextDirection
{
    LeftToRight,
    RightToLeft
}
