using Baseline.Automation.Enums;
using Baseline.Automation.Graph;

namespace Baseline.Automation.Steps;

/// <summary>
/// Generates AutomationNode instances for each step type with correct visual styling.
/// Maps to CMS.AutomationEngine.Internal.StepTypeAutomationNodes.
/// </summary>
public static class StepTypeAutomationNodes
{
    /// <summary>
    /// Creates an AutomationNode for a step definition with full visual configuration.
    /// </summary>
    public static AutomationNode CreateNode(
        int stepId,
        Guid stepGuid,
        string displayName,
        StepDefinition stepDefinition,
        string? actionName = null,
        string? actionIcon = null)
    {
        var node = AutomationNode.GetInstance(stepDefinition.Type);
        node.ID = stepId.ToString();
        node.GUID = stepGuid.ToString();
        node.Name = displayName;

        if (actionName is not null)
        {
            node.LoadAction(actionName, actionIcon ?? "");
        }

        if (stepDefinition.SourcePoints.Count > 0)
        {
            node.LoadSourcePoints(stepDefinition.SourcePoints);
        }
        else
        {
            node.SourcePoints = node.GetDefaultSourcePoints();
        }

        if (stepDefinition.TimeoutEnabled)
        {
            node.HasTimeout = true;
            node.TimeoutDescription = FormatTimeout(stepDefinition.TimeoutInterval);
        }

        return node;
    }

    /// <summary>
    /// Creates the default start node for a new automation process.
    /// </summary>
    public static AutomationNode CreateStartNode(Guid stepGuid) =>
        CreateNode(0, stepGuid, "Start", StepFactory.Create(StepTypeEnum.Start));

    /// <summary>
    /// Creates the default finished node for a new automation process.
    /// </summary>
    public static AutomationNode CreateFinishedNode(Guid stepGuid) =>
        CreateNode(0, stepGuid, "Finished", StepFactory.Create(StepTypeEnum.Finished));

    private static string FormatTimeout(int intervalMinutes) => intervalMinutes switch
    {
        < 60 => $"{intervalMinutes} min",
        < 1440 => $"{intervalMinutes / 60} hr",
        _ => $"{intervalMinutes / 1440} day(s)"
    };
}
