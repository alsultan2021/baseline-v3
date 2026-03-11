using System.Xml;
using System.Xml.Serialization;
using Baseline.Automation.Enums;

namespace Baseline.Automation.Steps;

/// <summary>
/// Creates <see cref="StepDefinition"/> instances from XML configuration data.
/// Maps to CMS.AutomationEngine.Internal.StepFactory.
/// </summary>
public static class StepFactory
{
    private static readonly XmlSerializer StepSerializer = new(typeof(StepDefinition));
    private static readonly XmlSerializer NoteStepSerializer = new(typeof(NoteStep));

    /// <summary>
    /// Creates a StepDefinition from an XML string.
    /// </summary>
    public static StepDefinition? FromXml(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return null;
        }

        using var reader = new StringReader(xml);
        return StepSerializer.Deserialize(reader) as StepDefinition;
    }

    /// <summary>
    /// Creates a StepDefinition from a step type with default source points.
    /// </summary>
    public static StepDefinition Create(StepTypeEnum stepType) => stepType switch
    {
        StepTypeEnum.Note => new NoteStep(),
        StepTypeEnum.Condition => new StepDefinition
        {
            Type = StepTypeEnum.Condition,
            SourcePoints =
            [
                new SourcePoints.ConditionSourcePoint(),
                new SourcePoints.ElseSourcePoint()
            ]
        },
        StepTypeEnum.Multichoice or StepTypeEnum.MultichoiceFirstWin => new StepDefinition
        {
            Type = stepType,
            SourcePoints =
            [
                new SourcePoints.CaseSourcePoint("Case 1"),
                new SourcePoints.CaseSourcePoint("Case 2")
            ]
        },
        StepTypeEnum.Userchoice => new StepDefinition
        {
            Type = StepTypeEnum.Userchoice,
            SourcePoints =
            [
                new SourcePoints.ChoiceSourcePoint(0),
                new SourcePoints.ChoiceSourcePoint(1)
            ]
        },
        _ => new StepDefinition { Type = stepType }
    };

    /// <summary>
    /// Serializes a step definition to XML.
    /// </summary>
    public static string ToXml(StepDefinition step)
    {
        var serializer = step is NoteStep ? NoteStepSerializer : StepSerializer;
        using var writer = new StringWriter();
        using var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true });
        serializer.Serialize(xmlWriter, step);
        return writer.ToString();
    }
}
