namespace Baseline.Automation.Enums;

/// <summary>
/// Aggregate status of all process instances for a given contact.
/// Maps to CMS.AutomationEngine.Internal.ProcessInstanceStatusEnum.
/// </summary>
public enum ProcessInstanceStatusEnum
{
    /// <summary>No process instances exist.</summary>
    None = 0,

    /// <summary>At least one instance is running.</summary>
    Running = 1,

    /// <summary>All instances are finished.</summary>
    Finished = 2
}
