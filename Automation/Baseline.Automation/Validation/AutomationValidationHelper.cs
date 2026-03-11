using Baseline.Automation.Enums;
using Baseline.Automation.Steps;

namespace Baseline.Automation.Validation;

/// <summary>
/// Validation helper for automation process definitions and graph structures.
/// Maps to CMS.AutomationEngine.Internal.WorkflowValidationHelper / AutomationValidationHelper.
/// </summary>
public static class AutomationValidationHelper
{
    /// <summary>
    /// Validates a complete process graph definition.
    /// </summary>
    public static ValidationResult ValidateGraph(
        IReadOnlyList<Models.AutomationStepInfo> steps,
        IReadOnlyList<Models.AutomationTransitionInfo> transitions)
    {
        var result = new ValidationResult();

        // Must have at least start and finish steps
        var startSteps = steps.Where(s => s.AutomationStepType == (int)StepTypeEnum.Start).ToList();
        var finishSteps = steps.Where(s => s.AutomationStepType == (int)StepTypeEnum.Finished).ToList();

        if (startSteps.Count == 0)
        {
            result.AddError("Process must have a Start step");
        }
        else if (startSteps.Count > 1)
        {
            result.AddError("Process must have exactly one Start step");
        }

        if (finishSteps.Count == 0)
        {
            result.AddError("Process must have at least one Finished step");
        }

        // Validate connections
        foreach (var step in steps)
        {
            var stepType = (StepTypeEnum)step.AutomationStepType;
            var outgoing = transitions.Count(t => t.AutomationTransitionSourceStepID == step.AutomationStepID);
            var incoming = transitions.Count(t => t.AutomationTransitionTargetStepID == step.AutomationStepID);

            // Start step should have no incoming
            if (stepType == StepTypeEnum.Start && incoming > 0)
            {
                result.AddWarning($"Start step '{step.AutomationStepDisplayName}' has incoming connections");
            }

            // Finish step should have no outgoing
            if (stepType == StepTypeEnum.Finished && outgoing > 0)
            {
                result.AddWarning($"Finished step '{step.AutomationStepDisplayName}' has outgoing connections");
            }

            // Non-terminal steps should have at least one outgoing connection
            if (!StepTypeFactory.IsTerminal(stepType) && stepType != StepTypeEnum.Note && outgoing == 0)
            {
                result.AddWarning($"Step '{step.AutomationStepDisplayName}' has no outgoing connections");
            }

            // Validate source point counts
            if (!NodeSourcePointsLimits.IsValidSourcePointCount(stepType, outgoing) &&
                !StepTypeFactory.IsTerminal(stepType))
            {
                var (min, max) = StepTypeFactory.GetSourcePointLimits(stepType);
                result.AddWarning($"Step '{step.AutomationStepDisplayName}' has {outgoing} connections but expected {min}-{max}");
            }
        }

        // Check for orphaned steps (no incoming and not start)
        foreach (var step in steps)
        {
            var stepType = (StepTypeEnum)step.AutomationStepType;
            if (stepType == StepTypeEnum.Start || stepType == StepTypeEnum.Note)
            {
                continue;
            }

            var incoming = transitions.Count(t => t.AutomationTransitionTargetStepID == step.AutomationStepID);
            if (incoming == 0)
            {
                result.AddWarning($"Step '{step.AutomationStepDisplayName}' is unreachable (no incoming connections)");
            }
        }

        return result;
    }

    /// <summary>
    /// Validates a single step definition.
    /// </summary>
    public static ValidationResult ValidateStep(Models.AutomationStepInfo step)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(step.AutomationStepDisplayName))
        {
            result.AddError("Step must have a display name");
        }

        var stepType = (StepTypeEnum)step.AutomationStepType;
        if (stepType == StepTypeEnum.Action && step.AutomationStepActionID == 0)
        {
            result.AddError($"Action step '{step.AutomationStepDisplayName}' must have an action assigned");
        }

        return result;
    }
}

/// <summary>Result of a validation operation.</summary>
public class ValidationResult
{
    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];
    public bool IsValid => Errors.Count == 0;

    public void AddError(string message) => Errors.Add(message);
    public void AddWarning(string message) => Warnings.Add(message);
}
