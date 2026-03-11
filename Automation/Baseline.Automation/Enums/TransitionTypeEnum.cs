namespace Baseline.Automation.Enums;

/// <summary>
/// Type of step transition.
/// Maps to CMS.AutomationEngine.Internal.WorkflowTransitionTypeEnum.
/// </summary>
public enum TransitionTypeEnum
{
    /// <summary>Requires human decision to advance.</summary>
    Manual = 0,

    /// <summary>Automatically transitions to next step.</summary>
    Automatic = 1
}
