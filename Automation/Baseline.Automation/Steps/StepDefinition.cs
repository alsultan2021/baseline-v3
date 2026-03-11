using System.Xml.Serialization;
using Baseline.Automation.Enums;
using Baseline.Automation.SourcePoints;

namespace Baseline.Automation.Steps;

/// <summary>
/// Defines an automation step's configuration, including source points for branching.
/// Serializable to XML for persistence in the process definition.
/// Maps to CMS.AutomationEngine.Internal.Step.
/// </summary>
[XmlRoot("Step")]
[XmlInclude(typeof(NoteStep))]
public class StepDefinition
{
    /// <summary>Step type.</summary>
    [XmlAttribute("Type")]
    public StepTypeEnum Type { get; set; }

    /// <summary>Whether a timeout is enabled for this step.</summary>
    [XmlAttribute("TimeoutEnabled")]
    public bool TimeoutEnabled { get; set; }

    /// <summary>Timeout interval in minutes.</summary>
    [XmlAttribute("TimeoutInterval")]
    public int TimeoutInterval { get; set; }

    /// <summary>Timeout target step GUID to move to on timeout.</summary>
    [XmlAttribute("TimeoutTarget")]
    public string TimeoutTarget { get; set; } = "";

    /// <summary>Source points defining outgoing connections and branches.</summary>
    [XmlArray("SourcePoints")]
    [XmlArrayItem("SourcePoint", typeof(SourcePoint))]
    [XmlArrayItem("Condition", typeof(ConditionSourcePoint))]
    [XmlArrayItem("Else", typeof(ElseSourcePoint))]
    [XmlArrayItem("Case", typeof(CaseSourcePoint))]
    [XmlArrayItem("Choice", typeof(ChoiceSourcePoint))]
    [XmlArrayItem("Timeout", typeof(TimeoutSourcePoint))]
    public List<SourcePoint> SourcePoints { get; set; } = [];

    /// <summary>Associated action assembly name.</summary>
    [XmlAttribute("ActionAssemblyName")]
    public string ActionAssemblyName { get; set; } = "";

    /// <summary>Associated action class name.</summary>
    [XmlAttribute("ActionClassName")]
    public string ActionClassName { get; set; } = "";

    /// <summary>Action-specific parameters as XML.</summary>
    [XmlElement("ActionParameters")]
    public string? ActionParameters { get; set; }

    /// <summary>Get the timeout source point if one exists.</summary>
    public TimeoutSourcePoint? GetTimeoutSourcePoint() =>
        SourcePoints.OfType<TimeoutSourcePoint>().FirstOrDefault();

    /// <summary>Get all condition source points.</summary>
    public IEnumerable<ConditionSourcePoint> GetConditionSourcePoints() =>
        SourcePoints.OfType<ConditionSourcePoint>();

    /// <summary>Get the else (default) source point if one exists.</summary>
    public ElseSourcePoint? GetElseSourcePoint() =>
        SourcePoints.OfType<ElseSourcePoint>().FirstOrDefault();

    /// <summary>Get the standard (next step) source point.</summary>
    public SourcePoint? GetStandardSourcePoint() =>
        SourcePoints.Find(sp => sp.Type == SourcePointTypeEnum.Standard);

    /// <summary>Creates a deep clone.</summary>
    public virtual StepDefinition Clone()
    {
        var clone = (StepDefinition)MemberwiseClone();
        clone.SourcePoints = SourcePoints.Select(sp => sp.Clone()).ToList();
        return clone;
    }
}
