using Baseline.Automation.Enums;

namespace Baseline.Automation.SourcePoints;

/// <summary>
/// User choice branch (waits for user decision).
/// Maps to CMS.AutomationEngine.Internal.ChoiceSourcePoint.
/// </summary>
public class ChoiceSourcePoint : SourcePoint
{
    public ChoiceSourcePoint()
    {
        Label = "New choice";
        Type = SourcePointTypeEnum.SwitchCase;
    }

    public ChoiceSourcePoint(int order) : this()
    {
        Label = $"Choice {order}";
    }

    protected override SourcePoint CloneInternal() => new ChoiceSourcePoint
    {
        Label = Label,
        Text = Text,
        Tooltip = Tooltip,
        Condition = Condition,
        StepRolesSecurity = StepRolesSecurity,
        StepUsersSecurity = StepUsersSecurity
    };
}
