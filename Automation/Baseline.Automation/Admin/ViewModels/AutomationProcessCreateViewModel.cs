using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Baseline.Automation.Admin.ViewModels;

/// <summary>
/// Simplified view model for the automation process create form.
/// Name + Recurrence only, then navigates to builder.
/// </summary>
public class AutomationProcessCreateViewModel
{
    [TextInputComponent(
        Label = "Process name",
        Order = 1)]
    [RequiredValidationRule(ErrorMessage = "Process name is required")]
    public string Name { get; set; } = string.Empty;

    [DropDownComponent(
        Label = "Process recurrence",
        DataProviderType = typeof(ProcessRecurrenceDataProvider),
        Order = 2)]
    [RequiredValidationRule(ErrorMessage = "Process recurrence is required")]
    public string Recurrence { get; set; } = "IfNotAlreadyRunning";
}
