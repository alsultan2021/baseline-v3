using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.FormAnnotations.Internal;

namespace Baseline.Automation.Admin.ViewModels;

/// <summary>
/// View model for automation process General settings — matches native layout.
/// </summary>
public class AutomationProcessViewModel
{
    [TextInputComponent(
        Label = "Process name",
        Order = 1)]
    [RequiredValidationRule]
    public string Name { get; set; } = string.Empty;

    [CodeNameComponent(
        Label = "Code name",
        HasAutomaticCodeNameGenerationOption = false,
        IsCollapsed = true,
        Order = 2)]
    [RequiredValidationRule]
    public string CodeName { get; set; } = string.Empty;

    [DropDownComponent(
        Label = "Process recurrence",
        DataProviderType = typeof(ProcessRecurrenceDataProvider),
        Order = 3)]
    [RequiredValidationRule]
    public string Recurrence { get; set; } = "IfNotAlreadyRunning";
}

public class ProcessRecurrenceDataProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
        Task.FromResult<IEnumerable<DropDownOptionItem>>(
        [
            new() { Value = "Always", Text = "Always" },
            new() { Value = "IfNotAlreadyRunning", Text = "If not already running" },
            new() { Value = "OnlyOnce", Text = "Only once" },
        ]);
}
