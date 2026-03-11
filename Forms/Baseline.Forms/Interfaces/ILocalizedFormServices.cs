using Baseline.Forms.Localization.Models;

namespace Baseline.Forms.Localization.Interfaces;

/// <summary>
/// Service for retrieving forms with localized field labels, placeholders, and validation messages.
/// </summary>
public interface ILocalizedFormService
{
    /// <summary>
    /// Gets a form with localized field labels and placeholders for the specified culture.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <param name="cultureCode">The culture code (e.g., "en-US", "fr-FR").</param>
    /// <returns>The localized form or null if not found.</returns>
    Task<LocalizedForm?> GetLocalizedFormAsync(string formCodeName, string? cultureCode = null);

    /// <summary>
    /// Gets available cultures for a form.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <returns>Collection of culture codes with translations.</returns>
    Task<IEnumerable<string>> GetAvailableCulturesAsync(string formCodeName);

    /// <summary>
    /// Gets a localized field definition.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <param name="fieldName">The field name.</param>
    /// <param name="cultureCode">The culture code.</param>
    /// <returns>The localized field definition.</returns>
    Task<LocalizedFormField?> GetLocalizedFieldAsync(string formCodeName, string fieldName, string? cultureCode = null);

    /// <summary>
    /// Gets the translated submit button text.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <param name="cultureCode">The culture code.</param>
    /// <returns>The localized submit button text.</returns>
    Task<string> GetSubmitButtonTextAsync(string formCodeName, string? cultureCode = null);
}

/// <summary>
/// Service for culture-specific form validation.
/// </summary>
public interface ILocalizedValidationService
{
    /// <summary>
    /// Gets localized validation messages for a culture.
    /// </summary>
    /// <param name="cultureCode">The culture code.</param>
    /// <returns>Dictionary of validation message keys and localized messages.</returns>
    Task<IDictionary<string, string>> GetValidationMessagesAsync(string? cultureCode = null);

    /// <summary>
    /// Validates a field value with culture-specific rules.
    /// </summary>
    /// <param name="fieldName">The field name.</param>
    /// <param name="value">The value to validate.</param>
    /// <param name="validationType">The validation type (e.g., "PostalCode", "Phone").</param>
    /// <param name="cultureCode">The culture code.</param>
    /// <returns>Validation result.</returns>
    Task<LocalizedValidationResult> ValidateAsync(
        string fieldName,
        object? value,
        string validationType,
        string? cultureCode = null);

    /// <summary>
    /// Validates all form fields with culture-specific rules.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <param name="data">The form data.</param>
    /// <param name="cultureCode">The culture code.</param>
    /// <returns>Validation result with localized error messages.</returns>
    Task<LocalizedFormValidationResult> ValidateFormAsync(
        string formCodeName,
        IDictionary<string, object?> data,
        string? cultureCode = null);

    /// <summary>
    /// Gets a localized error message for a validation rule.
    /// </summary>
    /// <param name="ruleType">The validation rule type.</param>
    /// <param name="fieldCaption">The field caption for substitution.</param>
    /// <param name="cultureCode">The culture code.</param>
    /// <param name="parameters">Additional parameters for message formatting.</param>
    /// <returns>The localized error message.</returns>
    string GetLocalizedErrorMessage(
        string ruleType,
        string fieldCaption,
        string? cultureCode = null,
        params object[] parameters);
}

/// <summary>
/// Service for submitting forms with multilingual support.
/// </summary>
public interface ILocalizedFormSubmissionService
{
    /// <summary>
    /// Submits form data with culture context.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <param name="data">The form data.</param>
    /// <param name="cultureCode">The submission culture.</param>
    /// <returns>Submission result with localized messages.</returns>
    Task<LocalizedSubmissionResult> SubmitFormAsync(
        string formCodeName,
        IDictionary<string, object?> data,
        string? cultureCode = null);

    /// <summary>
    /// Gets the success message for a form submission.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <param name="cultureCode">The culture code.</param>
    /// <returns>Localized success message.</returns>
    Task<string> GetSuccessMessageAsync(string formCodeName, string? cultureCode = null);

    /// <summary>
    /// Gets the error message for a form submission failure.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <param name="cultureCode">The culture code.</param>
    /// <returns>Localized error message.</returns>
    Task<string> GetErrorMessageAsync(string formCodeName, string? cultureCode = null);
}

/// <summary>
/// Service for managing form translations in the admin.
/// </summary>
public interface IFormTranslationService
{
    /// <summary>
    /// Gets all translatable strings for a form.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <returns>Collection of translatable form strings.</returns>
    Task<IEnumerable<FormTranslatableString>> GetTranslatableStringsAsync(string formCodeName);

    /// <summary>
    /// Updates translations for a form field.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <param name="fieldName">The field name.</param>
    /// <param name="translations">Dictionary of culture code to translation.</param>
    /// <returns>Update result.</returns>
    Task<TranslationUpdateResult> UpdateFieldTranslationsAsync(
        string formCodeName,
        string fieldName,
        IDictionary<string, FieldTranslation> translations);

    /// <summary>
    /// Exports form translations.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <param name="format">Export format (json, xliff, csv).</param>
    /// <returns>Export stream.</returns>
    Task<Stream> ExportTranslationsAsync(string formCodeName, string format = "json");

    /// <summary>
    /// Imports form translations.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <param name="stream">Import stream.</param>
    /// <param name="format">Import format.</param>
    /// <returns>Import result.</returns>
    Task<TranslationImportResult> ImportTranslationsAsync(string formCodeName, Stream stream, string format = "json");

    /// <summary>
    /// Gets translation coverage for a form.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <returns>Translation coverage by culture.</returns>
    Task<IDictionary<string, TranslationCoverage>> GetTranslationCoverageAsync(string formCodeName);
}
