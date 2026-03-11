namespace Baseline.Automation.Triggers;

/// <summary>
/// Configuration options for trigger behavior.
/// Maps to CMS.Automation.Internal.TriggerOptions.
/// </summary>
public class TriggerOptions
{
    /// <summary>Whether to evaluate triggers asynchronously in a background queue.</summary>
    public bool EvaluateAsync { get; set; } = true;

    /// <summary>Maximum number of concurrent trigger evaluations.</summary>
    public int MaxConcurrentEvaluations { get; set; } = 10;

    /// <summary>Whether to log trigger evaluation results.</summary>
    public bool LogEvaluationResults { get; set; } = true;

    /// <summary>Timeout for individual trigger evaluation in seconds.</summary>
    public int EvaluationTimeoutSeconds { get; set; } = 30;

    /// <summary>Whether to skip trigger evaluation for disabled processes.</summary>
    public bool SkipDisabledProcesses { get; set; } = true;

    /// <summary>Whether to check recurrence before starting a process.</summary>
    public bool CheckRecurrence { get; set; } = true;
}
