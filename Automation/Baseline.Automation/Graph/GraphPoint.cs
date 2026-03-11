using System.Runtime.Serialization;

namespace Baseline.Automation.Graph;

/// <summary>
/// 2D coordinates for node positioning in the graph designer.
/// Maps to CMS.AutomationEngine.Internal.GraphPoint.
/// </summary>
[DataContract]
public class GraphPoint
{
    [DataMember]
    public int X { get; set; }

    [DataMember]
    public int Y { get; set; }

    public GraphPoint() { }

    public GraphPoint(int x, int y)
    {
        X = x;
        Y = y;
    }

    public GraphPoint Clone() => new(X, Y);
}
