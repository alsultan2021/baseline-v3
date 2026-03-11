namespace Baseline.Forms.Localization.Configuration;

/// <summary>
/// Configuration options for multilingual forms.
/// </summary>
public class LocalizedFormsOptions
{
    /// <summary>
    /// Enable multilingual form support. Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default culture code when none is specified. Default: "en-US".
    /// </summary>
    public string DefaultCulture { get; set; } = "en-US";

    /// <summary>
    /// Prefix for form translation resource keys. Default: "forms.".
    /// </summary>
    public string ResourceKeyPrefix { get; set; } = "forms.";

    /// <summary>
    /// Cache duration for localized forms in minutes. Default: 10.
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 10;

    /// <summary>
    /// Store submission culture in form data. Default: true.
    /// </summary>
    public bool TrackSubmissionCulture { get; set; } = true;

    /// <summary>
    /// Field name for storing submission culture. Default: "SubmissionCulture".
    /// </summary>
    public string CultureFieldName { get; set; } = "SubmissionCulture";

    /// <summary>
    /// Enable culture-specific validation patterns. Default: true.
    /// </summary>
    public bool EnableCultureSpecificValidation { get; set; } = true;

    /// <summary>
    /// Fallback to default culture if translation missing. Default: true.
    /// </summary>
    public bool FallbackToDefaultCulture { get; set; } = true;

    /// <summary>
    /// Culture-specific validation patterns.
    /// </summary>
    public CultureValidationPatterns ValidationPatterns { get; set; } = new();
}

/// <summary>
/// Culture-specific validation patterns.
/// </summary>
public class CultureValidationPatterns
{
    /// <summary>
    /// Postal code patterns by culture. Key: culture code, Value: regex pattern.
    /// </summary>
    public Dictionary<string, string> PostalCodePatterns { get; set; } = new()
    {
        ["en-US"] = @"^\d{5}(-\d{4})?$",
        ["en-GB"] = @"^[A-Z]{1,2}\d[A-Z\d]?\s*\d[A-Z]{2}$",
        ["en-CA"] = @"^[A-Z]\d[A-Z]\s*\d[A-Z]\d$",
        ["de-DE"] = @"^\d{5}$",
        ["fr-FR"] = @"^\d{5}$",
        ["es-ES"] = @"^\d{5}$",
        ["it-IT"] = @"^\d{5}$",
        ["nl-NL"] = @"^\d{4}\s*[A-Z]{2}$",
        ["pl-PL"] = @"^\d{2}-\d{3}$",
        ["cs-CZ"] = @"^\d{3}\s*\d{2}$"
    };

    /// <summary>
    /// Phone number patterns by culture.
    /// </summary>
    public Dictionary<string, string> PhonePatterns { get; set; } = new()
    {
        ["en-US"] = @"^(\+1)?[\s.-]?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}$",
        ["en-GB"] = @"^(\+44)?[\s.-]?\d{10,11}$",
        ["de-DE"] = @"^(\+49)?[\s.-]?\d{10,14}$",
        ["fr-FR"] = @"^(\+33)?[\s.-]?\d{9,10}$"
    };

    /// <summary>
    /// Date format patterns by culture.
    /// </summary>
    public Dictionary<string, string> DateFormats { get; set; } = new()
    {
        ["en-US"] = "MM/dd/yyyy",
        ["en-GB"] = "dd/MM/yyyy",
        ["de-DE"] = "dd.MM.yyyy",
        ["fr-FR"] = "dd/MM/yyyy",
        ["cs-CZ"] = "d.M.yyyy"
    };

    /// <summary>
    /// Decimal separator by culture.
    /// </summary>
    public Dictionary<string, string> DecimalSeparators { get; set; } = new()
    {
        ["en-US"] = ".",
        ["en-GB"] = ".",
        ["de-DE"] = ",",
        ["fr-FR"] = ",",
        ["cs-CZ"] = ","
    };
}
