using Baseline.Automation.Events;
using Baseline.Automation.Exceptions;
using Microsoft.Extensions.Logging;

namespace Baseline.Automation.Engine;

/// <summary>
/// Concrete automation workflow manager.
/// Handles step execution, condition evaluation, and process flow control.
/// Maps to CMS.Automation.Internal.AutomationManager.
/// </summary>
public class AutomationManager(
    IEnumerable<IAutomationActionExecutor> actionExecutors,
    IAutomationConditionEvaluator conditionEvaluator,
    IProcessStateRepository stateRepository,
    IProcessRepository processRepository,
    ILogger<AutomationManager> logger) : AbstractAutomationManager
{
    private readonly Dictionary<AutomationStepType, IAutomationActionExecutor> _executors =
        actionExecutors.ToDictionary(e => e.StepType);

    /// <summary>
    /// Processes the current step for a contact and advances them through the process.
    /// </summary>
    public async Task ProcessStepAsync(AutomationProcessItem processItem)
    {
        var process = await processRepository.GetByIdAsync(processItem.ProcessId)
            ?? throw new ProcessDisabledException(processItem.ProcessId);

        if (!process.IsEnabled)
        {
            throw new ProcessDisabledException(processItem.ProcessId);
        }

        var currentStep = process.Steps.FirstOrDefault(s => s.Id == processItem.CurrentStepId);
        if (currentStep is null)
        {
            logger.LogWarning("Step {StepId} not found in process {ProcessId}", processItem.CurrentStepId, processItem.ProcessId);
            return;
        }

        // Check for finish step
        if (currentStep.StepType == AutomationStepType.Finish)
        {
            await FinishProcessAsync(processItem, logger);
            return;
        }

        // Execute step action
        var result = await ExecuteStepAsync(processItem, currentStep);

        if (!result.Success)
        {
            Events.OnProcessError(new AutomationEventArgs
            {
                ProcessId = processItem.ProcessId,
                ContactId = processItem.ContactId,
                CurrentStepId = processItem.CurrentStepId
            });
            return;
        }

        // Determine next step
        var nextStepId = DetermineNextStep(currentStep, result);
        if (nextStepId is null)
        {
            logger.LogWarning("No next step found for step {StepId} in process {ProcessId}",
                currentStep.Id, processItem.ProcessId);
            return;
        }

        await MoveToStepAsync(processItem, nextStepId.Value, logger);
    }

    /// <summary>
    /// Evaluates a condition step and returns the appropriate branch result.
    /// </summary>
    public async Task<bool> EvaluateConditionAsync(AutomationProcessItem processItem, AutomationStep conditionStep)
    {
        var config = conditionStep.GetConfiguration<ConditionStepConfig>();
        if (config is null)
        {
            return true;
        }

        var result = await conditionEvaluator.EvaluateAsync(processItem.ContactId, config);

        Events.OnConditionEvaluated(new AutomationEventArgs
        {
            ProcessId = processItem.ProcessId,
            ContactId = processItem.ContactId,
            CurrentStepId = conditionStep.Id
        });

        return result;
    }

    private async Task<StepExecutionResult> ExecuteStepAsync(AutomationProcessItem processItem, AutomationStep step)
    {
        if (!_executors.TryGetValue(step.StepType, out var executor))
        {
            logger.LogWarning("No executor registered for step type {StepType}", step.StepType);
            return new StepExecutionResult { Success = false, ErrorMessage = $"No executor for {step.StepType}" };
        }

        var process = await processRepository.GetByIdAsync(processItem.ProcessId);
        var state = await stateRepository.GetByProcessAndContactAsync(processItem.ProcessId, processItem.ContactId);

        var context = new AutomationContext
        {
            ContactId = processItem.ContactId,
            Process = process!,
            CurrentStep = step,
            State = state!,
            TriggerData = processItem.TriggerData
        };

        var actionArgs = new AutomationActionEventArgs
        {
            ProcessId = processItem.ProcessId,
            ContactId = processItem.ContactId,
            CurrentStepId = step.Id,
            ActionName = step.StepType.ToString()
        };

        Events.OnActionExecuting(new AutomationEventArgs
        {
            ProcessId = processItem.ProcessId,
            ContactId = processItem.ContactId,
            CurrentStepId = step.Id,
            StepType = step.StepType
        });

        var result = await executor.ExecuteAsync(context);

        Events.OnActionExecuted(new AutomationEventArgs
        {
            ProcessId = processItem.ProcessId,
            ContactId = processItem.ContactId,
            CurrentStepId = step.Id,
            StepType = step.StepType
        });

        return result;
    }

    private static Guid? DetermineNextStep(AutomationStep currentStep, StepExecutionResult result)
    {
        if (currentStep.StepType == AutomationStepType.Condition)
        {
            return result.ConditionResult == true ? currentStep.TrueBranchStepId : currentStep.FalseBranchStepId;
        }

        // For non-condition steps, next step is not tracked here — the engine
        // advances by step order (handled by MoveToStepAsync caller).
        return null;
    }

    protected override async Task OnProcessFinishedAsync(AutomationProcessItem processItem, ILogger logger)
    {
        var state = await stateRepository.GetByProcessAndContactAsync(processItem.ProcessId, processItem.ContactId);
        if (state is not null)
        {
            await stateRepository.UpdateAsync(state with
            {
                Status = ProcessContactStatus.Completed,
                FinishedAt = DateTimeOffset.UtcNow
            });
        }
    }
}
