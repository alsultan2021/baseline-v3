using Baseline.Automation.Enums;

namespace Baseline.Automation.SourcePoints;

/// <summary>
/// Case branch in a multi-choice step.
/// Maps to CMS.AutomationEngine.Internal.CaseSourcePoint.
/// </summary>
public class CaseSourcePoint : SourcePoint
{
    public CaseSourcePoint()
    {
        Label = "Case";
        Type = SourcePointTypeEnum.SwitchCase;
    }

    public CaseSourcePoint(string label) : this()
    {
        Label = label;
    }

    public CaseSourcePoint(int order) : this()
    {
        Label = $"Case {order}";
    }

    protected override SourcePoint CloneInternal() => new CaseSourcePoint
    {
        Label = Label,
        Text = Text,
        Tooltip = Tooltip,
        Condition = Condition,
        StepRolesSecurity = StepRolesSecurity,
        StepUsersSecurity = StepUsersSecurity
    };
}
