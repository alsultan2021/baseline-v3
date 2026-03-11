using System.Runtime.Serialization;
using Baseline.Automation.Enums;

namespace Baseline.Automation.Graph;

/// <summary>
/// Source point representation for the automation graph designer UI.
/// Converts from a step's <see cref="SourcePoints.SourcePoint"/> definition to a visual graph element.
/// Maps to CMS.AutomationEngine.Internal.WorkflowSourcePoint.
/// </summary>
[DataContract]
public class AutomationSourcePoint : GraphSourcePoint
{
    /// <summary>Condition expression associated with this source point.</summary>
    [DataMember]
    public string Condition { get; set; } = "";

    /// <summary>Whether labels should be localized using resource strings.</summary>
    [DataMember]
    public bool IsLocalized { get; set; }

    /// <summary>Step security setting for roles.</summary>
    [DataMember]
    public StepSecurityEnum RolesSecurity { get; set; } = StepSecurityEnum.Default;

    /// <summary>Step security setting for users.</summary>
    [DataMember]
    public StepSecurityEnum UsersSecurity { get; set; } = StepSecurityEnum.Default;

    /// <summary>Creates an AutomationSourcePoint from a SourcePoint model.</summary>
    public static AutomationSourcePoint FromSourcePoint(SourcePoints.SourcePoint sp) => new()
    {
        ID = sp.Guid.ToString(),
        Type = sp.Type,
        Label = sp.Label,
        Tooltip = sp.Tooltip,
        Condition = sp.Condition,
        RolesSecurity = sp.StepRolesSecurity,
        UsersSecurity = sp.StepUsersSecurity
    };
}
