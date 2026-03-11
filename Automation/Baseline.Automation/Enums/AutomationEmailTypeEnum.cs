namespace Baseline.Automation.Enums;

/// <summary>
/// Types of emails sent during automation processes.
/// Maps to CMS.AutomationEngine.Internal.WorkflowEmailTypeEnum.
/// </summary>
public enum AutomationEmailTypeEnum
{
    /// <summary>Notification email about step completion.</summary>
    Notification = 0,

    /// <summary>Action-triggered email to contacts.</summary>
    ActionEmail = 1,

    /// <summary>Timeout warning email.</summary>
    TimeoutWarning = 2,

    /// <summary>Process completion summary email.</summary>
    ProcessComplete = 3
}
