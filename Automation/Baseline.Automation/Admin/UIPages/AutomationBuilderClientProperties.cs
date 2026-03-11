using Kentico.Xperience.Admin.Base;

namespace Baseline.Automation.Admin.UIPages;

/// <summary>
/// Client properties for the automation process builder.
/// Process data (nodes/connections) loaded on-demand via LoadAutomationProcess command.
/// </summary>
public class AutomationBuilderClientProperties : TemplateClientProperties
{
    /// <summary>Whether the automation process is enabled.</summary>
    public bool IsAutomationProcessEnabled { get; set; }

    /// <summary>Whether the user is allowed to edit the automation process.</summary>
    public bool IsEditingAllowed { get; set; }

    /// <summary>Whether the automation process has history data (contacts triggered).</summary>
    public bool HasHistoryData { get; set; }

    /// <summary>Save button properties — uses Kentico's framework <see cref="SubmitButton"/>.</summary>
    public SubmitButton SaveButton { get; init; } = new();
}

/* ------------------------------------------------------------------ */
/*  Command argument / result DTOs                                     */
/* ------------------------------------------------------------------ */

/// <summary>Result of LoadAutomationProcess command.</summary>
public class LoadAutomationProcessResult
{
    public List<AutomationProcessNodeDto> Nodes { get; set; } = [];
    public List<AutomationProcessConnectionDto> Connections { get; set; } = [];
}

/// <summary>Node in the automation process graph.</summary>
public class AutomationProcessNodeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string StepType { get; set; } = "";
    public string IconName { get; set; } = "xp-arrows-movable";
    public string? IconTooltip { get; set; }
    public bool IsSaved { get; set; }
    public List<AutomationProcessNodeStatisticDto> Statistics { get; set; } = [];
    public IDictionary<string, object>? Configuration { get; set; }
}

/// <summary>Statistic for a single node.</summary>
public class AutomationProcessNodeStatisticDto
{
    public string IconName { get; set; } = "";
    public int Value { get; set; }
    public string? StatisticTooltip { get; set; }
}

/// <summary>Connection (edge) between two nodes.</summary>
public class AutomationProcessConnectionDto
{
    public string Id { get; set; } = "";
    public Guid SourceNodeGuid { get; set; }
    public Guid TargetNodeGuid { get; set; }
    public string? SourceHandle { get; set; }
}

/// <summary>Arguments for the Save command.</summary>
public class AutomationBuilderSaveCommandArguments
{
    public List<AutomationProcessSaveNodeDataDto> Nodes { get; set; } = [];
    public List<AutomationProcessConnectionDto> Connections { get; set; } = [];
}

/// <summary>Node data submitted during save.</summary>
public class AutomationProcessSaveNodeDataDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string StepType { get; set; } = "";
    public Dictionary<string, object>? Configuration { get; set; }
}

/// <summary>Result of Save command.</summary>
public class AutomationBuilderSaveResult
{
    public string Status { get; set; } = "validationSuccess";
}

/// <summary>Result of Enable/Disable commands.</summary>
public class SetIsAutomationProcessEnabledResult
{
    public bool IsEnabled { get; set; }
}

/// <summary>Step type tile selector item for "Add step" panel.</summary>
public class StepTypeTileItem
{
    public string StepType { get; set; } = "";
    public string Label { get; set; } = "";
    public string IconName { get; set; } = "xp-arrows-movable";
    public string? Description { get; set; }
}

/// <summary>Result of GetStepTypeTileItems command.</summary>
public class GetStepTypeTileItemsResult
{
    public List<StepTypeTileItem> Items { get; set; } = [];
}

/// <summary>Trigger type tile selector item for "Add trigger" panel.</summary>
public class TriggerTypeTileItem
{
    public string TriggerType { get; set; } = "";
    public string Label { get; set; } = "";
    public string IconName { get; set; } = "xp-lightning";
    public string? Description { get; set; }
}

/// <summary>Result of GetTriggerTypeTileItems command.</summary>
public class GetTriggerTypeTileItemsResult
{
    public List<TriggerTypeTileItem> Items { get; set; } = [];
}

/// <summary>Arguments for GetStepFormDefinition command.</summary>
public class GetStepFormDefinitionArgs
{
    public string StepType { get; set; } = "";
    public string? TriggerType { get; set; }
}

/// <summary>A single form field definition for step configuration.</summary>
public class StepFormFieldDefinition
{
    public string Name { get; set; } = "";
    public string Label { get; set; } = "";
    public string FieldType { get; set; } = "text"; // text, number, select, checkbox, datetime, codename, radio, conditionBuilder, objectSelector, combobox
    public bool Required { get; set; }
    public string? DefaultValue { get; set; }
    public string? Placeholder { get; set; }
    public List<StepFormFieldOption>? Options { get; set; }
    public string? EditUrl { get; set; }
    public string? HelpText { get; set; }
    public StepFormFieldVisibility? VisibleWhen { get; set; }
}

/// <summary>Conditional visibility for a field based on another field's value.</summary>
public class StepFormFieldVisibility
{
    public string FieldName { get; set; } = "";
    public string Value { get; set; } = "";
}

/// <summary>Option for a select field.</summary>
public class StepFormFieldOption
{
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
    public string? EditUrl { get; set; }
}

/// <summary>Result of GetStepFormDefinition command.</summary>
public class GetStepFormDefinitionResult
{
    public string StepType { get; set; } = "";
    public string Headline { get; set; } = "";
    public List<StepFormFieldDefinition> Fields { get; set; } = [];
}

/* ------------------------------------------------------------------ */
/*  Condition rules DTOs                                               */
/* ------------------------------------------------------------------ */

/// <summary>A macro rule for the condition picker.</summary>
public class ConditionRuleItem
{
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
    public string CategoryId { get; set; } = "";
    public string RuleText { get; set; } = "";
    public List<ConditionRuleParameter> Parameters { get; set; } = [];
}

/// <summary>A parameter definition for a macro rule condition.</summary>
public class ConditionRuleParameter
{
    public string Name { get; set; } = "";
    public string Caption { get; set; } = "";
    public string ControlType { get; set; } = "text"; // dropdown, text, number
    public string? Placeholder { get; set; }
    public string? DefaultValue { get; set; }
    public List<StepFormFieldOption>? Options { get; set; }
}

/// <summary>Category for the condition picker.</summary>
public class ConditionRuleCategory
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
}

/// <summary>Result of GetConditionRules command.</summary>
public class GetConditionRulesResult
{
    public List<ConditionRuleItem> Items { get; set; } = [];
    public List<ConditionRuleCategory> Categories { get; set; } = [];
}

/* ------------------------------------------------------------------ */
/*  Email selector DTOs                                                */
/* ------------------------------------------------------------------ */

/// <summary>An email item for the object selector.</summary>
public class EmailSelectorItem
{
    public string Guid { get; set; } = "";
    public string Name { get; set; } = "";
    public string Purpose { get; set; } = "";
    public string ChannelName { get; set; } = "";
    public string Status { get; set; } = "Draft";
}

/// <summary>Email channel for the selector filter.</summary>
public class EmailChannelOption
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

/// <summary>Result of GetEmailsForSelector command.</summary>
public class GetEmailsForSelectorResult
{
    public List<EmailSelectorItem> Emails { get; set; } = [];
    public List<EmailChannelOption> Channels { get; set; } = [];
}
