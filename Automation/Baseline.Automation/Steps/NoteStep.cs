using System.Xml.Serialization;

namespace Baseline.Automation.Steps;

/// <summary>
/// Step definition for a Note step (annotation on the process graph with no execution logic).
/// Maps to CMS.AutomationEngine.Internal.NoteStep.
/// </summary>
[XmlRoot("NoteStep")]
public class NoteStep : StepDefinition
{
    /// <summary>Note text content (may contain HTML).</summary>
    [XmlElement("NoteText")]
    public string NoteText { get; set; } = "";

    /// <summary>Width of the note element in pixels.</summary>
    [XmlAttribute("Width")]
    public int Width { get; set; } = 200;

    /// <summary>Background color of the note.</summary>
    [XmlAttribute("Color")]
    public string Color { get; set; } = "#fffde7";

    public NoteStep()
    {
        Type = Enums.StepTypeEnum.Note;
    }

    public override StepDefinition Clone()
    {
        var clone = (NoteStep)MemberwiseClone();
        clone.SourcePoints = SourcePoints.Select(sp => sp.Clone()).ToList();
        return clone;
    }
}
