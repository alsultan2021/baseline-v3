using System.Runtime.Serialization;

namespace Baseline.Automation.Graph;

/// <summary>
/// JSON-serializable connection between two nodes in the graph.
/// Maps to CMS.AutomationEngine.Internal.Connection.
/// </summary>
[DataContract]
public class Connection
{
    /// <summary>Internal connection identifier.</summary>
    public int ID { get; set; }

    /// <summary>GUID of the source point this connection originates from.</summary>
    [DataMember]
    public string SourcePointID { get; set; } = "";

    /// <summary>ID of the node this connection originates from.</summary>
    [DataMember]
    public string SourceNodeID { get; set; } = "";

    /// <summary>ID of the node this connection points to.</summary>
    [DataMember]
    public string TargetNodeID { get; set; } = "";
}
