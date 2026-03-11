namespace Baseline.Automation;

/// <summary>
/// Core automation engine that orchestrates process execution.
/// Handles trigger dispatch, step advancement, and contact state management.
/// </summary>
public interface IAutomationEngine
{
    /// <summary>
    /// Fires a trigger event, starting matching automation processes for the contact.
    /// </summary>
    Task<int> FireTriggerAsync(TriggerEventData eventData);

    /// <summary>
    /// Advances a contact to the next step in their process.
    /// </summary>
    Task AdvanceContactAsync(Guid processContactStateId);

    /// <summary>
    /// Processes all contacts that are waiting and whose wait time has expired.
    /// </summary>
    Task<int> ProcessWaitingContactsAsync();

    /// <summary>
    /// Removes a contact from a running process.
    /// </summary>
    Task RemoveContactFromProcessAsync(Guid processId, int contactId);

    /// <summary>
    /// Gets the current state of a contact in a process.
    /// </summary>
    Task<ProcessContactState?> GetContactStateAsync(Guid processId, int contactId);

    /// <summary>
    /// Gets all active process contact states for a specific process.
    /// </summary>
    Task<IEnumerable<ProcessContactState>> GetActiveContactsInProcessAsync(Guid processId);
}

/// <summary>
/// Manages automation process definitions (CRUD).
/// </summary>
public interface IAutomationProcessService
{
    Task<AutomationProcess?> GetProcessAsync(Guid processId);
    Task<IEnumerable<AutomationProcess>> GetProcessesAsync(bool? enabledOnly = null);
    Task<IEnumerable<AutomationProcess>> GetProcessesByTriggerAsync(AutomationTriggerType triggerType);
    Task<AutomationProcess> CreateProcessAsync(AutomationProcess process);
    Task<AutomationProcess> UpdateProcessAsync(AutomationProcess process);
    Task EnableProcessAsync(Guid processId);
    Task DisableProcessAsync(Guid processId);
    Task DeleteProcessAsync(Guid processId);
}

/// <summary>
/// Executes a specific automation step action.
/// </summary>
public interface IAutomationActionExecutor
{
    /// <summary>The step type this executor handles.</summary>
    AutomationStepType StepType { get; }

    /// <summary>Executes the action for the given context.</summary>
    Task<StepExecutionResult> ExecuteAsync(AutomationContext context);
}

/// <summary>
/// Evaluates conditions for branching in automation processes.
/// </summary>
public interface IAutomationConditionEvaluator
{
    Task<bool> EvaluateAsync(int contactId, ConditionStepConfig config, string? triggerData = null);
}

/// <summary>
/// Handles validation and matching for automation triggers.
/// </summary>
public interface IAutomationTriggerHandler
{
    /// <summary>The trigger type this handler processes.</summary>
    AutomationTriggerType TriggerType { get; }

    /// <summary>Determines if the trigger event matches the process trigger configuration.</summary>
    Task<bool> MatchesAsync(AutomationTrigger trigger, TriggerEventData eventData);
}

/// <summary>
/// Repository for managing process contact state persistence.
/// </summary>
public interface IProcessStateRepository
{
    Task<ProcessContactState> CreateAsync(ProcessContactState state);
    Task<ProcessContactState> UpdateAsync(ProcessContactState state);
    Task<ProcessContactState?> GetByIdAsync(Guid id);
    Task<ProcessContactState?> GetByProcessAndContactAsync(Guid processId, int contactId);
    Task<IEnumerable<ProcessContactState>> GetActiveByProcessAsync(Guid processId);
    Task<IEnumerable<ProcessContactState>> GetExpiredWaitingStatesAsync();
    Task<int> GetExecutionCountAsync(Guid processId, int contactId);
    Task AddStepHistoryAsync(ProcessStepHistory history);
    Task<IEnumerable<ProcessStepHistory>> GetStepHistoryAsync(Guid processContactStateId);
}

/// <summary>
/// Repository for managing automation process persistence.
/// </summary>
public interface IProcessRepository
{
    Task<AutomationProcess?> GetByIdAsync(Guid processId);
    Task<IEnumerable<AutomationProcess>> GetAllAsync(bool? enabledOnly = null);
    Task<IEnumerable<AutomationProcess>> GetByTriggerTypeAsync(AutomationTriggerType triggerType);
    Task<AutomationProcess> SaveAsync(AutomationProcess process);
    Task DeleteAsync(Guid processId);
}
