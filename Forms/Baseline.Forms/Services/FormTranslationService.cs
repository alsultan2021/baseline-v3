using Baseline.Forms.Interfaces;
using Baseline.Forms.Localization.Configuration;
using Baseline.Forms.Localization.Interfaces;
using Baseline.Forms.Localization.Models;
using Baseline.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Forms.Localization.Services;

/// <summary>
/// Service for managing form translations.
/// </summary>
public class FormTranslationService(
    IFormRetrievalService formRetrievalService,
    ICultureProvider cultureProvider,
    IResourceStringProvider resourceProvider,
    IResourceStringService? resourceStringService,
    IOptions<LocalizedFormsOptions> options,
    ILogger<FormTranslationService> logger) : IFormTranslationService
{
    private readonly IFormRetrievalService _formRetrievalService = formRetrievalService;
    private readonly ICultureProvider _cultureProvider = cultureProvider;
    private readonly IResourceStringProvider _resourceProvider = resourceProvider;
    private readonly IResourceStringService? _resourceStringService = resourceStringService;
    private readonly LocalizedFormsOptions _options = options.Value;
    private readonly ILogger<FormTranslationService> _logger = logger;

    /// <inheritdoc />
    public async Task<IEnumerable<FormTranslatableString>> GetTranslatableStringsAsync(string formCodeName)
    {
        var form = await _formRetrievalService.GetFormAsync(formCodeName);
        if (form == null)
        {
            return [];
        }

        var strings = new List<FormTranslatableString>();
        var supportedCultures = (await _cultureProvider.GetSupportedCulturesAsync()).ToList();

        // Form-level strings
        strings.Add(await CreateTranslatableStringAsync(
            $"{formCodeName}.name",
            FormStringType.FormName,
            null,
            form.FormDisplayName,
            supportedCultures));

        strings.Add(await CreateTranslatableStringAsync(
            $"{formCodeName}.submit",
            FormStringType.SubmitButton,
            null,
            "Submit",
            supportedCultures));

        strings.Add(await CreateTranslatableStringAsync(
            $"{formCodeName}.success",
            FormStringType.SuccessMessage,
            null,
            null,
            supportedCultures));

        strings.Add(await CreateTranslatableStringAsync(
            $"{formCodeName}.error",
            FormStringType.ErrorMessage,
            null,
            null,
            supportedCultures));

        // Field-level strings
        var fields = await _formRetrievalService.GetFormFieldsAsync(formCodeName);
        foreach (var field in fields)
        {
            var fieldPrefix = $"{formCodeName}.fields.{field.Name}";

            strings.Add(await CreateTranslatableStringAsync(
                $"{fieldPrefix}.caption",
                FormStringType.FieldCaption,
                field.Name,
                field.Caption ?? field.Name,
                supportedCultures));

            strings.Add(await CreateTranslatableStringAsync(
                $"{fieldPrefix}.placeholder",
                FormStringType.FieldPlaceholder,
                field.Name,
                null,
                supportedCultures));

            strings.Add(await CreateTranslatableStringAsync(
                $"{fieldPrefix}.help",
                FormStringType.FieldHelpText,
                field.Name,
                null,
                supportedCultures));

            // Validation messages
            if (field.ValidationRules?.Any() == true)
            {
                foreach (var rule in field.ValidationRules)
                {
                    strings.Add(await CreateTranslatableStringAsync(
                        $"{fieldPrefix}.validation.{rule.Type.ToLowerInvariant()}",
                        FormStringType.ValidationMessage,
                        field.Name,
                        rule.ErrorMessage,
                        supportedCultures));
                }
            }
        }

        return strings;
    }

    /// <inheritdoc />
    public async Task<TranslationUpdateResult> UpdateFieldTranslationsAsync(
        string formCodeName,
        string fieldName,
        IDictionary<string, FieldTranslation> translations)
    {
        var result = new TranslationUpdateResult();

        var form = await _formRetrievalService.GetFormAsync(formCodeName);
        if (form == null)
        {
            result.Success = false;
            result.ErrorMessage = "Form not found.";
            return result;
        }

        var keyPrefix = $"{_options.ResourceKeyPrefix}{formCodeName}.fields.{fieldName}";
        var updatedCount = 0;

        foreach (var (culture, translation) in translations)
        {
            try
            {
                if (!string.IsNullOrEmpty(translation.Caption))
                {
                    await UpdateResourceStringAsync($"{keyPrefix}.caption", culture, translation.Caption);
                    updatedCount++;
                }

                if (!string.IsNullOrEmpty(translation.Placeholder))
                {
                    await UpdateResourceStringAsync($"{keyPrefix}.placeholder", culture, translation.Placeholder);
                    updatedCount++;
                }

                if (!string.IsNullOrEmpty(translation.HelpText))
                {
                    await UpdateResourceStringAsync($"{keyPrefix}.help", culture, translation.HelpText);
                    updatedCount++;
                }

                if (translation.ValidationMessages?.Any() == true)
                {
                    foreach (var (ruleType, message) in translation.ValidationMessages)
                    {
                        await UpdateResourceStringAsync(
                            $"{keyPrefix}.validation.{ruleType.ToLowerInvariant()}",
                            culture,
                            message);
                        updatedCount++;
                    }
                }

                if (translation.OptionTexts?.Any() == true)
                {
                    foreach (var (optionValue, text) in translation.OptionTexts)
                    {
                        await UpdateResourceStringAsync(
                            $"{keyPrefix}.options.{optionValue}",
                            culture,
                            text);
                        updatedCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating translations for {Field} in {Culture}", fieldName, culture);
                result.Success = false;
                result.ErrorMessage = $"Error updating {culture}: {ex.Message}";
                return result;
            }
        }

        result.Success = true;
        result.UpdatedCount = updatedCount;
        return result;
    }

    /// <inheritdoc />
    public async Task<Stream> ExportTranslationsAsync(string formCodeName, string format = "json")
    {
        var strings = await GetTranslatableStringsAsync(formCodeName);

        var content = format.ToLowerInvariant() switch
        {
            "json" => System.Text.Json.JsonSerializer.Serialize(strings, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            }),
            "csv" => ExportToCsv(strings),
            "xliff" => ExportToXliff(formCodeName, strings),
            _ => throw new ArgumentException($"Unsupported export format: {format}")
        };

        var memoryStream = new MemoryStream();
        var writer = new StreamWriter(memoryStream, System.Text.Encoding.UTF8, leaveOpen: true);
        await writer.WriteAsync(content);
        await writer.FlushAsync();
        memoryStream.Position = 0;
        return memoryStream;
    }

    /// <inheritdoc />
    public async Task<TranslationImportResult> ImportTranslationsAsync(string formCodeName, Stream stream, string format = "json")
    {
        var result = new TranslationImportResult();
        var errors = new List<string>();

        try
        {
            using var reader = new StreamReader(stream);
            var data = await reader.ReadToEndAsync();

            var translations = format.ToLowerInvariant() switch
            {
                "json" => System.Text.Json.JsonSerializer.Deserialize<List<FormTranslatableString>>(data),
                _ => throw new ArgumentException($"Unsupported import format: {format}")
            };

            if (translations == null)
            {
                errors.Add("Failed to parse translation data.");
                result.Errors = errors;
                return result;
            }

            var importedCount = 0;
            var skippedCount = 0;

            foreach (var item in translations)
            {
                foreach (var (culture, value) in item.Translations)
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        skippedCount++;
                        continue;
                    }

                    try
                    {
                        await UpdateResourceStringAsync(
                            $"{_options.ResourceKeyPrefix}{item.Key}",
                            culture,
                            value);

                        importedCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error importing {item.Key} for {culture}: {ex.Message}");
                        skippedCount++;
                    }
                }
            }

            result.Success = errors.Count == 0;
            result.ImportedCount = importedCount;
            result.SkippedCount = skippedCount;
            result.Errors = errors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing translations for form {FormCodeName}", formCodeName);
            errors.Add($"Import error: {ex.Message}");
            result.Errors = errors;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IDictionary<string, TranslationCoverage>> GetTranslationCoverageAsync(string formCodeName)
    {
        var strings = (await GetTranslatableStringsAsync(formCodeName)).ToList();
        var supportedCultures = (await _cultureProvider.GetSupportedCulturesAsync()).ToList();
        var totalStrings = strings.Count;

        var coverageByCulture = new Dictionary<string, TranslationCoverage>();

        foreach (var culture in supportedCultures)
        {
            var translatedCount = strings.Count(s =>
                s.Translations.TryGetValue(culture, out var value) &&
                !string.IsNullOrEmpty(value));

            // Find missing translations for this culture
            var missingKeys = strings
                .Where(s => !s.Translations.TryGetValue(culture, out var v) || string.IsNullOrEmpty(v))
                .Select(s => s.Key)
                .ToList();

            coverageByCulture[culture] = new TranslationCoverage
            {
                CultureCode = culture,
                TotalStrings = totalStrings,
                TranslatedStrings = translatedCount,
                MissingKeys = missingKeys
            };
        }

        return coverageByCulture;
    }

    private async Task<FormTranslatableString> CreateTranslatableStringAsync(
        string key,
        FormStringType type,
        string? fieldName,
        string? defaultValue,
        List<string> cultures)
    {
        var item = new FormTranslatableString
        {
            Key = key,
            StringType = type,
            FieldName = fieldName,
            DefaultValue = defaultValue ?? string.Empty,
            Translations = new Dictionary<string, string>()
        };

        // Get existing translations
        foreach (var culture in cultures)
        {
            var resourceKey = $"{_options.ResourceKeyPrefix}{key}";
            var value = await _resourceProvider.GetStringAsync(resourceKey, culture);

            if (culture == _options.DefaultCulture && string.IsNullOrEmpty(value))
            {
                value = defaultValue;
            }

            item.Translations[culture] = value ?? string.Empty;
        }

        return item;
    }

    private async Task UpdateResourceStringAsync(string key, string culture, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (_resourceStringService is null)
        {
            _logger.LogWarning(
                "IResourceStringService not registered — cannot persist translation {Key} for {Culture}",
                key, culture);
            return;
        }

        // Fetch existing resource string or create new one
        var existing = await _resourceStringService.GetAsync(key);
        var resourceString = existing ?? new ResourceString
        {
            Key = key,
            Category = "forms",
            Description = $"Form translation for {key}"
        };

        resourceString.Translations[culture] = value;
        resourceString.LastModified = DateTimeOffset.UtcNow;

        var result = await _resourceStringService.SaveAsync(resourceString);
        if (!result.Success)
        {
            _logger.LogError("Failed to save resource string {Key} for {Culture}: {Error}",
                key, culture, result.ErrorMessage);
        }
    }

    private static string ExportToCsv(IEnumerable<FormTranslatableString> strings)
    {
        var stringsList = strings.ToList();
        var cultures = stringsList.SelectMany(s => s.Translations.Keys).Distinct().ToList();
        var sb = new System.Text.StringBuilder();

        // Header
        sb.AppendLine($"Key,Type,FieldName,{string.Join(",", cultures)}");

        // Data
        foreach (var str in stringsList)
        {
            var values = cultures.Select(c =>
                str.Translations.TryGetValue(c, out var v)
                    ? $"\"{v?.Replace("\"", "\"\"")}\""
                    : "\"\"");

            sb.AppendLine($"\"{str.Key}\",\"{str.StringType}\",\"{str.FieldName}\",{string.Join(",", values)}");
        }

        return sb.ToString();
    }

    private static string ExportToXliff(string formCodeName, IEnumerable<FormTranslatableString> strings)
    {
        // Basic XLIFF 1.2 format
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<xliff version=\"1.2\" xmlns=\"urn:oasis:names:tc:xliff:document:1.2\">");
        sb.AppendLine($"  <file source-language=\"en\" datatype=\"plaintext\" original=\"{formCodeName}\">");
        sb.AppendLine("    <body>");

        foreach (var str in strings)
        {
            var defaultValue = str.Translations.Values.FirstOrDefault(v => !string.IsNullOrEmpty(v));
            sb.AppendLine($"      <trans-unit id=\"{str.Key}\">");
            sb.AppendLine($"        <source>{System.Security.SecurityElement.Escape(defaultValue ?? string.Empty)}</source>");
            sb.AppendLine("      </trans-unit>");
        }

        sb.AppendLine("    </body>");
        sb.AppendLine("  </file>");
        sb.AppendLine("</xliff>");

        return sb.ToString();
    }
}
