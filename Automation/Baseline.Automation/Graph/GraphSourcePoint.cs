using System.Runtime.Serialization;
using Baseline.Automation.Enums;

namespace Baseline.Automation.Graph;

/// <summary>
/// Visual representation of a source point (outgoing connection point on a node).
/// Maps to CMS.AutomationEngine.Internal.GraphSourcePoint.
/// </summary>
[DataContract]
public class GraphSourcePoint
{
    /// <summary>Unique identifier (typically from SourcePoint.Guid).</summary>
    [DataMember]
    public string ID { get; set; } = "";

    /// <summary>Type of source point.</summary>
    [DataMember]
    public SourcePointTypeEnum Type { get; set; }

    /// <summary>Display label.</summary>
    [DataMember]
    public string Label { get; set; } = "";

    /// <summary>Whether the label is a localization key.</summary>
    [DataMember]
    public bool IsLabelLocalized { get; set; }

    /// <summary>Tooltip text.</summary>
    [DataMember]
    public string Tooltip { get; set; } = "";
}
