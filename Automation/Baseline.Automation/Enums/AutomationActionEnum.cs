namespace Baseline.Automation.Enums;

/// <summary>
/// Actions that can be taken during automation step transitions.
/// Maps to CMS.Automation.Internal.AutomationActionEnum.
/// </summary>
public enum AutomationActionEnum
{
    /// <summary>Unknown or unrecognized action.</summary>
    Unknown = 0,

    /// <summary>Advanced to the next sequential step.</summary>
    MoveToNextStep = 1,

    /// <summary>Moved to a specific (non-sequential) step.</summary>
    MoveToSpecificStep = 2
}
