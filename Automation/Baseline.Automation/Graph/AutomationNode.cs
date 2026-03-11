using System.Runtime.Serialization;
using Baseline.Automation.Enums;
using Baseline.Automation.SourcePoints;

namespace Baseline.Automation.Graph;

/// <summary>
/// Node representation of an automation step in the graph designer.
/// Maps to CMS.AutomationEngine.Internal.WorkflowNode.
/// </summary>
[DataContract]
public class AutomationNode : Node
{
    /// <summary>Underlying step type.</summary>
    public StepTypeEnum StepType { get; set; }

    /// <summary>ID of the associated action (if this is an action step).</summary>
    public int ActionID { get; set; }

    /// <summary>
    /// Creates an AutomationNode from a step type with default styling.
    /// </summary>
    public static AutomationNode GetInstance(StepTypeEnum stepType)
    {
        var node = new AutomationNode { StepType = stepType };

        switch (stepType)
        {
            case StepTypeEnum.Start:
                node.Type = NodeTypeEnum.Standard;
                node.TypeName = "start";
                node.CssClass = "node-start";
                node.IconClass = "xp-play";
                node.IsDeletable = false;
                node.HasTargetPoint = false;
                break;
            case StepTypeEnum.Finished:
                node.Type = NodeTypeEnum.Standard;
                node.TypeName = "finished";
                node.CssClass = "node-finished";
                node.IconClass = "xp-check-circle";
                node.IsDeletable = false;
                break;
            case StepTypeEnum.Action:
                node.Type = NodeTypeEnum.Action;
                node.TypeName = "action";
                node.CssClass = "node-action";
                node.IconClass = "xp-cog";
                break;
            case StepTypeEnum.Condition:
                node.Type = NodeTypeEnum.Condition;
                node.TypeName = "condition";
                node.CssClass = "node-condition";
                node.IconClass = "xp-separate";
                break;
            case StepTypeEnum.Multichoice:
            case StepTypeEnum.MultichoiceFirstWin:
                node.Type = NodeTypeEnum.Multichoice;
                node.TypeName = "multichoice";
                node.CssClass = "node-multichoice";
                node.IconClass = "xp-fork";
                break;
            case StepTypeEnum.Userchoice:
                node.Type = NodeTypeEnum.Userchoice;
                node.TypeName = "userchoice";
                node.CssClass = "node-userchoice";
                node.IconClass = "xp-user-decision";
                break;
            case StepTypeEnum.Wait:
                node.Type = NodeTypeEnum.Standard;
                node.TypeName = "wait";
                node.CssClass = "node-wait";
                node.IconClass = "xp-clock";
                node.HasTimeout = true;
                break;
            case StepTypeEnum.Note:
                node.Type = NodeTypeEnum.Note;
                node.TypeName = "note";
                node.CssClass = "node-note";
                node.IconClass = "xp-sticky-note";
                node.HasTargetPoint = false;
                break;
            default:
                node.Type = NodeTypeEnum.Standard;
                node.TypeName = "standard";
                node.CssClass = "node-standard";
                node.IconClass = "xp-step";
                break;
        }

        return node;
    }

    /// <summary>Loads action metadata onto this node.</summary>
    public void LoadAction(string actionName, string actionIconClass)
    {
        ActionName = actionName;
        if (!string.IsNullOrEmpty(actionIconClass))
        {
            IconClass = actionIconClass;
        }
    }

    /// <summary>Creates default source points based on step type.</summary>
    public override List<GraphSourcePoint> GetDefaultSourcePoints()
    {
        return StepType switch
        {
            StepTypeEnum.Condition =>
            [
                new() { ID = Guid.NewGuid().ToString(), Type = SourcePointTypeEnum.SwitchCase, Label = "If" },
                new() { ID = Guid.NewGuid().ToString(), Type = SourcePointTypeEnum.SwitchDefault, Label = "Else" }
            ],
            StepTypeEnum.Multichoice or StepTypeEnum.MultichoiceFirstWin =>
            [
                new() { ID = Guid.NewGuid().ToString(), Type = SourcePointTypeEnum.SwitchCase, Label = "Case 1" },
                new() { ID = Guid.NewGuid().ToString(), Type = SourcePointTypeEnum.SwitchCase, Label = "Case 2" }
            ],
            StepTypeEnum.Userchoice =>
            [
                new() { ID = Guid.NewGuid().ToString(), Type = SourcePointTypeEnum.SwitchCase, Label = "Choice 1" },
                new() { ID = Guid.NewGuid().ToString(), Type = SourcePointTypeEnum.SwitchCase, Label = "Choice 2" }
            ],
            StepTypeEnum.Note or StepTypeEnum.Finished => [],
            _ =>
            [
                new() { ID = Guid.NewGuid().ToString(), Type = SourcePointTypeEnum.Standard, Label = "Next" }
            ]
        };
    }

    /// <summary>Populates source points from a step's SourcePoint definitions.</summary>
    public void LoadSourcePoints(IEnumerable<SourcePoint> sourcePoints)
    {
        SourcePoints = sourcePoints.Select(sp => new GraphSourcePoint
        {
            ID = sp.Guid.ToString(),
            Type = sp.Type,
            Label = sp.Label,
            Tooltip = sp.Tooltip
        }).ToList();
    }

    public override Node CloneInternal() => new AutomationNode
    {
        ID = ID,
        GUID = GUID,
        Name = Name,
        Content = Content,
        Position = Position.Clone(),
        Type = Type,
        TypeName = TypeName,
        CssClass = CssClass,
        IconClass = IconClass,
        ThumbnailClass = ThumbnailClass,
        SourcePoints = [.. SourcePoints],
        HasTimeout = HasTimeout,
        TimeoutDescription = TimeoutDescription,
        IsDeletable = IsDeletable,
        HasTargetPoint = HasTargetPoint,
        ActionName = ActionName,
        StepType = StepType,
        ActionID = ActionID
    };
}
