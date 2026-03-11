using Baseline.Forms.Configuration;
using Baseline.Forms.Interfaces;
using CMS.DataEngine;
using CMS.OnlineForms;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Forms.Services;

/// <summary>
/// Default implementation of form retrieval service.
/// </summary>
public class FormRetrievalService : IFormRetrievalService
{
    private readonly IInfoProvider<BizFormInfo> _formInfoProvider;
    private readonly IMemoryCache _cache;
    private readonly IOptions<BaselineFormsOptions> _options;
    private readonly ILogger<FormRetrievalService> _logger;

    public FormRetrievalService(
        IInfoProvider<BizFormInfo> formInfoProvider,
        IMemoryCache cache,
        IOptions<BaselineFormsOptions> options,
        ILogger<FormRetrievalService> logger)
    {
        _formInfoProvider = formInfoProvider;
        _cache = cache;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<BizFormInfo?> GetFormAsync(string formCodeName)
    {
        if (!_options.Value.EnableCaching)
        {
            return await FetchFormAsync(formCodeName);
        }

        var cacheKey = $"baseline_form_{formCodeName}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow =
                TimeSpan.FromMinutes(_options.Value.CacheDurationMinutes);
            entry.Size = 1;
            return await FetchFormAsync(formCodeName);
        });
    }

    private Task<BizFormInfo?> FetchFormAsync(string formCodeName)
    {
        var form = _formInfoProvider.Get()
            .WhereEquals(nameof(BizFormInfo.FormName), formCodeName)
            .FirstOrDefault();

        return Task.FromResult(form);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BizFormInfo>> GetAllFormsAsync()
    {
        var forms = _formInfoProvider.Get().ToList();
        return await Task.FromResult(forms);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FormFieldDefinition>> GetFormFieldsAsync(string formCodeName)
    {
        var form = await GetFormAsync(formCodeName);
        if (form == null)
        {
            return [];
        }

        var classInfo = CMS.DataEngine.DataClassInfoProvider.GetDataClassInfo(form.FormClassID);
        if (classInfo == null)
        {
            return [];
        }

        var formInfo = new CMS.FormEngine.FormInfo(classInfo.ClassFormDefinition);
        var fields = new List<FormFieldDefinition>();

        foreach (var field in formInfo.GetFields<CMS.FormEngine.FormFieldInfo>())
        {
            var validationRules = new List<FormValidationRule>();

            // Extract validation rules from field settings
            if (!field.AllowEmpty)
            {
                validationRules.Add(new FormValidationRule(
                    "Required",
                    "This field is required.",
                    null
                ));
            }

            // Check size for max length
            if (field.Size > 0 && field.DataType == "text")
            {
                validationRules.Add(new FormValidationRule(
                    "MaxLength",
                    $"Maximum length is {field.Size} characters.",
                    new Dictionary<string, object?> { ["maxLength"] = field.Size }
                ));
            }

            var properties = new Dictionary<string, object?>
            {
                ["Visible"] = field.Visible,
                ["Size"] = field.Size,
                ["Precision"] = field.Precision
            };

            fields.Add(new FormFieldDefinition(
                field.Name,
                field.GetPropertyValue(CMS.FormEngine.FormFieldPropertyEnum.FieldCaption)?.ToString(),
                field.DataType,
                !field.AllowEmpty,
                field.DefaultValue,
                validationRules,
                field.Settings?["controlname"]?.ToString(),
                properties
            ));
        }

        return fields;
    }

    /// <inheritdoc />
    public async Task<bool> FormExistsAsync(string formCodeName)
    {
        var form = await GetFormAsync(formCodeName);
        return form != null;
    }
}
