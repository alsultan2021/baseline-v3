namespace Baseline.Automation.Enums;

/// <summary>
/// Security model for step permissions.
/// Maps to CMS.AutomationEngine.Internal.WorkflowStepSecurityEnum.
/// </summary>
public enum StepSecurityEnum
{
    /// <summary>Default access (inherit from step or no restriction).</summary>
    Default = 0,

    /// <summary>Only assigned users/roles can act on this step.</summary>
    OnlyAssigned = 1,

    /// <summary>Everyone except assigned users/roles can act on this step.</summary>
    AllExceptAssigned = 2
}
