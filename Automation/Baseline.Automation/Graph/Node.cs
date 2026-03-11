using System.Runtime.Serialization;
using Baseline.Automation.Enums;

namespace Baseline.Automation.Graph;

/// <summary>
/// Abstract base class for visual graph nodes. Serializable to JSON for admin UI rendering.
/// Maps to CMS.AutomationEngine.Internal.Node.
/// </summary>
[DataContract]
public abstract class Node
{
    /// <summary>HTML element ID for the node.</summary>
    [DataMember]
    public string ID { get; set; } = "";

    /// <summary>GUID of the underlying step.</summary>
    [DataMember]
    public string GUID { get; set; } = "";

    /// <summary>Display name.</summary>
    [DataMember]
    public string Name { get; set; } = "";

    /// <summary>Whether the name is a localization key.</summary>
    [DataMember]
    public bool IsNameLocalized { get; set; }

    /// <summary>Additional content text.</summary>
    [DataMember]
    public string Content { get; set; } = "";

    /// <summary>Position in the graph designer.</summary>
    [DataMember]
    public GraphPoint Position { get; set; } = new();

    /// <summary>Visual node type.</summary>
    [DataMember]
    public NodeTypeEnum Type { get; set; }

    /// <summary>String variant of the node type for JS consumption.</summary>
    [DataMember]
    public string TypeName { get; set; } = "";

    /// <summary>CSS class for the node container.</summary>
    [DataMember]
    public string CssClass { get; set; } = "";

    /// <summary>CSS class for the node icon.</summary>
    [DataMember]
    public string IconClass { get; set; } = "";

    /// <summary>CSS class for thumbnail display.</summary>
    [DataMember]
    public string ThumbnailClass { get; set; } = "";

    /// <summary>Outgoing connection points.</summary>
    [DataMember]
    public List<GraphSourcePoint> SourcePoints { get; set; } = [];

    /// <summary>Whether this step has a timeout configured.</summary>
    [DataMember]
    public bool HasTimeout { get; set; }

    /// <summary>Description of the timeout (e.g., "2 Days").</summary>
    [DataMember]
    public string TimeoutDescription { get; set; } = "";

    /// <summary>Whether this node can be deleted from the designer.</summary>
    [DataMember]
    public bool IsDeletable { get; set; } = true;

    /// <summary>Whether this node has a target connection point.</summary>
    [DataMember]
    public bool HasTargetPoint { get; set; } = true;

    /// <summary>Associated action name (for action nodes).</summary>
    [DataMember]
    public virtual string ActionName { get; set; } = "";

    /// <summary>Creates default source points for this node type.</summary>
    public virtual List<GraphSourcePoint> GetDefaultSourcePoints() => [];

    /// <summary>Clones the node.</summary>
    public abstract Node CloneInternal();
}
