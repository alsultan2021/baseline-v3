namespace Baseline.Automation.Enums;

/// <summary>
/// Distinguishes sub-types of automation processes.
/// Maps to CMS.AutomationEngine.Internal.AutomationType.
/// </summary>
public enum AutomationType
{
    /// <summary>Regular automation process.</summary>
    General = 0,

    /// <summary>Auto-generated simple form autoresponder.</summary>
    SimpleFormAutoresponder = 1
}
