using System.Runtime.Serialization;

namespace Baseline.Automation.Graph;

/// <summary>
/// Abstract base class for visual graph definitions used by the admin graph designer.
/// Contains collections of nodes and connections that form the directed graph.
/// Maps to CMS.AutomationEngine.Internal.Graph.
/// </summary>
[DataContract]
[KnownType(typeof(AutomationGraph))]
public abstract class Graph
{
    /// <summary>Nodes in the graph.</summary>
    [DataMember]
    public List<Node> Nodes { get; set; } = [];

    /// <summary>Connections between nodes.</summary>
    [DataMember]
    public List<Connection> Connections { get; set; } = [];

    /// <summary>JavaScript files required for rendering the graph.</summary>
    [DataMember]
    public List<string> JsFiles { get; set; } = [];

    /// <summary>Localized resource strings for the graph UI.</summary>
    [DataMember]
    public Dictionary<string, string> ResourceStrings { get; set; } = new();

    /// <summary>Width of the graph canvas.</summary>
    [DataMember]
    public int CanvasWidth { get; set; } = 5000;

    /// <summary>Height of the graph canvas.</summary>
    [DataMember]
    public int CanvasHeight { get; set; } = 5000;

    /// <summary>Adds a node to the graph.</summary>
    public void AddNode(Node node) => Nodes.Add(node);

    /// <summary>Adds a connection between two nodes.</summary>
    public void AddConnection(Connection connection) => Connections.Add(connection);

    /// <summary>Finds a node by its ID.</summary>
    public Node? FindNode(string nodeId) =>
        Nodes.Find(n => n.ID == nodeId);

    /// <summary>Finds a node by its GUID.</summary>
    public Node? FindNodeByGuid(string guid) =>
        Nodes.Find(n => n.GUID == guid);

    /// <summary>Gets all connections originating from a node.</summary>
    public IEnumerable<Connection> GetNodeConnections(string nodeId) =>
        Connections.Where(c => c.SourceNodeID == nodeId);

    /// <summary>Gets the connection count for a source point on a node.</summary>
    public int GetSourcePointConnectionCount(string nodeId, string sourcePointId) =>
        Connections.Count(c => c.SourceNodeID == nodeId && c.SourcePointID == sourcePointId);
}
