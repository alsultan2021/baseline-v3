using Baseline.Automation.Enums;

namespace Baseline.Automation.Steps;

/// <summary>
/// Factory for resolving step type metadata (icons, labels, capabilities).
/// Maps to CMS.AutomationEngine.Internal.StepTypeFactory.
/// </summary>
public static class StepTypeFactory
{
    /// <summary>
    /// Gets the display name for a step type.
    /// </summary>
    public static string GetDisplayName(StepTypeEnum stepType) => stepType switch
    {
        StepTypeEnum.Start => "Start",
        StepTypeEnum.Finished => "Finished",
        StepTypeEnum.Standard => "Standard",
        StepTypeEnum.Action => "Action",
        StepTypeEnum.Condition => "Condition",
        StepTypeEnum.Multichoice => "Multi-choice",
        StepTypeEnum.MultichoiceFirstWin => "Multi-choice (First Win)",
        StepTypeEnum.Userchoice => "User Choice",
        StepTypeEnum.Wait => "Wait",
        StepTypeEnum.Note => "Note",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets the icon CSS class for a step type.
    /// </summary>
    public static string GetIconClass(StepTypeEnum stepType) => stepType switch
    {
        StepTypeEnum.Start => "xp-play",
        StepTypeEnum.Finished => "xp-check-circle",
        StepTypeEnum.Action => "xp-cog",
        StepTypeEnum.Condition => "xp-separate",
        StepTypeEnum.Multichoice or StepTypeEnum.MultichoiceFirstWin => "xp-fork",
        StepTypeEnum.Userchoice => "xp-user-decision",
        StepTypeEnum.Wait => "xp-clock",
        StepTypeEnum.Note => "xp-sticky-note",
        _ => "xp-step"
    };

    /// <summary>
    /// Whether the step type supports branching (multiple outgoing paths).
    /// </summary>
    public static bool SupportsBranching(StepTypeEnum stepType) => stepType switch
    {
        StepTypeEnum.Condition or StepTypeEnum.Multichoice or StepTypeEnum.MultichoiceFirstWin or StepTypeEnum.Userchoice => true,
        _ => false
    };

    /// <summary>
    /// Whether the step type can have a timeout configured.
    /// </summary>
    public static bool SupportsTimeout(StepTypeEnum stepType) => stepType switch
    {
        StepTypeEnum.Standard or StepTypeEnum.Action or StepTypeEnum.Wait or StepTypeEnum.Condition => true,
        _ => false
    };

    /// <summary>
    /// Whether the step type represents a terminal node.
    /// </summary>
    public static bool IsTerminal(StepTypeEnum stepType) => stepType is StepTypeEnum.Finished;

    /// <summary>
    /// Whether the step type represents a start node.
    /// </summary>
    public static bool IsStart(StepTypeEnum stepType) => stepType is StepTypeEnum.Start;

    /// <summary>
    /// Gets the allowed source point count for a given step type.
    /// Returns (min, max) tuple.
    /// </summary>
    public static (int Min, int Max) GetSourcePointLimits(StepTypeEnum stepType) => stepType switch
    {
        StepTypeEnum.Condition => (2, 2),
        StepTypeEnum.Multichoice or StepTypeEnum.MultichoiceFirstWin => (2, int.MaxValue),
        StepTypeEnum.Userchoice => (2, int.MaxValue),
        StepTypeEnum.Finished or StepTypeEnum.Note => (0, 0),
        _ => (1, 1)
    };
}
