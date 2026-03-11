using System.Globalization;
using Baseline.Forms.Interfaces;
using Baseline.Forms.Localization.Configuration;
using Baseline.Forms.Localization.Interfaces;
using Baseline.Forms.Localization.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Forms.Localization.Services;

/// <summary>
/// Service for submitting forms with culture-aware processing.
/// </summary>
public class LocalizedFormSubmissionService(
    IFormSubmissionService formSubmissionService,
    ILocalizedFormService localizedFormService,
    ILocalizedValidationService validationService,
    ICultureProvider cultureProvider,
    IResourceStringProvider resourceProvider,
    IOptions<LocalizedFormsOptions> options,
    ILogger<LocalizedFormSubmissionService> logger) : ILocalizedFormSubmissionService
{
    private readonly IFormSubmissionService _formSubmissionService = formSubmissionService;
    private readonly ILocalizedFormService _localizedFormService = localizedFormService;
    private readonly ILocalizedValidationService _validationService = validationService;
    private readonly ICultureProvider _cultureProvider = cultureProvider;
    private readonly IResourceStringProvider _resourceProvider = resourceProvider;
    private readonly LocalizedFormsOptions _options = options.Value;
    private readonly ILogger<LocalizedFormSubmissionService> _logger = logger;

    /// <inheritdoc />
    public async Task<LocalizedSubmissionResult> SubmitFormAsync(
        string formCodeName,
        IDictionary<string, object?> data,
        string? cultureCode = null)
    {
        var culture = cultureCode ?? _cultureProvider.GetCurrentCulture();

        _logger.LogInformation(
            "Submitting form {FormCodeName} with culture {Culture}",
            formCodeName,
            culture);

        // Validate form data
        var validationResult = await _validationService.ValidateFormAsync(formCodeName, data, culture);
        if (!validationResult.IsValid)
        {
            return new LocalizedSubmissionResult
            {
                Success = false,
                CultureCode = culture,
                Message = await GetErrorMessageAsync(formCodeName, culture),
                Errors = validationResult.Errors
            };
        }

        // Prepare culture-normalized data
        var normalizedData = await NormalizeDataForCultureAsync(formCodeName, data, culture);

        // Track submission culture if enabled
        if (_options.TrackSubmissionCulture)
        {
            normalizedData[_options.CultureFieldName] = culture;
        }

        try
        {
            // Submit to base form service
            var result = await _formSubmissionService.SubmitFormAsync(formCodeName, normalizedData);

            if (result.Success)
            {
                return new LocalizedSubmissionResult
                {
                    Success = true,
                    SubmissionId = result.SubmissionId,
                    CultureCode = culture,
                    Message = await GetSuccessMessageAsync(formCodeName, culture)
                };
            }

            // Map errors to localized field errors
            var localizedErrors = result.Errors?
                .Select(e => new LocalizedFieldError
                {
                    FieldName = e.FieldName,
                    FieldCaption = e.FieldName, // We'd need the form to get captions
                    ErrorMessage = e.ErrorMessage,
                    RuleType = e.ErrorCode ?? "Unknown"
                })
                .ToList();

            return new LocalizedSubmissionResult
            {
                Success = false,
                CultureCode = culture,
                Message = await GetErrorMessageAsync(formCodeName, culture),
                Errors = localizedErrors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting form {FormCodeName}", formCodeName);

            return new LocalizedSubmissionResult
            {
                Success = false,
                CultureCode = culture,
                Message = await GetErrorMessageAsync(formCodeName, culture)
            };
        }
    }

    /// <inheritdoc />
    public async Task<string> GetSuccessMessageAsync(string formCodeName, string? cultureCode = null)
    {
        var culture = cultureCode ?? _cultureProvider.GetCurrentCulture();
        var key = $"{_options.ResourceKeyPrefix}{formCodeName}.success";

        var message = await _resourceProvider.GetStringAsync(key, culture);

        if (string.IsNullOrEmpty(message) && _options.FallbackToDefaultCulture)
        {
            message = await _resourceProvider.GetStringAsync(key, _options.DefaultCulture);
        }

        return message ?? GetDefaultSuccessMessage(culture);
    }

    /// <inheritdoc />
    public async Task<string> GetErrorMessageAsync(string formCodeName, string? cultureCode = null)
    {
        var culture = cultureCode ?? _cultureProvider.GetCurrentCulture();
        var key = $"{_options.ResourceKeyPrefix}{formCodeName}.error";

        var message = await _resourceProvider.GetStringAsync(key, culture);

        if (string.IsNullOrEmpty(message) && _options.FallbackToDefaultCulture)
        {
            message = await _resourceProvider.GetStringAsync(key, _options.DefaultCulture);
        }

        return message ?? GetDefaultErrorMessage(culture);
    }

    private async Task<Dictionary<string, object?>> NormalizeDataForCultureAsync(
        string formCodeName,
        IDictionary<string, object?> data,
        string culture)
    {
        var normalizedData = new Dictionary<string, object?>(data);
        var localizedForm = await _localizedFormService.GetLocalizedFormAsync(formCodeName, culture);

        if (localizedForm == null)
        {
            return normalizedData;
        }

        // Normalize culture-specific data types
        var cultureInfo = new CultureInfo(culture);

        foreach (var field in localizedForm.Fields)
        {
            if (!normalizedData.TryGetValue(field.Name, out var value) || value == null)
            {
                continue;
            }

            var stringValue = value.ToString();
            if (string.IsNullOrEmpty(stringValue))
            {
                continue;
            }

            switch (field.DataType?.ToLowerInvariant())
            {
                case "decimal":
                case "double":
                case "float":
                    // Normalize decimal separator to invariant
                    if (decimal.TryParse(stringValue, NumberStyles.Any, cultureInfo, out var decimalValue))
                    {
                        normalizedData[field.Name] = decimalValue;
                    }
                    break;

                case "date":
                case "datetime":
                    // Parse with culture-specific format
                    var dateFormat = _options.ValidationPatterns.DateFormats.TryGetValue(culture, out var fmt) ? fmt
                        : (_options.ValidationPatterns.DateFormats.TryGetValue(_options.DefaultCulture, out var defFmt) ? defFmt : null);

                    if (!string.IsNullOrEmpty(dateFormat) &&
                        DateTime.TryParseExact(stringValue, dateFormat, cultureInfo, DateTimeStyles.None, out var dateValue))
                    {
                        normalizedData[field.Name] = dateValue;
                    }
                    else if (DateTime.TryParse(stringValue, cultureInfo, DateTimeStyles.None, out var parsedDate))
                    {
                        normalizedData[field.Name] = parsedDate;
                    }
                    break;

                case "integer":
                case "int":
                    if (int.TryParse(stringValue, NumberStyles.Any, cultureInfo, out var intValue))
                    {
                        normalizedData[field.Name] = intValue;
                    }
                    break;
            }
        }

        return normalizedData;
    }

    private static string GetDefaultSuccessMessage(string culture)
    {
        // Basic multi-language defaults
        return culture.Split('-')[0] switch
        {
            "en" => "Thank you for your submission.",
            "de" => "Vielen Dank für Ihre Einsendung.",
            "fr" => "Merci pour votre soumission.",
            "es" => "Gracias por su envío.",
            "cs" => "Děkujeme za vaše odeslání.",
            _ => "Thank you for your submission."
        };
    }

    private static string GetDefaultErrorMessage(string culture)
    {
        return culture.Split('-')[0] switch
        {
            "en" => "There was an error processing your submission. Please try again.",
            "de" => "Bei der Verarbeitung Ihrer Einreichung ist ein Fehler aufgetreten. Bitte versuchen Sie es erneut.",
            "fr" => "Une erreur s'est produite lors du traitement de votre soumission. Veuillez réessayer.",
            "es" => "Se produjo un error al procesar su envío. Por favor, inténtelo de nuevo.",
            "cs" => "Při zpracování vašeho odeslání došlo k chybě. Zkuste to prosím znovu.",
            _ => "There was an error processing your submission. Please try again."
        };
    }
}
