using Kentico.Xperience.Admin.Base;

namespace Baseline.Automation.Admin.UIPages;

/// <summary>
/// Client properties for the automation process statistics page.
/// Matches native AutomationProcessStatisticsClientProperties layout.
/// </summary>
public class AutomationStatisticsClientProperties : TemplateClientProperties
{
    /// <summary>Last recalculation datetime (ISO 8601).</summary>
    public string LastStatisticsRecalculationDateTime { get; set; } = "";

    /// <summary>Tooltip template, e.g. "Last recalculated: {0}".</summary>
    public string LastStatisticsRecalculationTooltipTemplate { get; set; } = "";

    /// <summary>Label template, e.g. "Statistics from: {0}".</summary>
    public string LastStatisticsRecalculationLabelTemplate { get; set; } = "";

    /// <summary>Recalculate button label.</summary>
    public string RecalculateButtonLabel { get; set; } = "";

    /// <summary>Recalculating (in-progress) button label.</summary>
    public string RecalculatingButtonLabel { get; set; } = "";
}
