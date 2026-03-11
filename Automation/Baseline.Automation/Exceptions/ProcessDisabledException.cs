namespace Baseline.Automation.Exceptions;

/// <summary>
/// Thrown when an automation process is disabled and cannot accept new contacts.
/// Maps to CMS.Automation.Internal.ProcessDisabledException.
/// </summary>
public class ProcessDisabledException : InvalidOperationException
{
    /// <summary>ID of the disabled process.</summary>
    public Guid ProcessId { get; }

    public ProcessDisabledException(Guid processId)
        : base($"Automation process '{processId}' is disabled and cannot be executed.")
    {
        ProcessId = processId;
    }

    public ProcessDisabledException(Guid processId, string message) : base(message)
    {
        ProcessId = processId;
    }
}
