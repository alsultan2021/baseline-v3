namespace Baseline.Automation.Enums;

/// <summary>
/// Trigger type for object-based workflow triggers.
/// Maps to CMS.AutomationEngine.Internal.WorkflowTriggerTypeEnum.
/// </summary>
public enum WorkflowTriggerTypeEnum
{
    /// <summary>Trigger on object creation.</summary>
    Creation = 0,

    /// <summary>Trigger on object modification.</summary>
    Change = 1,

    /// <summary>Trigger on a time-based schedule.</summary>
    TimeBased = 2
}
