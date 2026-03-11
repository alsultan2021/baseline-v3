using Baseline.Automation.Enums;

namespace Baseline.Automation.SourcePoints;

/// <summary>
/// "Else" branch in a condition step.
/// Maps to CMS.AutomationEngine.Internal.ElseSourcePoint.
/// </summary>
public class ElseSourcePoint : SourcePoint
{
    public ElseSourcePoint()
    {
        Label = "Else";
        Type = SourcePointTypeEnum.SwitchDefault;
    }

    protected override SourcePoint CloneInternal() => new ElseSourcePoint
    {
        Label = Label,
        Text = Text,
        Tooltip = Tooltip,
        Condition = Condition,
        StepRolesSecurity = StepRolesSecurity,
        StepUsersSecurity = StepUsersSecurity
    };
}
