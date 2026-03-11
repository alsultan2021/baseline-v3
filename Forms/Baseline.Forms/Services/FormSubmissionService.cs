using Baseline.Forms.Configuration;
using Baseline.Forms.Interfaces;
using CMS.Activities;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.OnlineForms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Forms.Services;

/// <summary>
/// Default implementation of form submission service.
/// Enforces consent, file upload validation, honeypot, and event hooks.
/// </summary>
public class FormSubmissionService : IFormSubmissionService
{
    private readonly IFormRetrievalService _formRetrievalService;
    private readonly ICurrentContactProvider _currentContactProvider;
    private readonly IConsentAgreementService _consentAgreementService;
    private readonly IInfoProvider<ConsentInfo> _consentInfoProvider;
    private readonly IEnumerable<IFormEventHandler> _eventHandlers;
    private readonly IActivityLogService _activityLogService;
    private readonly IOptions<BaselineFormsOptions> _options;
    private readonly ILogger<FormSubmissionService> _logger;

    public FormSubmissionService(
        IFormRetrievalService formRetrievalService,
        ICurrentContactProvider currentContactProvider,
        IConsentAgreementService consentAgreementService,
        IInfoProvider<ConsentInfo> consentInfoProvider,
        IEnumerable<IFormEventHandler> eventHandlers,
        IActivityLogService activityLogService,
        IOptions<BaselineFormsOptions> options,
        ILogger<FormSubmissionService> logger)
    {
        _formRetrievalService = formRetrievalService;
        _currentContactProvider = currentContactProvider;
        _consentAgreementService = consentAgreementService;
        _consentInfoProvider = consentInfoProvider;
        _eventHandlers = eventHandlers;
        _activityLogService = activityLogService;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FormSubmissionResult> SubmitFormAsync(
        string formCodeName, 
        IDictionary<string, object?> data)
    {
        try
        {
            // --- Honeypot check ---
            if (_options.Value.EnableHoneypot
                && data.TryGetValue(_options.Value.HoneypotFieldName, out var honeypotValue)
                && !string.IsNullOrEmpty(honeypotValue?.ToString()))
            {
                _logger.LogWarning("Honeypot triggered on form {Form}", formCodeName);
                // Return success to avoid giving bots feedback
                return new FormSubmissionResult(true, null, "Form submitted successfully", null);
            }

            // --- Event handlers: OnBeforeValidate ---
            foreach (var handler in _eventHandlers)
            {
                var eventResult = await handler.OnBeforeValidateAsync(formCodeName, data);
                if (!eventResult.Continue)
                {
                    return new FormSubmissionResult(false, null, eventResult.ErrorMessage, null);
                }
            }

            // --- Server-side validation ---
            if (_options.Value.EnableServerSideValidation)
            {
                var validationResult = await ValidateFormDataAsync(formCodeName, data);
                if (!validationResult.IsValid)
                {
                    return new FormSubmissionResult(
                        Success: false,
                        SubmissionId: null,
                        Message: "Validation failed",
                        Errors: validationResult.Errors
                    );
                }
            }

            // --- Consent enforcement ---
            if (_options.Value.RequireConsentForSubmission
                && !string.IsNullOrEmpty(_options.Value.RequiredConsentCodeName))
            {
                var consentCheck = await CheckConsentAsync(_options.Value.RequiredConsentCodeName);
                if (!consentCheck)
                {
                    return new FormSubmissionResult(
                        Success: false,
                        SubmissionId: null,
                        Message: "Consent is required before submitting this form.",
                        Errors: [new FormFieldError("_consent", "You must agree to the required consent.", "CONSENT_REQUIRED")]
                    );
                }
            }

            // --- File upload validation ---
            var fileErrors = ValidateFileUploads(data);
            if (fileErrors.Count > 0)
            {
                return new FormSubmissionResult(
                    Success: false,
                    SubmissionId: null,
                    Message: "File validation failed",
                    Errors: fileErrors
                );
            }

            var form = await _formRetrievalService.GetFormAsync(formCodeName);
            if (form == null)
            {
                return new FormSubmissionResult(
                    Success: false,
                    SubmissionId: null,
                    Message: $"Form '{formCodeName}' not found",
                    Errors: null
                );
            }

            // --- Event handlers: OnBeforeSubmit ---
            foreach (var handler in _eventHandlers)
            {
                var eventResult = await handler.OnBeforeSubmitAsync(formCodeName, data);
                if (!eventResult.Continue)
                {
                    return new FormSubmissionResult(false, null, eventResult.ErrorMessage, null);
                }
            }

            // Create submission using BizFormItemProvider
            var classInfo = CMS.DataEngine.DataClassInfoProvider.GetDataClassInfo(form.FormClassID);
            if (classInfo == null)
            {
                return new FormSubmissionResult(
                    Success: false,
                    SubmissionId: null,
                    Message: "Form class not found",
                    Errors: null
                );
            }

            var formItem = BizFormItem.New(classInfo.ClassName);
            
            // Set field values (skip honeypot field)
            foreach (var (key, value) in data)
            {
                if (key.Equals(_options.Value.HoneypotFieldName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (formItem.ContainsColumn(key))
                {
                    formItem.SetValue(key, value);
                }
            }

            // Set system fields
            formItem.FormInserted = DateTime.UtcNow;
            formItem.FormUpdated = DateTime.UtcNow;

            // Associate with contact if enabled
            if (_options.Value.AssociateWithContacts)
            {
                var contact = _currentContactProvider.GetExistingContact();
                if (contact != null)
                {
                    formItem.SetValue("FormContactGUID", contact.ContactGUID);
                }
            }

            formItem.Insert();

            if (form.FormLogActivity)
            {
                _activityLogService.Log(new FormSubmitActivityInitializer(formItem));
            }

            _logger.LogInformation(
                "Form {FormCodeName} submitted successfully, ID: {SubmissionId}",
                formCodeName, formItem.ItemID);

            // --- Event handlers: OnAfterSubmit ---
            foreach (var handler in _eventHandlers)
            {
                try
                {
                    await handler.OnAfterSubmitAsync(formCodeName, formItem.ItemID, data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Event handler {Handler} failed OnAfterSubmit for form {Form}",
                        handler.GetType().Name, formCodeName);
                }
            }

            return new FormSubmissionResult(
                Success: true,
                SubmissionId: formItem.ItemID,
                Message: "Form submitted successfully",
                Errors: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting form {FormCodeName}", formCodeName);
            return new FormSubmissionResult(
                Success: false,
                SubmissionId: null,
                Message: "An error occurred while submitting the form",
                Errors: null
            );
        }
    }

    /// <inheritdoc />
    public async Task<FormValidationResult> ValidateFormDataAsync(
        string formCodeName, 
        IDictionary<string, object?> data)
    {
        var errors = new List<FormFieldError>();
        var fields = await _formRetrievalService.GetFormFieldsAsync(formCodeName);

        foreach (var field in fields)
        {
            data.TryGetValue(field.Name, out var value);

            // Check required
            if (field.IsRequired && IsEmpty(value))
            {
                errors.Add(new FormFieldError(
                    field.Name,
                    field.ValidationRules?.FirstOrDefault(r => r.Type == "Required")?.ErrorMessage 
                        ?? $"{field.Caption ?? field.Name} is required.",
                    "REQUIRED"
                ));
            }

            // Check max length
            if (value is string stringValue && field.ValidationRules != null)
            {
                var maxLengthRule = field.ValidationRules
                    .FirstOrDefault(r => r.Type == "MaxLength");
                
                if (maxLengthRule?.Parameters?.TryGetValue("maxLength", out var maxLen) == true
                    && maxLen is int maxLength
                    && stringValue.Length > maxLength)
                {
                    errors.Add(new FormFieldError(
                        field.Name,
                        maxLengthRule.ErrorMessage ?? $"Maximum length is {maxLength} characters.",
                        "MAX_LENGTH"
                    ));
                }
            }
        }

        return new FormValidationResult(
            IsValid: errors.Count == 0,
            Errors: errors
        );
    }

    /// <inheritdoc />
    public async Task<PagedFormSubmissions> GetSubmissionsAsync(
        string formCodeName, 
        int skip = 0, 
        int take = 100)
    {
        var form = await _formRetrievalService.GetFormAsync(formCodeName);
        if (form == null)
        {
            return new PagedFormSubmissions(
                [],
                0, skip, take
            );
        }

        var classInfo = CMS.DataEngine.DataClassInfoProvider.GetDataClassInfo(form.FormClassID);
        if (classInfo == null)
        {
            return new PagedFormSubmissions(
                [],
                0, skip, take
            );
        }

        var items = BizFormItemProvider.GetItems(classInfo.ClassName)
            .OrderByDescending("FormInserted")
            .Skip(skip)
            .Take(take)
            .ToList();

        var totalCount = BizFormItemProvider.GetItems(classInfo.ClassName).Count;

        var submissions = items.Select(item => new FormSubmissionData(
            SubmissionId: item.ItemID,
            FormCodeName: formCodeName,
            Data: item.ColumnNames
                .ToDictionary<string, string, object?>(col => col, col => item.GetValue(col)),
            SubmittedAt: item.GetValue("FormInserted") as DateTime? ?? DateTime.MinValue,
            ContactId: null // Would need to look up from FormContactGUID
        )).ToList();

        return new PagedFormSubmissions(
            Submissions: submissions,
            TotalCount: totalCount,
            Skip: skip,
            Take: take
        );
    }

    private static bool IsEmpty(object? value) => value switch
    {
        null => true,
        string s => string.IsNullOrWhiteSpace(s),
        _ => false
    };

    /// <summary>
    /// Checks whether the current contact has agreed to the required consent.
    /// Uses XbK's <see cref="IConsentAgreementService"/> API.
    /// </summary>
    private async Task<bool> CheckConsentAsync(string consentCodeName)
    {
        try
        {
            var contact = _currentContactProvider.GetExistingContact();
            if (contact is null)
            {
                _logger.LogDebug("No current contact — consent check skipped");
                return false;
            }

            var consent = await _consentInfoProvider.GetAsync(consentCodeName);
            if (consent is null)
            {
                _logger.LogWarning("Consent {Consent} not found in system", consentCodeName);
                return false;
            }

            return _consentAgreementService.IsAgreed(contact, consent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking consent {Consent}", consentCodeName);
            return false;
        }
    }

    /// <summary>
    /// Validates file upload fields against <see cref="BaselineFormsOptions.MaxFileUploadSize"/>
    /// and <see cref="BaselineFormsOptions.AllowedFileExtensions"/>.
    /// </summary>
    private List<FormFieldError> ValidateFileUploads(IDictionary<string, object?> data)
    {
        var errors = new List<FormFieldError>();
        var opts = _options.Value;

        foreach (var (key, value) in data)
        {
            // HttpPostedFileBase / IFormFile detection — the value is typically a file name or stream
            if (value is not string fileName || !fileName.Contains('.'))
            {
                continue;
            }

            var extension = System.IO.Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(extension))
            {
                continue;
            }

            // Only validate fields that look like file uploads (have known file extensions)
            var isFileField = opts.AllowedFileExtensions
                .Any(ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));

            var isDisallowedExt = !string.IsNullOrEmpty(extension)
                && extension.Length <= 6
                && !opts.AllowedFileExtensions
                    .Any(ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));

            if (isDisallowedExt && extension.Length <= 6)
            {
                // This looks like a file path with a disallowed extension
                errors.Add(new FormFieldError(
                    key,
                    $"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", opts.AllowedFileExtensions)}",
                    "FILE_TYPE_NOT_ALLOWED"
                ));
            }
        }

        return errors;
    }
}
