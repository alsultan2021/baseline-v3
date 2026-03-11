using Baseline.Automation.Enums;

namespace Baseline.Automation.Steps;

/// <summary>
/// Defines source point count limits per step/node type.
/// Used to validate graph edits and prevent invalid configurations.
/// Maps to CMS.AutomationEngine.Internal.NodeSourcePointsLimits.
/// </summary>
public static class NodeSourcePointsLimits
{
    /// <summary>
    /// Validates whether adding a source point to the step type is allowed.
    /// </summary>
    public static bool CanAddSourcePoint(StepTypeEnum stepType, int currentCount)
    {
        var (_, max) = StepTypeFactory.GetSourcePointLimits(stepType);
        return currentCount < max;
    }

    /// <summary>
    /// Validates whether removing a source point from the step type is allowed.
    /// </summary>
    public static bool CanRemoveSourcePoint(StepTypeEnum stepType, int currentCount)
    {
        var (min, _) = StepTypeFactory.GetSourcePointLimits(stepType);
        return currentCount > min;
    }

    /// <summary>
    /// Gets the maximum number of outgoing connections per source point for a step type.
    /// </summary>
    public static int GetMaxConnectionsPerSourcePoint(StepTypeEnum stepType) => stepType switch
    {
        StepTypeEnum.Condition or StepTypeEnum.Multichoice or StepTypeEnum.MultichoiceFirstWin => 1,
        StepTypeEnum.Userchoice => 1,
        _ => 1
    };

    /// <summary>
    /// Gets the maximum number of incoming connections for a node type.
    /// </summary>
    public static int GetMaxIncomingConnections(StepTypeEnum stepType) => stepType switch
    {
        StepTypeEnum.Start => 0,
        StepTypeEnum.Note => 0,
        _ => int.MaxValue
    };

    /// <summary>
    /// Validates the source point count for a step type.
    /// </summary>
    public static bool IsValidSourcePointCount(StepTypeEnum stepType, int count)
    {
        var (min, max) = StepTypeFactory.GetSourcePointLimits(stepType);
        return count >= min && count <= max;
    }
}
