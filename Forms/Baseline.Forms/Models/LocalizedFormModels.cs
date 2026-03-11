using System.Text.Json.Serialization;

namespace Baseline.Forms.Localization.Models;

/// <summary>
/// Represents a form with localized field labels, placeholders, and messages.
/// </summary>
public class LocalizedForm
{
    /// <summary>
    /// Form code name.
    /// </summary>
    public string CodeName { get; set; } = string.Empty;

    /// <summary>
    /// Localized form display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Culture code for this localization.
    /// </summary>
    public string CultureCode { get; set; } = string.Empty;

    /// <summary>
    /// Localized form description/instructions.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Localized submit button text.
    /// </summary>
    public string SubmitButtonText { get; set; } = "Submit";

    /// <summary>
    /// Localized success message after submission.
    /// </summary>
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Localized error message for submission failure.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Collection of localized form fields.
    /// </summary>
    public IReadOnlyList<LocalizedFormField> Fields { get; set; } = [];

    /// <summary>
    /// Available cultures for this form.
    /// </summary>
    public IReadOnlyList<string> AvailableCultures { get; set; } = [];
}

/// <summary>
/// Represents a form field with localized labels and messages.
/// </summary>
public class LocalizedFormField
{
    /// <summary>
    /// Field name (code name).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Localized field caption/label.
    /// </summary>
    public string Caption { get; set; } = string.Empty;

    /// <summary>
    /// Localized placeholder text.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Localized help text/tooltip.
    /// </summary>
    public string? HelpText { get; set; }

    /// <summary>
    /// Data type of the field.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Whether the field is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Default value.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Form component type identifier.
    /// </summary>
    public string? ComponentType { get; set; }

    /// <summary>
    /// Localized validation rules.
    /// </summary>
    public IReadOnlyList<LocalizedValidationRule> ValidationRules { get; set; } = [];

    /// <summary>
    /// Localized options for select/radio fields.
    /// </summary>
    public IReadOnlyList<LocalizedOption>? Options { get; set; }

    /// <summary>
    /// Additional properties.
    /// </summary>
    public IDictionary<string, object?>? Properties { get; set; }
}

/// <summary>
/// Represents a localized validation rule.
/// </summary>
public class LocalizedValidationRule
{
    /// <summary>
    /// Validation rule type (Required, Email, Regex, MinLength, etc.).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Localized error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Rule parameters.
    /// </summary>
    public IDictionary<string, object?>? Parameters { get; set; }
}

/// <summary>
/// Represents a localized option for select/radio fields.
/// </summary>
public class LocalizedOption
{
    /// <summary>
    /// Option value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Localized display text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Whether this option is selected by default.
    /// </summary>
    public bool IsDefault { get; set; }
}

/// <summary>
/// Result of localized field validation.
/// </summary>
public class LocalizedValidationResult
{
    /// <summary>
    /// Whether the value is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Localized error message if invalid.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Validation rule that failed.
    /// </summary>
    public string? FailedRule { get; set; }
}

/// <summary>
/// Result of localized form validation.
/// </summary>
public class LocalizedFormValidationResult
{
    /// <summary>
    /// Whether the form data is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Field-level validation errors.
    /// </summary>
    public IReadOnlyList<LocalizedFieldError> Errors { get; set; } = [];
}

/// <summary>
/// Localized field validation error.
/// </summary>
public class LocalizedFieldError
{
    /// <summary>
    /// Field name.
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Localized field caption.
    /// </summary>
    public string FieldCaption { get; set; } = string.Empty;

    /// <summary>
    /// Localized error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Validation rule that failed.
    /// </summary>
    public string RuleType { get; set; } = string.Empty;
}

/// <summary>
/// Result of localized form submission.
/// </summary>
public class LocalizedSubmissionResult
{
    /// <summary>
    /// Whether the submission succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Submission ID if successful.
    /// </summary>
    public int? SubmissionId { get; set; }

    /// <summary>
    /// Localized result message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Validation errors if submission failed.
    /// </summary>
    public IReadOnlyList<LocalizedFieldError>? Errors { get; set; }

    /// <summary>
    /// Culture code of the submission.
    /// </summary>
    public string CultureCode { get; set; } = string.Empty;
}

/// <summary>
/// Translatable string from a form.
/// </summary>
public class FormTranslatableString
{
    /// <summary>
    /// Unique key for the string.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// String type (Caption, Placeholder, HelpText, ErrorMessage, etc.).
    /// </summary>
    public FormStringType StringType { get; set; }

    /// <summary>
    /// Field name (null for form-level strings).
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// Default value (from default culture).
    /// </summary>
    public string DefaultValue { get; set; } = string.Empty;

    /// <summary>
    /// Current translations.
    /// </summary>
    public IDictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Type of form string for translation.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FormStringType
{
    /// <summary>
    /// Form display name.
    /// </summary>
    FormName,

    /// <summary>
    /// Form description.
    /// </summary>
    FormDescription,

    /// <summary>
    /// Submit button text.
    /// </summary>
    SubmitButton,

    /// <summary>
    /// Success message.
    /// </summary>
    SuccessMessage,

    /// <summary>
    /// Error message.
    /// </summary>
    ErrorMessage,

    /// <summary>
    /// Field caption/label.
    /// </summary>
    FieldCaption,

    /// <summary>
    /// Field placeholder.
    /// </summary>
    FieldPlaceholder,

    /// <summary>
    /// Field help text.
    /// </summary>
    FieldHelpText,

    /// <summary>
    /// Validation error message.
    /// </summary>
    ValidationMessage,

    /// <summary>
    /// Option text for select/radio.
    /// </summary>
    OptionText
}

/// <summary>
/// Field translation data.
/// </summary>
public class FieldTranslation
{
    /// <summary>
    /// Localized caption.
    /// </summary>
    public string? Caption { get; set; }

    /// <summary>
    /// Localized placeholder.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Localized help text.
    /// </summary>
    public string? HelpText { get; set; }

    /// <summary>
    /// Localized validation error messages.
    /// </summary>
    public IDictionary<string, string>? ValidationMessages { get; set; }

    /// <summary>
    /// Localized option texts.
    /// </summary>
    public IDictionary<string, string>? OptionTexts { get; set; }
}

/// <summary>
/// Result of translation update.
/// </summary>
public class TranslationUpdateResult
{
    /// <summary>
    /// Whether the update succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of translations updated.
    /// </summary>
    public int UpdatedCount { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result of translation import.
/// </summary>
public class TranslationImportResult
{
    /// <summary>
    /// Whether the import succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of strings imported.
    /// </summary>
    public int ImportedCount { get; set; }

    /// <summary>
    /// Number of strings skipped.
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// Errors encountered during import.
    /// </summary>
    public IReadOnlyList<string> Errors { get; set; } = [];
}

/// <summary>
/// Translation coverage statistics.
/// </summary>
public class TranslationCoverage
{
    /// <summary>
    /// Culture code.
    /// </summary>
    public string CultureCode { get; set; } = string.Empty;

    /// <summary>
    /// Total translatable strings.
    /// </summary>
    public int TotalStrings { get; set; }

    /// <summary>
    /// Number of translated strings.
    /// </summary>
    public int TranslatedStrings { get; set; }

    /// <summary>
    /// Coverage percentage (0-100).
    /// </summary>
    public double CoveragePercentage => TotalStrings > 0
        ? (double)TranslatedStrings / TotalStrings * 100
        : 0;

    /// <summary>
    /// Missing string keys.
    /// </summary>
    public IReadOnlyList<string> MissingKeys { get; set; } = [];
}
