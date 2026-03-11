using CMS.OnlineForms;

namespace Baseline.Forms.Interfaces;

/// <summary>
/// Service for retrieving form definitions and metadata.
/// </summary>
public interface IFormRetrievalService
{
    /// <summary>
    /// Gets a form by its code name.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <returns>The form info or null if not found.</returns>
    Task<BizFormInfo?> GetFormAsync(string formCodeName);

    /// <summary>
    /// Gets all available forms.
    /// </summary>
    /// <returns>Collection of form info objects.</returns>
    Task<IEnumerable<BizFormInfo>> GetAllFormsAsync();

    /// <summary>
    /// Gets form field definitions.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <returns>Collection of field definitions.</returns>
    Task<IEnumerable<FormFieldDefinition>> GetFormFieldsAsync(string formCodeName);

    /// <summary>
    /// Checks if a form exists.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <returns>True if the form exists.</returns>
    Task<bool> FormExistsAsync(string formCodeName);
}

/// <summary>
/// Service for handling form submissions.
/// </summary>
public interface IFormSubmissionService
{
    /// <summary>
    /// Submits form data.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <param name="data">The form data dictionary.</param>
    /// <returns>The submission result.</returns>
    Task<FormSubmissionResult> SubmitFormAsync(string formCodeName, IDictionary<string, object?> data);

    /// <summary>
    /// Validates form data before submission.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <param name="data">The form data dictionary.</param>
    /// <returns>Validation result.</returns>
    Task<FormValidationResult> ValidateFormDataAsync(string formCodeName, IDictionary<string, object?> data);

    /// <summary>
    /// Gets submissions for a form.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <returns>Paged result of submissions.</returns>
    Task<PagedFormSubmissions> GetSubmissionsAsync(string formCodeName, int skip = 0, int take = 100);
}

/// <summary>
/// Service for form autoresponders and notifications.
/// </summary>
public interface IFormAutoresponderService
{
    /// <summary>
    /// Sends autoresponder email after form submission.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <param name="recipientEmail">The recipient email.</param>
    /// <param name="formData">The submitted form data.</param>
    Task SendAutoresponderAsync(string formCodeName, string recipientEmail, IDictionary<string, object?> formData);

    /// <summary>
    /// Sends notification email to administrators.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <param name="formData">The submitted form data.</param>
    Task SendNotificationAsync(string formCodeName, IDictionary<string, object?> formData);
}

/// <summary>
/// Form field definition.
/// </summary>
/// <param name="Name">Field name.</param>
/// <param name="Caption">Display caption.</param>
/// <param name="DataType">Data type of the field.</param>
/// <param name="IsRequired">Whether the field is required.</param>
/// <param name="DefaultValue">Default value.</param>
/// <param name="ValidationRules">Validation rules.</param>
/// <param name="FormComponentType">Form component type identifier.</param>
/// <param name="Properties">Additional component properties.</param>
public record FormFieldDefinition(
    string Name,
    string? Caption,
    string DataType,
    bool IsRequired,
    object? DefaultValue,
    IEnumerable<FormValidationRule>? ValidationRules,
    string? FormComponentType,
    IDictionary<string, object?>? Properties
);

/// <summary>
/// Form validation rule.
/// </summary>
/// <param name="Type">Validation rule type.</param>
/// <param name="ErrorMessage">Error message.</param>
/// <param name="Parameters">Rule parameters.</param>
public record FormValidationRule(
    string Type,
    string? ErrorMessage,
    IDictionary<string, object?>? Parameters
);

/// <summary>
/// Form submission result.
/// </summary>
/// <param name="Success">Whether submission succeeded.</param>
/// <param name="SubmissionId">ID of the created submission.</param>
/// <param name="Message">Result message.</param>
/// <param name="Errors">Validation errors if any.</param>
public record FormSubmissionResult(
    bool Success,
    int? SubmissionId,
    string? Message,
    IEnumerable<FormFieldError>? Errors
);

/// <summary>
/// Form validation result.
/// </summary>
/// <param name="IsValid">Whether the data is valid.</param>
/// <param name="Errors">Validation errors.</param>
public record FormValidationResult(
    bool IsValid,
    IEnumerable<FormFieldError> Errors
);

/// <summary>
/// Form field error.
/// </summary>
/// <param name="FieldName">Name of the field with error.</param>
/// <param name="ErrorMessage">Error message.</param>
/// <param name="ErrorCode">Error code for programmatic handling.</param>
public record FormFieldError(
    string FieldName,
    string ErrorMessage,
    string? ErrorCode
);

/// <summary>
/// Paged form submissions result.
/// </summary>
/// <param name="Submissions">The submissions.</param>
/// <param name="TotalCount">Total number of submissions.</param>
/// <param name="Skip">Number skipped.</param>
/// <param name="Take">Number taken.</param>
public record PagedFormSubmissions(
    IEnumerable<FormSubmissionData> Submissions,
    int TotalCount,
    int Skip,
    int Take
);

/// <summary>
/// Form submission data.
/// </summary>
/// <param name="SubmissionId">The submission ID.</param>
/// <param name="FormCodeName">The form code name.</param>
/// <param name="Data">The submitted data.</param>
/// <param name="SubmittedAt">When the form was submitted.</param>
/// <param name="ContactId">Associated contact ID if available.</param>
public record FormSubmissionData(
    int SubmissionId,
    string FormCodeName,
    IDictionary<string, object?> Data,
    DateTime SubmittedAt,
    int? ContactId
);
