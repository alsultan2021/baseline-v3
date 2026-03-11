namespace Baseline.Automation.Configuration;

/// <summary>
/// Configuration options for the Baseline Automation engine.
/// Bind to "Baseline:Automation" section in appsettings.json.
/// </summary>
public class AutomationOptions
{
    public const string SectionName = "Baseline:Automation";

    /// <summary>Master switch for the automation engine (trigger dispatcher).</summary>
    public bool EnableAutomation { get; set; } = true;

    /// <summary>Whether the background service processes waiting contacts.</summary>
    public bool EnableBackgroundProcessing { get; set; } = true;

    /// <summary>Background polling interval in seconds.</summary>
    public int PollingIntervalSeconds { get; set; } = 60;

    /// <summary>Maximum retries before a step is marked as failed.</summary>
    public int MaxStepRetries { get; set; } = 3;

    /// <summary>Use in-memory repositories instead of database.</summary>
    public bool UseInMemoryStorage { get; set; }

    /// <summary>Maximum step depth to prevent infinite loops.</summary>
    public int MaxStepDepth { get; set; } = 100;

    /// <summary>Log detailed automation events to the Xperience event log.</summary>
    public bool EnableDetailedEventLogging { get; set; }
}
