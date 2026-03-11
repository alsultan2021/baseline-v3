namespace Baseline.Automation.Enums;

/// <summary>
/// Comprehensive step types including graph-oriented types.
/// Extends the simpler AutomationStepType with visual/structural types.
/// Maps to CMS.AutomationEngine.Internal.WorkflowStepTypeEnum.
/// </summary>
public enum StepTypeEnum
{
    /// <summary>Start node of the process.</summary>
    Start = 0,

    /// <summary>Process completed terminal node.</summary>
    Finished = 1,

    /// <summary>Standard step (container for actions).</summary>
    Standard = 2,

    /// <summary>Step that executes an action.</summary>
    Action = 3,

    /// <summary>IF/ELSE condition step (2 branches).</summary>
    Condition = 4,

    /// <summary>Multi-choice branching step (evaluates all, takes all matching).</summary>
    Multichoice = 5,

    /// <summary>Multi-choice branching step (evaluates all, takes first matching).</summary>
    MultichoiceFirstWin = 6,

    /// <summary>User-choice step (waits for manual user selection).</summary>
    Userchoice = 7,

    /// <summary>Wait step with timeout.</summary>
    Wait = 100,

    /// <summary>Non-executable note/annotation step.</summary>
    Note = 101,

    /// <summary>Undefined/unknown.</summary>
    Undefined = 999
}
