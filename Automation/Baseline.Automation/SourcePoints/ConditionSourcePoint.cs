using Baseline.Automation.Enums;

namespace Baseline.Automation.SourcePoints;

/// <summary>
/// "If" branch in a condition step.
/// Maps to CMS.AutomationEngine.Internal.ConditionSourcePoint.
/// </summary>
public class ConditionSourcePoint : SourcePoint
{
    public ConditionSourcePoint()
    {
        Label = "If";
        Type = SourcePointTypeEnum.SwitchCase;
    }

    protected override SourcePoint CloneInternal() => new ConditionSourcePoint
    {
        Label = Label,
        Text = Text,
        Tooltip = Tooltip,
        Condition = Condition,
        StepRolesSecurity = StepRolesSecurity,
        StepUsersSecurity = StepUsersSecurity
    };
}
