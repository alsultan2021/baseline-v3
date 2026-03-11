using System.Text.Json;

namespace Baseline.Automation;

/// <summary>
/// Defines an automation process — a reusable workflow of steps
/// that runs for contacts who meet trigger conditions.
/// </summary>
public record AutomationProcess
{
    /// <summary>Unique identifier for the process.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Display name of the process.</summary>
    public required string Name { get; init; }

    /// <summary>Optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Whether the process is enabled and accepting new triggers.</summary>
    public bool IsEnabled { get; init; }

    /// <summary>Recurrence mode for the process.</summary>
    public ProcessRecurrence Recurrence { get; init; } = ProcessRecurrence.IfNotAlreadyRunning;

    /// <summary>The trigger that starts this process.</summary>
    public required AutomationTrigger Trigger { get; init; }

    /// <summary>Ordered list of steps in the process (after the trigger).</summary>
    public IList<AutomationStep> Steps { get; init; } = [];

    /// <summary>When the process was created.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the process was last modified.</summary>
    public DateTimeOffset? ModifiedAt { get; init; }

    /// <summary>User who created the process.</summary>
    public int? CreatedByUserId { get; init; }
}

/// <summary>
/// Defines a trigger that starts an automation process.
/// </summary>
public record AutomationTrigger
{
    /// <summary>Unique identifier for the trigger.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Display name for the trigger.</summary>
    public required string Name { get; init; }

    /// <summary>Type of trigger.</summary>
    public required AutomationTriggerType TriggerType { get; init; }

    /// <summary>
    /// Trigger-specific configuration as JSON.
    /// </summary>
    public string? Configuration { get; init; }

    /// <summary>
    /// Deserializes the configuration to a typed object.
    /// </summary>
    public T? GetConfiguration<T>() where T : class =>
        string.IsNullOrEmpty(Configuration) ? null :
        JsonSerializer.Deserialize<T>(Configuration, JsonOptions);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

/// <summary>
/// A single step in an automation process.
/// </summary>
public record AutomationStep
{
    /// <summary>Unique identifier for the step.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Display name for the step.</summary>
    public required string Name { get; init; }

    /// <summary>Type of step (action to perform).</summary>
    public required AutomationStepType StepType { get; init; }

    /// <summary>Order of the step within the process.</summary>
    public int Order { get; init; }

    /// <summary>
    /// Step-specific configuration as JSON.
    /// </summary>
    public string? Configuration { get; init; }

    /// <summary>
    /// For Condition steps: the ID of the step to go to when condition is true.
    /// If null, continues to the next sequential step.
    /// </summary>
    public Guid? TrueBranchStepId { get; init; }

    /// <summary>
    /// For Condition steps: the ID of the step to go to when condition is false.
    /// If null, continues to the next sequential step.
    /// </summary>
    public Guid? FalseBranchStepId { get; init; }

    /// <summary>
    /// Deserializes the configuration to a typed object.
    /// </summary>
    public T? GetConfiguration<T>() where T : class =>
        string.IsNullOrEmpty(Configuration) ? null :
        JsonSerializer.Deserialize<T>(Configuration, JsonOptions);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

/// <summary>
/// Tracks a contact's progress through an automation process.
/// </summary>
public record ProcessContactState
{
    /// <summary>Unique identifier for this state record.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The automation process ID.</summary>
    public required Guid ProcessId { get; init; }

    /// <summary>The contact ID being processed.</summary>
    public required int ContactId { get; init; }

    /// <summary>The current step ID where the contact is.</summary>
    public required Guid CurrentStepId { get; init; }

    /// <summary>Status of the contact in the process.</summary>
    public ProcessContactStatus Status { get; init; } = ProcessContactStatus.Active;

    /// <summary>When the contact entered the process.</summary>
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the contact entered the current step.</summary>
    public DateTimeOffset StepEnteredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the contact should be advanced (for Wait steps).</summary>
    public DateTimeOffset? WaitUntil { get; init; }

    /// <summary>When the process finished for this contact.</summary>
    public DateTimeOffset? FinishedAt { get; init; }

    /// <summary>Number of times the process has run for this contact.</summary>
    public int ExecutionCount { get; init; } = 1;

    /// <summary>Last error message if the process failed for this contact.</summary>
    public string? LastError { get; init; }

    /// <summary>
    /// Contextual data passed through the process (e.g., form data).
    /// Stored as JSON.
    /// </summary>
    public string? ContextData { get; init; }
}

/// <summary>
/// History record of a contact passing through a step.
/// </summary>
public record ProcessStepHistory
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The process contact state ID.</summary>
    public required Guid ProcessContactStateId { get; init; }

    /// <summary>The step that was executed.</summary>
    public required Guid StepId { get; init; }

    /// <summary>Step type for quick filtering.</summary>
    public required AutomationStepType StepType { get; init; }

    /// <summary>When the step was entered.</summary>
    public DateTimeOffset EnteredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the step was completed.</summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>Whether the step executed successfully.</summary>
    public bool Success { get; init; } = true;

    /// <summary>Result or error details.</summary>
    public string? ResultDetails { get; init; }
}
