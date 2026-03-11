using System.Runtime.Serialization;

namespace Baseline.Automation.Graph;

/// <summary>
/// Connection from a transition record in the automation process.
/// Maps to CMS.AutomationEngine.Internal.WorkflowConnection.
/// </summary>
[DataContract]
public class AutomationConnection : Connection
{
    /// <summary>ID of the underlying transition record.</summary>
    public int TransitionID { get; set; }

    public AutomationConnection() { }

    public AutomationConnection(string sourceNodeId, string targetNodeId)
    {
        SourceNodeID = sourceNodeId;
        TargetNodeID = targetNodeId;
    }

    public AutomationConnection(int transitionId, string sourceNodeId, string targetNodeId, string sourcePointId)
    {
        TransitionID = transitionId;
        SourceNodeID = sourceNodeId;
        TargetNodeID = targetNodeId;
        SourcePointID = sourcePointId;
    }
}
