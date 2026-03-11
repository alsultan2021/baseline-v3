namespace Baseline.Automation.Enums;

/// <summary>
/// Visual node types for the automation graph designer.
/// Maps to CMS.AutomationEngine.Internal.NodeTypeEnum.
/// </summary>
public enum NodeTypeEnum
{
    /// <summary>Standard visual container node.</summary>
    Standard = 0,

    /// <summary>Node that executes an action.</summary>
    Action = 1,

    /// <summary>IF/ELSE branch node.</summary>
    Condition = 2,

    /// <summary>SWITCH/CASE multi-branch node.</summary>
    Multichoice = 3,

    /// <summary>Wait for user to choose a branch.</summary>
    Userchoice = 4,

    /// <summary>Text annotation (non-executable).</summary>
    Note = 5
}
