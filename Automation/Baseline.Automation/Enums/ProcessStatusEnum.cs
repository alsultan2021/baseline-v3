namespace Baseline.Automation.Enums;

/// <summary>
/// Status of an automation process execution (state machine state).
/// Maps to CMS.AutomationEngine.Internal.ProcessStatusEnum.
/// </summary>
public enum ProcessStatusEnum
{
    /// <summary>Process not yet started.</summary>
    Pending = 0,

    /// <summary>Process currently executing.</summary>
    Processing = 1,

    /// <summary>Process completed.</summary>
    Finished = 2
}
