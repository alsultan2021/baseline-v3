namespace Baseline.Experiments.Configuration;

/// <summary>
/// Configuration options for the Baseline Experiments module.
/// </summary>
public class BaselineExperimentsOptions
{
    /// <summary>
    /// Enable A/B testing functionality. Default: true.
    /// </summary>
    public bool EnableExperiments { get; set; } = true;

    /// <summary>
    /// Enable automatic experiment assignment for visitors. Default: true.
    /// </summary>
    public bool AutoAssignVariants { get; set; } = true;

    /// <summary>
    /// Cookie name for storing experiment assignments. Default: "baseline_experiments".
    /// </summary>
    public string ExperimentCookieName { get; set; } = "baseline_experiments";

    /// <summary>
    /// Cookie expiration in days. Default: 30.
    /// </summary>
    public int CookieExpirationDays { get; set; } = 30;

    /// <summary>
    /// Default confidence level for statistical significance (0-1). Default: 0.95 (95%).
    /// </summary>
    public double DefaultConfidenceLevel { get; set; } = 0.95;

    /// <summary>
    /// Minimum sample size required before calculating statistical significance. Default: 100.
    /// </summary>
    public int MinimumSampleSize { get; set; } = 100;

    /// <summary>
    /// Enable preview mode for testing experiments without affecting statistics. Default: false.
    /// </summary>
    public bool EnablePreviewMode { get; set; } = false;

    /// <summary>
    /// Cache duration for experiment definitions in minutes. Default: 5.
    /// </summary>
    public int ExperimentCacheDurationMinutes { get; set; } = 5;

    /// <summary>
    /// Enable debug logging for experiment assignments. Default: false.
    /// </summary>
    public bool EnableDebugLogging { get; set; } = false;

    /// <summary>
    /// URL paths excluded from experiments.
    /// </summary>
    public HashSet<string> ExcludedPaths { get; set; } =
    [
        "/api/",
        "/admin/",
        "/_content/",
        "/health"
    ];

    /// <summary>
    /// Traffic allocation configuration.
    /// </summary>
    public TrafficAllocationOptions TrafficAllocation { get; set; } = new();

    /// <summary>
    /// Statistical analysis configuration.
    /// </summary>
    public StatisticsOptions Statistics { get; set; } = new();
}

/// <summary>
/// Traffic allocation configuration for experiments.
/// </summary>
public class TrafficAllocationOptions
{
    /// <summary>
    /// Default traffic percentage for control variant (0-100). Default: 50.
    /// </summary>
    public int DefaultControlPercentage { get; set; } = 50;

    /// <summary>
    /// Use consistent hashing for traffic assignment (deterministic). Default: true.
    /// </summary>
    public bool UseConsistentHashing { get; set; } = true;

    /// <summary>
    /// Salt value for consistent hashing. Default: generated GUID.
    /// </summary>
    public string HashingSalt { get; set; } = "baseline-experiments-salt";
}

/// <summary>
/// Statistical analysis configuration for experiments.
/// </summary>
public class StatisticsOptions
{
    /// <summary>
    /// Enable automatic significance detection. Default: true.
    /// </summary>
    public bool AutoDetectSignificance { get; set; } = true;

    /// <summary>
    /// Use Bayesian analysis (vs frequentist). Default: false.
    /// </summary>
    public bool UseBayesianAnalysis { get; set; } = false;

    /// <summary>
    /// Minimum detectable effect size (relative %). Default: 5.
    /// </summary>
    public double MinimumDetectableEffect { get; set; } = 5.0;

    /// <summary>
    /// Statistical power (1 - beta). Default: 0.8 (80%).
    /// </summary>
    public double StatisticalPower { get; set; } = 0.8;
}
