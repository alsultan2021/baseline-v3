using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Baseline.Automation.Admin.ViewModels;

/// <summary>
/// View model for displaying automation contact state details in the admin UI.
/// </summary>
public class AutomationContactStateViewModel
{
    [TextInputComponent(Label = "State ID", Order = 1)]
    public string StateId { get; set; } = string.Empty;

    [TextInputComponent(Label = "Contact ID", Order = 2)]
    public string ContactId { get; set; } = string.Empty;

    [TextInputComponent(Label = "Contact Name", Order = 3)]
    public string ContactName { get; set; } = string.Empty;

    [TextInputComponent(Label = "Contact Email", Order = 4)]
    public string ContactEmail { get; set; } = string.Empty;

    [TextInputComponent(Label = "Process Name", Order = 5)]
    public string ProcessName { get; set; } = string.Empty;

    [TextInputComponent(Label = "Current Step", Order = 6)]
    public string CurrentStepName { get; set; } = string.Empty;

    [TextInputComponent(Label = "Status", Order = 7)]
    public string Status { get; set; } = string.Empty;

    [TextInputComponent(Label = "Started At", Order = 8)]
    public string StartedAt { get; set; } = string.Empty;

    [TextInputComponent(Label = "Step Entered At", Order = 9)]
    public string StepEnteredAt { get; set; } = string.Empty;

    [TextInputComponent(Label = "Wait Until", Order = 10)]
    public string? WaitUntil { get; set; }

    [TextInputComponent(Label = "Finished At", Order = 11)]
    public string? FinishedAt { get; set; }

    [NumberInputComponent(Label = "Execution Count", Order = 12)]
    public int ExecutionCount { get; set; }

    [TextAreaComponent(Label = "Last Error", Order = 13)]
    public string? LastError { get; set; }
}

/// <summary>
/// Summary view model for the automation dashboard.
/// </summary>
public class AutomationDashboardViewModel
{
    [NumberInputComponent(Label = "Total Processes", Order = 1)]
    public int TotalProcesses { get; set; }

    [NumberInputComponent(Label = "Enabled Processes", Order = 2)]
    public int EnabledProcesses { get; set; }

    [NumberInputComponent(Label = "Disabled Processes", Order = 3)]
    public int DisabledProcesses { get; set; }

    [NumberInputComponent(Label = "Active Contact States", Order = 4)]
    public int ActiveContactStates { get; set; }

    [NumberInputComponent(Label = "Waiting Contact States", Order = 5)]
    public int WaitingContactStates { get; set; }
}
