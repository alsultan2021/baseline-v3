using System.Runtime.Serialization;
using Baseline.Automation.Enums;

namespace Baseline.Automation.Graph;

/// <summary>
/// Automation-specific graph built from a process definition.
/// Contains AutomationNodes with step-type styling and AutomationConnections from transitions.
/// Maps to CMS.AutomationEngine.Internal.WorkflowGraph.
/// </summary>
[DataContract]
public class AutomationGraph : Graph
{
    /// <summary>ID of the automation process this graph represents.</summary>
    [DataMember]
    public int ProcessID { get; set; }

    /// <summary>GUID of the automation process.</summary>
    [DataMember]
    public Guid ProcessGuid { get; set; }

    /// <summary>Display name of the process.</summary>
    [DataMember]
    public string ProcessDisplayName { get; set; } = "";

    /// <summary>Whether the process is enabled.</summary>
    [DataMember]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Builds an AutomationGraph from step and transition data.
    /// </summary>
    public static AutomationGraph Build(
        Guid processGuid,
        string processName,
        IEnumerable<AutomationStepData> steps,
        IEnumerable<AutomationTransitionData> transitions)
    {
        var graph = new AutomationGraph
        {
            ProcessGuid = processGuid,
            ProcessDisplayName = processName
        };

        // Create nodes from steps
        int x = 100, y = 100;
        foreach (var step in steps)
        {
            var node = AutomationNode.GetInstance(step.StepType);
            node.ID = step.StepID.ToString();
            node.GUID = step.StepGuid.ToString();
            node.Name = step.DisplayName;
            node.Position = new GraphPoint { X = step.PositionX ?? x, Y = step.PositionY ?? y };

            if (step.SourcePoints is { Count: > 0 })
            {
                node.LoadSourcePoints(step.SourcePoints);
            }
            else
            {
                node.SourcePoints = node.GetDefaultSourcePoints();
            }

            if (step.HasTimeout)
            {
                node.HasTimeout = true;
                node.TimeoutDescription = step.TimeoutDescription ?? "";
            }

            graph.AddNode(node);
            y += 120;
        }

        // Create connections from transitions
        foreach (var transition in transitions)
        {
            graph.AddConnection(new AutomationConnection(
                transition.TransitionID,
                transition.SourceStepID.ToString(),
                transition.TargetStepID.ToString(),
                transition.SourcePointGuid.ToString()));
        }

        return graph;
    }
}

/// <summary>Step data for graph construction.</summary>
public record AutomationStepData
{
    public required int StepID { get; init; }
    public required Guid StepGuid { get; init; }
    public required string DisplayName { get; init; }
    public required StepTypeEnum StepType { get; init; }
    public int? PositionX { get; init; }
    public int? PositionY { get; init; }
    public bool HasTimeout { get; init; }
    public string? TimeoutDescription { get; init; }
    public List<SourcePoints.SourcePoint>? SourcePoints { get; init; }
}

/// <summary>Transition data for graph construction.</summary>
public record AutomationTransitionData
{
    public required int TransitionID { get; init; }
    public required int SourceStepID { get; init; }
    public required int TargetStepID { get; init; }
    public required Guid SourcePointGuid { get; init; }
}
