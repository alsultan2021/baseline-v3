namespace Baseline.Core.Admin.Sms;

/// <summary>
/// Result state for modification operations.
/// </summary>
public enum ModificationResultState
{
    /// <summary>Operation completed successfully.</summary>
    Success,
    /// <summary>Operation failed.</summary>
    Failure
}

/// <summary>
/// Result of a modification operation.
/// </summary>
/// <param name="resultState">The result state.</param>
/// <param name="message">Optional message.</param>
public class ModificationResult(ModificationResultState resultState, string? message = null)
{
    /// <summary>Gets or sets the result state.</summary>
    public ModificationResultState ModificationResultState { get; set; } = resultState;
    /// <summary>Gets or sets the message.</summary>
    public string? Message { get; set; } = message;
}
