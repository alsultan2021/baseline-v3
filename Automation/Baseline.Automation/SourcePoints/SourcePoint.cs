using System.Runtime.Serialization;
using System.Xml.Serialization;
using Baseline.Automation.Enums;

namespace Baseline.Automation.SourcePoints;

/// <summary>
/// Defines an outgoing connection point from a step with optional condition and security.
/// Maps to CMS.AutomationEngine.Internal.SourcePoint.
/// Supports XML/DataContract serialization for step definitions.
/// </summary>
[DataContract]
[XmlInclude(typeof(ConditionSourcePoint))]
[XmlInclude(typeof(ElseSourcePoint))]
[XmlInclude(typeof(CaseSourcePoint))]
[XmlInclude(typeof(ChoiceSourcePoint))]
[XmlInclude(typeof(TimeoutSourcePoint))]
[KnownType(typeof(ConditionSourcePoint))]
[KnownType(typeof(ElseSourcePoint))]
[KnownType(typeof(CaseSourcePoint))]
[KnownType(typeof(ChoiceSourcePoint))]
[KnownType(typeof(TimeoutSourcePoint))]
public class SourcePoint
{
    /// <summary>Unique identifier for this source point.</summary>
    [DataMember]
    [XmlAttribute("Guid")]
    public Guid Guid { get; set; } = Guid.NewGuid();

    /// <summary>Display label for the connection.</summary>
    [DataMember]
    [XmlAttribute("Label")]
    public string Label { get; set; } = "Next";

    /// <summary>Action text shown on the transition.</summary>
    [DataMember]
    [XmlAttribute("Text")]
    public string Text { get; set; } = "";

    /// <summary>Tooltip for the connection point.</summary>
    [DataMember]
    [XmlAttribute("Tooltip")]
    public string Tooltip { get; set; } = "";

    /// <summary>Macro condition expression for this branch.</summary>
    [DataMember]
    [XmlAttribute("Condition")]
    public string Condition { get; set; } = "";

    /// <summary>Type of source point.</summary>
    [DataMember]
    [XmlAttribute("Type")]
    public SourcePointTypeEnum Type { get; set; } = SourcePointTypeEnum.Standard;

    /// <summary>Role-based security for this source point.</summary>
    [DataMember]
    [XmlAttribute("StepRolesSecurity")]
    public StepSecurityEnum StepRolesSecurity { get; set; } = StepSecurityEnum.Default;

    /// <summary>User-based security for this source point.</summary>
    [DataMember]
    [XmlAttribute("StepUsersSecurity")]
    public StepSecurityEnum StepUsersSecurity { get; set; } = StepSecurityEnum.Default;

    /// <summary>Whether role settings are inherited from the parent step.</summary>
    [XmlIgnore]
    public bool InheritsRolesSettings => StepRolesSecurity == StepSecurityEnum.Default;

    /// <summary>Whether user settings are inherited from the parent step.</summary>
    [XmlIgnore]
    public bool InheritsUsersSettings => StepUsersSecurity == StepSecurityEnum.Default;

    /// <summary>Creates a deep clone of this source point.</summary>
    public SourcePoint Clone()
    {
        var clone = CloneInternal();
        clone.Guid = Guid.NewGuid();
        return clone;
    }

    /// <summary>Override to create type-specific clones.</summary>
    protected virtual SourcePoint CloneInternal() => new()
    {
        Label = Label,
        Text = Text,
        Tooltip = Tooltip,
        Condition = Condition,
        Type = Type,
        StepRolesSecurity = StepRolesSecurity,
        StepUsersSecurity = StepUsersSecurity
    };
}
