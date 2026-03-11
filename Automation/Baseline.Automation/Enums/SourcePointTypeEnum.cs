using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Baseline.Automation.Enums;

/// <summary>
/// Type of a source point (outgoing connection from a step).
/// Maps to CMS.AutomationEngine.Internal.SourcePointType.
/// </summary>
[DataContract]
public enum SourcePointTypeEnum
{
    /// <summary>Normal sequential transition.</summary>
    [EnumMember]
    [XmlEnum("0")]
    Standard = 0,

    /// <summary>IF/Case branch transition with a condition.</summary>
    [EnumMember]
    [XmlEnum("1")]
    SwitchCase = 1,

    /// <summary>ELSE/Default branch transition.</summary>
    [EnumMember]
    [XmlEnum("2")]
    SwitchDefault = 2,

    /// <summary>Timeout branch transition.</summary>
    [EnumMember]
    [XmlEnum("3")]
    Timeout = 3
}
