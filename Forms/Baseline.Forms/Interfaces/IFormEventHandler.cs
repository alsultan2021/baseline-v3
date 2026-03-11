namespace Baseline.Forms.Interfaces;

/// <summary>
/// Extensibility hook for form submission lifecycle events.
/// Register multiple implementations to run custom logic before/after submission.
/// </summary>
public interface IFormEventHandler
{
    /// <summary>
    /// Called before form data is validated. Return false to abort submission.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <param name="data">Mutable form data — handlers may modify values.</param>
    /// <returns>True to continue, false to abort.</returns>
    Task<FormEventResult> OnBeforeValidateAsync(string formCodeName, IDictionary<string, object?> data);

    /// <summary>
    /// Called after validation but before data is persisted. Return false to abort submission.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <param name="data">The validated form data.</param>
    /// <returns>True to continue, false to abort.</returns>
    Task<FormEventResult> OnBeforeSubmitAsync(string formCodeName, IDictionary<string, object?> data);

    /// <summary>
    /// Called after form data has been successfully persisted.
    /// </summary>
    /// <param name="formCodeName">The form code name.</param>
    /// <param name="submissionId">The ID of the new submission.</param>
    /// <param name="data">The submitted form data.</param>
    Task OnAfterSubmitAsync(string formCodeName, int submissionId, IDictionary<string, object?> data);
}

/// <summary>
/// Result of a form event handler invocation.
/// </summary>
/// <param name="Continue">Whether to continue the submission pipeline.</param>
/// <param name="ErrorMessage">Optional error message when <paramref name="Continue"/> is false.</param>
public record FormEventResult(bool Continue, string? ErrorMessage = null)
{
    /// <summary>
    /// Allow submission to proceed.
    /// </summary>
    public static FormEventResult Ok => new(true);

    /// <summary>
    /// Abort submission with an error.
    /// </summary>
    public static FormEventResult Abort(string message) => new(false, message);
}
