using Baseline.Automation.Enums;

namespace Baseline.Automation.SourcePoints;

/// <summary>
/// Timeout branch transition (fires when step timeout expires).
/// Maps to CMS.AutomationEngine.Internal.TimeoutSourcePoint.
/// </summary>
public class TimeoutSourcePoint : SourcePoint
{
    public TimeoutSourcePoint()
    {
        Label = "Timeout";
        Type = SourcePointTypeEnum.Timeout;
    }

    protected override SourcePoint CloneInternal() => new TimeoutSourcePoint
    {
        Label = Label,
        Text = Text,
        Tooltip = Tooltip,
        Condition = Condition,
        StepRolesSecurity = StepRolesSecurity,
        StepUsersSecurity = StepUsersSecurity
    };
}
