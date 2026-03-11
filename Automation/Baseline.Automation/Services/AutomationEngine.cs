using Microsoft.Extensions.Logging;

namespace Baseline.Automation.Services;

/// <summary>
/// Core automation engine that orchestrates process execution.
/// Dispatches triggers to matching processes, executes steps, and manages contact state.
/// </summary>
public class AutomationEngine(
    IProcessRepository processRepository,
    IProcessStateRepository stateRepository,
    IEnumerable<IAutomationActionExecutor> actionExecutors,
    IEnumerable<IAutomationTriggerHandler> triggerHandlers,
    ILogger<AutomationEngine> logger) : IAutomationEngine
{
    private readonly Dictionary<AutomationStepType, IAutomationActionExecutor> _executors =
        actionExecutors.ToDictionary(e => e.StepType);

    private readonly Dictionary<AutomationTriggerType, IAutomationTriggerHandler> _handlers =
        triggerHandlers.ToDictionary(h => h.TriggerType);

    /// <inheritdoc/>
    public async Task<int> FireTriggerAsync(TriggerEventData eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        logger.LogInformation(
            "Trigger fired: {TriggerType} for contact {ContactId}",
            eventData.TriggerType, eventData.ContactId);

        var processes = await processRepository.GetByTriggerTypeAsync(eventData.TriggerType);
        var enabledProcesses = processes.Where(p => p.IsEnabled).ToList();

        if (enabledProcesses.Count == 0)
        {
            logger.LogDebug("No enabled processes found for trigger type {TriggerType}", eventData.TriggerType);
            return 0;
        }

        int startedCount = 0;

        foreach (var process in enabledProcesses)
        {
            try
            {
                if (_handlers.TryGetValue(eventData.TriggerType, out var handler))
                {
                    if (!await handler.MatchesAsync(process.Trigger, eventData))
                    {
                        logger.LogDebug(
                            "Trigger {TriggerType} did not match process {ProcessName}",
                            eventData.TriggerType, process.Name);
                        continue;
                    }
                }

                if (!await CanStartProcessForContactAsync(process, eventData.ContactId))
                {
                    logger.LogDebug(
                        "Process {ProcessName} cannot start for contact {ContactId} due to recurrence rules",
                        process.Name, eventData.ContactId);
                    continue;
                }

                await StartProcessForContactAsync(process, eventData);
                startedCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error processing trigger for process {ProcessName}, contact {ContactId}",
                    process.Name, eventData.ContactId);
            }
        }

        logger.LogInformation(
            "Started {Count} processes for trigger {TriggerType}, contact {ContactId}",
            startedCount, eventData.TriggerType, eventData.ContactId);

        return startedCount;
    }

    /// <inheritdoc/>
    public async Task AdvanceContactAsync(Guid processContactStateId)
    {
        var state = await stateRepository.GetByIdAsync(processContactStateId);
        if (state == null)
        {
            logger.LogWarning("Contact state {StateId} not found", processContactStateId);
            return;
        }

        if (state.Status is ProcessContactStatus.Finished or ProcessContactStatus.Removed or ProcessContactStatus.Failed)
        {
            logger.LogDebug("Contact state {StateId} is in terminal status {Status}", processContactStateId, state.Status);
            return;
        }

        var process = await processRepository.GetByIdAsync(state.ProcessId);
        if (process == null)
        {
            logger.LogWarning("Process {ProcessId} not found for state {StateId}", state.ProcessId, processContactStateId);
            return;
        }

        var currentStep = process.Steps.FirstOrDefault(s => s.Id == state.CurrentStepId);
        if (currentStep == null)
        {
            logger.LogWarning("Step {StepId} not found in process {ProcessName}", state.CurrentStepId, process.Name);
            await FinishContactAsync(state);
            return;
        }

        logger.LogDebug(
            "Executing step {StepName} ({StepType}) for contact {ContactId} in process {ProcessName}",
            currentStep.Name, currentStep.StepType, state.ContactId, process.Name);

        try
        {
            var context = new AutomationContext
            {
                ContactId = state.ContactId,
                Process = process,
                CurrentStep = currentStep,
                State = state,
                TriggerData = state.ContextData
            };

            StepExecutionResult result;
            if (_executors.TryGetValue(currentStep.StepType, out var executor))
            {
                result = await executor.ExecuteAsync(context);
            }
            else if (currentStep.StepType == AutomationStepType.Finish)
            {
                result = StepExecutionResult.Succeeded();
            }
            else
            {
                logger.LogWarning("No executor found for step type {StepType}", currentStep.StepType);
                result = StepExecutionResult.Failed($"No executor registered for step type {currentStep.StepType}");
            }

            await stateRepository.AddStepHistoryAsync(new ProcessStepHistory
            {
                ProcessContactStateId = state.Id,
                StepId = currentStep.Id,
                StepType = currentStep.StepType,
                CompletedAt = DateTimeOffset.UtcNow,
                Success = result.Success,
                ResultDetails = result.ErrorMessage ?? result.OutputData
            });

            if (!result.Success)
            {
                logger.LogWarning(
                    "Step {StepName} failed for contact {ContactId}: {Error}",
                    currentStep.Name, state.ContactId, result.ErrorMessage);

                await stateRepository.UpdateAsync(state with
                {
                    Status = ProcessContactStatus.Failed,
                    LastError = result.ErrorMessage
                });
                return;
            }

            if (result.ShouldWait && result.WaitUntil.HasValue)
            {
                logger.LogDebug(
                    "Contact {ContactId} waiting until {WaitUntil} at step {StepName}",
                    state.ContactId, result.WaitUntil, currentStep.Name);

                await stateRepository.UpdateAsync(state with
                {
                    Status = ProcessContactStatus.Waiting,
                    WaitUntil = result.WaitUntil
                });
                return;
            }

            Guid? nextStepId = DetermineNextStep(process, currentStep, result);

            if (nextStepId == null || currentStep.StepType == AutomationStepType.Finish)
            {
                await FinishContactAsync(state);
                return;
            }

            var updatedState = state with
            {
                CurrentStepId = nextStepId.Value,
                Status = ProcessContactStatus.Active,
                StepEnteredAt = DateTimeOffset.UtcNow,
                WaitUntil = null,
                ContextData = result.OutputData ?? state.ContextData
            };

            await stateRepository.UpdateAsync(updatedState);

            // Immediately execute the next step (recursive)
            await AdvanceContactAsync(updatedState.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error executing step {StepName} for contact {ContactId}",
                currentStep.Name, state.ContactId);

            await stateRepository.UpdateAsync(state with
            {
                Status = ProcessContactStatus.Failed,
                LastError = ex.Message
            });
        }
    }

    /// <inheritdoc/>
    public async Task<int> ProcessWaitingContactsAsync()
    {
        var expiredStates = await stateRepository.GetExpiredWaitingStatesAsync();
        var expiredList = expiredStates.ToList();

        if (expiredList.Count == 0)
        {
            return 0;
        }

        logger.LogInformation("Processing {Count} expired waiting contacts", expiredList.Count);

        int advancedCount = 0;
        foreach (var state in expiredList)
        {
            try
            {
                var process = await processRepository.GetByIdAsync(state.ProcessId);
                if (process == null)
                {
                    continue;
                }

                var currentStep = process.Steps.FirstOrDefault(s => s.Id == state.CurrentStepId);
                if (currentStep == null)
                {
                    await FinishContactAsync(state);
                    continue;
                }

                var nextStepId = GetNextSequentialStep(process, currentStep);
                if (nextStepId == null)
                {
                    await FinishContactAsync(state);
                    continue;
                }

                var updatedState = state with
                {
                    CurrentStepId = nextStepId.Value,
                    Status = ProcessContactStatus.Active,
                    StepEnteredAt = DateTimeOffset.UtcNow,
                    WaitUntil = null
                };

                await stateRepository.UpdateAsync(updatedState);
                await AdvanceContactAsync(updatedState.Id);
                advancedCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error advancing waiting contact state {StateId}", state.Id);
            }
        }

        return advancedCount;
    }

    /// <inheritdoc/>
    public async Task RemoveContactFromProcessAsync(Guid processId, int contactId)
    {
        var state = await stateRepository.GetByProcessAndContactAsync(processId, contactId);
        if (state == null)
        {
            return;
        }

        await stateRepository.UpdateAsync(state with
        {
            Status = ProcessContactStatus.Removed,
            FinishedAt = DateTimeOffset.UtcNow
        });

        logger.LogInformation(
            "Removed contact {ContactId} from process {ProcessId}",
            contactId, processId);
    }

    /// <inheritdoc/>
    public async Task<ProcessContactState?> GetContactStateAsync(Guid processId, int contactId) =>
        await stateRepository.GetByProcessAndContactAsync(processId, contactId);

    /// <inheritdoc/>
    public async Task<IEnumerable<ProcessContactState>> GetActiveContactsInProcessAsync(Guid processId) =>
        await stateRepository.GetActiveByProcessAsync(processId);

    private async Task<bool> CanStartProcessForContactAsync(AutomationProcess process, int contactId)
    {
        switch (process.Recurrence)
        {
            case ProcessRecurrence.Always:
                return true;

            case ProcessRecurrence.OnlyOnce:
                var executionCount = await stateRepository.GetExecutionCountAsync(process.Id, contactId);
                return executionCount == 0;

            case ProcessRecurrence.IfNotAlreadyRunning:
                var existingState = await stateRepository.GetByProcessAndContactAsync(process.Id, contactId);
                if (existingState == null)
                {
                    return true;
                }
                return existingState.Status is ProcessContactStatus.Finished or ProcessContactStatus.Removed;

            default:
                return false;
        }
    }

    private async Task StartProcessForContactAsync(AutomationProcess process, TriggerEventData eventData)
    {
        if (process.Steps.Count == 0)
        {
            logger.LogWarning("Process {ProcessName} has no steps", process.Name);
            return;
        }

        var firstStep = process.Steps.OrderBy(s => s.Order).First();
        var executionCount = await stateRepository.GetExecutionCountAsync(process.Id, eventData.ContactId);

        var state = new ProcessContactState
        {
            ProcessId = process.Id,
            ContactId = eventData.ContactId,
            CurrentStepId = firstStep.Id,
            Status = ProcessContactStatus.Active,
            ExecutionCount = executionCount + 1,
            ContextData = eventData.Data
        };

        await stateRepository.CreateAsync(state);

        logger.LogInformation(
            "Started process {ProcessName} for contact {ContactId} at step {StepName}",
            process.Name, eventData.ContactId, firstStep.Name);

        await AdvanceContactAsync(state.Id);
    }

    private static Guid? DetermineNextStep(AutomationProcess process, AutomationStep currentStep, StepExecutionResult result)
    {
        if (currentStep.StepType == AutomationStepType.Condition && result.ConditionResult.HasValue)
        {
            var branchId = result.ConditionResult.Value
                ? currentStep.TrueBranchStepId
                : currentStep.FalseBranchStepId;

            if (branchId.HasValue)
            {
                return branchId;
            }
        }

        return GetNextSequentialStep(process, currentStep);
    }

    private static Guid? GetNextSequentialStep(AutomationProcess process, AutomationStep currentStep)
    {
        var orderedSteps = process.Steps.OrderBy(s => s.Order).ToList();
        var currentIndex = orderedSteps.FindIndex(s => s.Id == currentStep.Id);

        if (currentIndex < 0 || currentIndex >= orderedSteps.Count - 1)
        {
            return null;
        }

        return orderedSteps[currentIndex + 1].Id;
    }

    private async Task FinishContactAsync(ProcessContactState state)
    {
        await stateRepository.UpdateAsync(state with
        {
            Status = ProcessContactStatus.Finished,
            FinishedAt = DateTimeOffset.UtcNow
        });

        logger.LogInformation(
            "Contact {ContactId} finished process {ProcessId}",
            state.ContactId, state.ProcessId);
    }
}
