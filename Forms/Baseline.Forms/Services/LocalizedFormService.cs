using System.Text.RegularExpressions;
using Baseline.Forms.Interfaces;
using Baseline.Forms.Localization.Configuration;
using Baseline.Forms.Localization.Interfaces;
using Baseline.Forms.Localization.Models;
using CMS.OnlineForms;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Forms.Localization.Services;

/// <summary>
/// Service for retrieving forms with localized content.
/// </summary>
public class LocalizedFormService(
    IFormRetrievalService formRetrievalService,
    IResourceStringProvider resourceProvider,
    ICultureProvider cultureProvider,
    IMemoryCache cache,
    IOptions<LocalizedFormsOptions> options,
    ILogger<LocalizedFormService> logger) : ILocalizedFormService
{
    private readonly IFormRetrievalService _formRetrievalService = formRetrievalService;
    private readonly IResourceStringProvider _resourceProvider = resourceProvider;
    private readonly ICultureProvider _cultureProvider = cultureProvider;
    private readonly IMemoryCache _cache = cache;
    private readonly LocalizedFormsOptions _options = options.Value;
    private readonly ILogger<LocalizedFormService> _logger = logger;

    /// <inheritdoc />
    public async Task<LocalizedForm?> GetLocalizedFormAsync(string formCodeName, string? cultureCode = null)
    {
        var culture = cultureCode ?? _cultureProvider.GetCurrentCulture();
        var cacheKey = $"localized-form:{formCodeName}:{culture}";

        if (_cache.TryGetValue(cacheKey, out LocalizedForm? cached))
        {
            return cached;
        }

        var form = await _formRetrievalService.GetFormAsync(formCodeName);
        if (form == null)
        {
            _logger.LogWarning("Form {FormCodeName} not found", formCodeName);
            return null;
        }

        var fields = await _formRetrievalService.GetFormFieldsAsync(formCodeName);
        var localizedForm = new LocalizedForm
        {
            CodeName = formCodeName,
            DisplayName = await GetLocalizedStringAsync($"{formCodeName}.name", form.FormDisplayName, culture),
            CultureCode = culture,
            Description = await GetLocalizedStringAsync($"{formCodeName}.description", null, culture),
            SubmitButtonText = await GetLocalizedStringAsync($"{formCodeName}.submit", form.FormSubmitButtonText ?? "Submit", culture),
            SuccessMessage = await GetLocalizedStringAsync($"{formCodeName}.success", null, culture),
            ErrorMessage = await GetLocalizedStringAsync($"{formCodeName}.error", null, culture),
            AvailableCultures = (await GetAvailableCulturesAsync(formCodeName)).ToList()
        };

        var localizedFields = new List<LocalizedFormField>();
        foreach (var field in fields)
        {
            var localizedField = await LocalizeFieldAsync(formCodeName, field, culture);
            localizedFields.Add(localizedField);
        }
        localizedForm.Fields = localizedFields;

        _cache.Set(cacheKey, localizedForm, TimeSpan.FromMinutes(_options.CacheDurationMinutes));

        return localizedForm;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetAvailableCulturesAsync(string formCodeName)
    {
        var supportedCultures = await _cultureProvider.GetSupportedCulturesAsync();
        var availableCultures = new List<string>();

        foreach (var culture in supportedCultures)
        {
            // Check if any translation exists for this culture
            var key = $"{_options.ResourceKeyPrefix}{formCodeName}.name";
            var translation = await _resourceProvider.GetStringAsync(key, culture);
            if (!string.IsNullOrEmpty(translation) || culture == _options.DefaultCulture)
            {
                availableCultures.Add(culture);
            }
        }

        return availableCultures.Any() ? availableCultures : [_options.DefaultCulture];
    }

    /// <inheritdoc />
    public async Task<LocalizedFormField?> GetLocalizedFieldAsync(string formCodeName, string fieldName, string? cultureCode = null)
    {
        var culture = cultureCode ?? _cultureProvider.GetCurrentCulture();
        var fields = await _formRetrievalService.GetFormFieldsAsync(formCodeName);
        var field = fields.FirstOrDefault(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

        if (field == null)
        {
            return null;
        }

        return await LocalizeFieldAsync(formCodeName, field, culture);
    }

    /// <inheritdoc />
    public async Task<string> GetSubmitButtonTextAsync(string formCodeName, string? cultureCode = null)
    {
        var culture = cultureCode ?? _cultureProvider.GetCurrentCulture();
        var form = await _formRetrievalService.GetFormAsync(formCodeName);
        return await GetLocalizedStringAsync($"{formCodeName}.submit", form?.FormSubmitButtonText ?? "Submit", culture);
    }

    private async Task<LocalizedFormField> LocalizeFieldAsync(string formCodeName, FormFieldDefinition field, string culture)
    {
        var keyPrefix = $"{formCodeName}.fields.{field.Name}";

        var localizedField = new LocalizedFormField
        {
            Name = field.Name,
            Caption = await GetLocalizedStringAsync($"{keyPrefix}.caption", field.Caption ?? field.Name, culture),
            Placeholder = await GetLocalizedStringAsync($"{keyPrefix}.placeholder", null, culture),
            HelpText = await GetLocalizedStringAsync($"{keyPrefix}.help", null, culture),
            DataType = field.DataType,
            IsRequired = field.IsRequired,
            DefaultValue = field.DefaultValue,
            ComponentType = field.FormComponentType,
            Properties = field.Properties
        };

        // Localize validation rules
        if (field.ValidationRules?.Any() == true)
        {
            var localizedRules = new List<LocalizedValidationRule>();
            foreach (var rule in field.ValidationRules)
            {
                var localizedRule = new LocalizedValidationRule
                {
                    Type = rule.Type,
                    ErrorMessage = await GetLocalizedStringAsync(
                        $"{keyPrefix}.validation.{rule.Type.ToLowerInvariant()}",
                        rule.ErrorMessage ?? GetDefaultValidationMessage(rule.Type, localizedField.Caption),
                        culture),
                    Parameters = rule.Parameters
                };
                localizedRules.Add(localizedRule);
            }
            localizedField.ValidationRules = localizedRules;
        }

        // Localize options if any
        if (field.Properties?.TryGetValue("Options", out var optionsObj) == true && optionsObj is IEnumerable<object> options)
        {
            var localizedOptions = new List<LocalizedOption>();
            var index = 0;
            foreach (var option in options)
            {
                var optionValue = option.ToString() ?? index.ToString();
                localizedOptions.Add(new LocalizedOption
                {
                    Value = optionValue,
                    Text = await GetLocalizedStringAsync($"{keyPrefix}.options.{optionValue}", optionValue, culture)
                });
                index++;
            }
            localizedField.Options = localizedOptions;
        }

        return localizedField;
    }

    private async Task<string> GetLocalizedStringAsync(string key, string? defaultValue, string culture)
    {
        var fullKey = $"{_options.ResourceKeyPrefix}{key}";
        var value = await _resourceProvider.GetStringAsync(fullKey, culture);

        if (string.IsNullOrEmpty(value) && _options.FallbackToDefaultCulture && culture != _options.DefaultCulture)
        {
            value = await _resourceProvider.GetStringAsync(fullKey, _options.DefaultCulture);
        }

        return !string.IsNullOrEmpty(value) ? value : (defaultValue ?? string.Empty);
    }

    private static string GetDefaultValidationMessage(string ruleType, string fieldCaption)
    {
        return ruleType.ToLowerInvariant() switch
        {
            "required" => $"{fieldCaption} is required.",
            "email" => $"{fieldCaption} must be a valid email address.",
            "minlength" => $"{fieldCaption} is too short.",
            "maxlength" => $"{fieldCaption} is too long.",
            "regex" => $"{fieldCaption} has an invalid format.",
            "range" => $"{fieldCaption} is out of range.",
            _ => $"{fieldCaption} is invalid."
        };
    }
}

/// <summary>
/// Service for culture-specific form validation.
/// </summary>
public class LocalizedValidationService(
    IFormRetrievalService formRetrievalService,
    ILocalizedFormService localizedFormService,
    IResourceStringProvider resourceProvider,
    ICultureProvider cultureProvider,
    IOptions<LocalizedFormsOptions> options,
    ILogger<LocalizedValidationService> logger) : ILocalizedValidationService
{
    private readonly IFormRetrievalService _formRetrievalService = formRetrievalService;
    private readonly ILocalizedFormService _localizedFormService = localizedFormService;
    private readonly IResourceStringProvider _resourceProvider = resourceProvider;
    private readonly ICultureProvider _cultureProvider = cultureProvider;
    private readonly LocalizedFormsOptions _options = options.Value;
    private readonly ILogger<LocalizedValidationService> _logger = logger;

    /// <inheritdoc />
    public async Task<IDictionary<string, string>> GetValidationMessagesAsync(string? cultureCode = null)
    {
        var culture = cultureCode ?? _cultureProvider.GetCurrentCulture();
        var prefix = $"{_options.ResourceKeyPrefix}validation.";

        return await _resourceProvider.GetStringsByPrefixAsync(prefix, culture);
    }

    /// <inheritdoc />
    public Task<LocalizedValidationResult> ValidateAsync(
        string fieldName,
        object? value,
        string validationType,
        string? cultureCode = null)
    {
        var culture = cultureCode ?? _cultureProvider.GetCurrentCulture();
        var result = new LocalizedValidationResult { IsValid = true };

        var stringValue = value?.ToString();

        switch (validationType.ToLowerInvariant())
        {
            case "postalcode":
                result = ValidatePattern(stringValue, _options.ValidationPatterns.PostalCodePatterns, culture, fieldName, "PostalCode");
                break;

            case "phone":
                result = ValidatePattern(stringValue, _options.ValidationPatterns.PhonePatterns, culture, fieldName, "Phone");
                break;

            case "required":
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    result.IsValid = false;
                    result.FailedRule = "Required";
                    result.ErrorMessage = GetLocalizedErrorMessage("Required", fieldName, culture);
                }
                break;

            default:
                _logger.LogDebug("Unknown validation type {ValidationType} for field {FieldName}", validationType, fieldName);
                break;
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public async Task<LocalizedFormValidationResult> ValidateFormAsync(
        string formCodeName,
        IDictionary<string, object?> data,
        string? cultureCode = null)
    {
        var culture = cultureCode ?? _cultureProvider.GetCurrentCulture();
        var localizedForm = await _localizedFormService.GetLocalizedFormAsync(formCodeName, culture);

        if (localizedForm == null)
        {
            return new LocalizedFormValidationResult
            {
                IsValid = false,
                Errors = [new LocalizedFieldError
                {
                    FieldName = "_form",
                    FieldCaption = "Form",
                    ErrorMessage = "Form not found.",
                    RuleType = "FormNotFound"
                }]
            };
        }

        var errors = new List<LocalizedFieldError>();

        foreach (var field in localizedForm.Fields)
        {
            data.TryGetValue(field.Name, out var value);

            // Check required
            if (field.IsRequired && (value == null || string.IsNullOrWhiteSpace(value.ToString())))
            {
                errors.Add(new LocalizedFieldError
                {
                    FieldName = field.Name,
                    FieldCaption = field.Caption,
                    ErrorMessage = GetLocalizedErrorMessage("Required", field.Caption, culture),
                    RuleType = "Required"
                });
                continue;
            }

            // Check validation rules
            foreach (var rule in field.ValidationRules)
            {
                var validationResult = await ValidateRuleAsync(field, value, rule, culture);
                if (!validationResult.IsValid)
                {
                    errors.Add(new LocalizedFieldError
                    {
                        FieldName = field.Name,
                        FieldCaption = field.Caption,
                        ErrorMessage = validationResult.ErrorMessage ?? rule.ErrorMessage,
                        RuleType = rule.Type
                    });
                }
            }
        }

        return new LocalizedFormValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    /// <inheritdoc />
    public string GetLocalizedErrorMessage(
        string ruleType,
        string fieldCaption,
        string? cultureCode = null,
        params object[] parameters)
    {
        var culture = cultureCode ?? _cultureProvider.GetCurrentCulture();
        var key = $"{_options.ResourceKeyPrefix}validation.{ruleType.ToLowerInvariant()}";

        var template = _resourceProvider.GetString(key, culture);

        if (string.IsNullOrEmpty(template))
        {
            template = GetDefaultErrorTemplate(ruleType);
        }

        // Replace placeholders
        template = template.Replace("{field}", fieldCaption);
        for (var i = 0; i < parameters.Length; i++)
        {
            template = template.Replace($"{{{i}}}", parameters[i]?.ToString() ?? string.Empty);
        }

        return template;
    }

    private LocalizedValidationResult ValidatePattern(
        string? value,
        Dictionary<string, string> patterns,
        string culture,
        string fieldName,
        string ruleType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new LocalizedValidationResult { IsValid = true };
        }

        // Try exact culture match, then language only, then default
        var pattern = patterns.GetValueOrDefault(culture)
            ?? patterns.GetValueOrDefault(culture.Split('-')[0])
            ?? patterns.GetValueOrDefault(_options.DefaultCulture);

        if (pattern == null)
        {
            return new LocalizedValidationResult { IsValid = true };
        }

        var isValid = Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase);

        return new LocalizedValidationResult
        {
            IsValid = isValid,
            FailedRule = isValid ? null : ruleType,
            ErrorMessage = isValid ? null : GetLocalizedErrorMessage(ruleType, fieldName, culture)
        };
    }

    private async Task<LocalizedValidationResult> ValidateRuleAsync(
        LocalizedFormField field,
        object? value,
        LocalizedValidationRule rule,
        string culture)
    {
        var stringValue = value?.ToString() ?? string.Empty;

        return rule.Type.ToLowerInvariant() switch
        {
            "email" => ValidateEmail(stringValue, field.Caption, culture),
            "minlength" => ValidateMinLength(stringValue, field.Caption, rule.Parameters, culture),
            "maxlength" => ValidateMaxLength(stringValue, field.Caption, rule.Parameters, culture),
            "regex" => ValidateRegex(stringValue, field.Caption, rule.Parameters, culture),
            "postalcode" => await ValidateAsync(field.Name, value, "PostalCode", culture),
            "phone" => await ValidateAsync(field.Name, value, "Phone", culture),
            _ => new LocalizedValidationResult { IsValid = true }
        };
    }

    private LocalizedValidationResult ValidateEmail(string value, string fieldCaption, string culture)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new LocalizedValidationResult { IsValid = true };
        }

        var isValid = Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        return new LocalizedValidationResult
        {
            IsValid = isValid,
            FailedRule = isValid ? null : "Email",
            ErrorMessage = isValid ? null : GetLocalizedErrorMessage("Email", fieldCaption, culture)
        };
    }

    private LocalizedValidationResult ValidateMinLength(string value, string fieldCaption, IDictionary<string, object?>? parameters, string culture)
    {
        var minLength = 0;
        if (parameters?.TryGetValue("length", out var lengthValue) == true && lengthValue is int len)
        {
            minLength = len;
        }
        var isValid = value.Length >= minLength;
        return new LocalizedValidationResult
        {
            IsValid = isValid,
            FailedRule = isValid ? null : "MinLength",
            ErrorMessage = isValid ? null : GetLocalizedErrorMessage("MinLength", fieldCaption, culture, minLength)
        };
    }

    private LocalizedValidationResult ValidateMaxLength(string value, string fieldCaption, IDictionary<string, object?>? parameters, string culture)
    {
        var maxLength = int.MaxValue;
        if (parameters?.TryGetValue("length", out var lengthValue) == true && lengthValue is int len)
        {
            maxLength = len;
        }
        var isValid = value.Length <= maxLength;
        return new LocalizedValidationResult
        {
            IsValid = isValid,
            FailedRule = isValid ? null : "MaxLength",
            ErrorMessage = isValid ? null : GetLocalizedErrorMessage("MaxLength", fieldCaption, culture, maxLength)
        };
    }

    private LocalizedValidationResult ValidateRegex(string value, string fieldCaption, IDictionary<string, object?>? parameters, string culture)
    {
        string? pattern = null;
        if (parameters?.TryGetValue("pattern", out var patternValue) == true)
        {
            pattern = patternValue?.ToString();
        }
        if (string.IsNullOrEmpty(pattern))
        {
            return new LocalizedValidationResult { IsValid = true };
        }

        var isValid = Regex.IsMatch(value, pattern);
        return new LocalizedValidationResult
        {
            IsValid = isValid,
            FailedRule = isValid ? null : "Regex",
            ErrorMessage = isValid ? null : GetLocalizedErrorMessage("Regex", fieldCaption, culture)
        };
    }

    private static string GetDefaultErrorTemplate(string ruleType)
    {
        return ruleType.ToLowerInvariant() switch
        {
            "required" => "{field} is required.",
            "email" => "{field} must be a valid email address.",
            "minlength" => "{field} must be at least {0} characters.",
            "maxlength" => "{field} must be at most {0} characters.",
            "regex" => "{field} has an invalid format.",
            "postalcode" => "{field} must be a valid postal code.",
            "phone" => "{field} must be a valid phone number.",
            "range" => "{field} must be between {0} and {1}.",
            _ => "{field} is invalid."
        };
    }
}
