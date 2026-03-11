namespace Baseline.Automation.Exceptions;

/// <summary>
/// Thrown when a contact cannot re-enter a process due to recurrence restrictions.
/// Maps to CMS.Automation.Internal.ProcessRecurrenceException.
/// </summary>
public class ProcessRecurrenceException : InvalidOperationException
{
    /// <summary>ID of the process.</summary>
    public Guid ProcessId { get; }

    /// <summary>Contact ID that was rejected.</summary>
    public int ContactId { get; }

    /// <summary>The recurrence mode that prevented re-entry.</summary>
    public ProcessRecurrence Recurrence { get; }

    public ProcessRecurrenceException(Guid processId, int contactId, ProcessRecurrence recurrence)
        : base($"Contact {contactId} cannot re-enter process '{processId}' due to recurrence setting '{recurrence}'.")
    {
        ProcessId = processId;
        ContactId = contactId;
        Recurrence = recurrence;
    }
}
