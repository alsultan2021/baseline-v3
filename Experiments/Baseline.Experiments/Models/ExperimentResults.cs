namespace Baseline.Experiments.Models;

/// <summary>
/// Results and statistics for an experiment.
/// </summary>
public class ExperimentResults
{
    /// <summary>
    /// The experiment these results belong to.
    /// </summary>
    public Guid ExperimentId { get; set; }

    /// <summary>
    /// Experiment name.
    /// </summary>
    public string ExperimentName { get; set; } = string.Empty;

    /// <summary>
    /// Current experiment status.
    /// </summary>
    public ExperimentStatus Status { get; set; }

    /// <summary>
    /// Total number of visitors in the experiment.
    /// </summary>
    public int TotalVisitors { get; set; }

    /// <summary>
    /// Total number of conversions across all variants.
    /// </summary>
    public int TotalConversions { get; set; }

    /// <summary>
    /// Results for each variant.
    /// </summary>
    public List<VariantResults> VariantResults { get; set; } = [];

    /// <summary>
    /// Statistical analysis of the results.
    /// </summary>
    public StatisticalAnalysis Statistics { get; set; } = new();

    /// <summary>
    /// Whether the experiment has reached statistical significance.
    /// </summary>
    public bool IsStatisticallySignificant { get; set; }

    /// <summary>
    /// Recommended winning variant (if significant).
    /// </summary>
    public Guid? WinningVariantId { get; set; }

    /// <summary>
    /// Confidence in the winning variant (0-1).
    /// </summary>
    public double? WinningConfidence { get; set; }

    /// <summary>
    /// Estimated days remaining to reach significance.
    /// </summary>
    public int? EstimatedDaysRemaining { get; set; }

    /// <summary>
    /// Date results were calculated.
    /// </summary>
    public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Results for a specific variant.
/// </summary>
public class VariantResults
{
    /// <summary>
    /// Variant identifier.
    /// </summary>
    public Guid VariantId { get; set; }

    /// <summary>
    /// Variant name.
    /// </summary>
    public string VariantName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the control variant.
    /// </summary>
    public bool IsControl { get; set; }

    /// <summary>
    /// Number of visitors assigned to this variant.
    /// </summary>
    public int Visitors { get; set; }

    /// <summary>
    /// Number of conversions for primary goal.
    /// </summary>
    public int Conversions { get; set; }

    /// <summary>
    /// Conversion rate (0-1).
    /// </summary>
    public double ConversionRate { get; set; }

    /// <summary>
    /// Improvement over control (relative %).
    /// </summary>
    public double? ImprovementOverControl { get; set; }

    /// <summary>
    /// Total revenue generated (if applicable).
    /// </summary>
    public decimal? TotalRevenue { get; set; }

    /// <summary>
    /// Average revenue per visitor.
    /// </summary>
    public decimal? AverageRevenuePerVisitor { get; set; }

    /// <summary>
    /// Results for each goal.
    /// </summary>
    public List<GoalResults> GoalResults { get; set; } = [];

    /// <summary>
    /// Confidence interval lower bound.
    /// </summary>
    public double ConfidenceIntervalLower { get; set; }

    /// <summary>
    /// Confidence interval upper bound.
    /// </summary>
    public double ConfidenceIntervalUpper { get; set; }
}

/// <summary>
/// Results for a specific goal.
/// </summary>
public class GoalResults
{
    /// <summary>
    /// Goal identifier.
    /// </summary>
    public Guid GoalId { get; set; }

    /// <summary>
    /// Goal name.
    /// </summary>
    public string GoalName { get; set; } = string.Empty;

    /// <summary>
    /// Number of conversions.
    /// </summary>
    public int Conversions { get; set; }

    /// <summary>
    /// Conversion rate for this goal.
    /// </summary>
    public double ConversionRate { get; set; }

    /// <summary>
    /// Total value generated.
    /// </summary>
    public decimal? TotalValue { get; set; }
}

/// <summary>
/// Statistical analysis of experiment results.
/// </summary>
public class StatisticalAnalysis
{
    /// <summary>
    /// P-value for the primary comparison.
    /// </summary>
    public double PValue { get; set; }

    /// <summary>
    /// Z-score for the primary comparison.
    /// </summary>
    public double ZScore { get; set; }

    /// <summary>
    /// Whether the result is significant at the configured confidence level.
    /// </summary>
    public bool IsSignificant { get; set; }

    /// <summary>
    /// Confidence level used (0-1).
    /// </summary>
    public double ConfidenceLevel { get; set; }

    /// <summary>
    /// Effect size (Cohen's d).
    /// </summary>
    public double EffectSize { get; set; }

    /// <summary>
    /// Statistical power achieved.
    /// </summary>
    public double AchievedPower { get; set; }

    /// <summary>
    /// Sample size per variant required for significance.
    /// </summary>
    public int RequiredSampleSize { get; set; }

    /// <summary>
    /// Current sample size per variant.
    /// </summary>
    public int CurrentSampleSize { get; set; }

    /// <summary>
    /// Percentage of required sample achieved.
    /// </summary>
    public double SampleProgress { get; set; }

    /// <summary>
    /// Bayesian probability that variant beats control (if Bayesian analysis enabled).
    /// </summary>
    public double? BayesianProbabilityToBeatControl { get; set; }

    /// <summary>
    /// Expected loss if choosing the wrong variant (Bayesian).
    /// </summary>
    public double? ExpectedLoss { get; set; }
}

/// <summary>
/// User's assignment to an experiment variant.
/// </summary>
public class ExperimentAssignment
{
    /// <summary>
    /// Experiment identifier.
    /// </summary>
    public Guid ExperimentId { get; set; }

    /// <summary>
    /// Assigned variant identifier.
    /// </summary>
    public Guid VariantId { get; set; }

    /// <summary>
    /// User identifier.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Assignment timestamp.
    /// </summary>
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Source of assignment (cookie, session, etc.).
    /// </summary>
    public string AssignmentSource { get; set; } = "cookie";
}

/// <summary>
/// Conversion event for an experiment.
/// </summary>
public class ConversionEvent
{
    /// <summary>
    /// Unique event identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Experiment identifier.
    /// </summary>
    public Guid ExperimentId { get; set; }

    /// <summary>
    /// Variant identifier.
    /// </summary>
    public Guid VariantId { get; set; }

    /// <summary>
    /// Goal identifier.
    /// </summary>
    public Guid GoalId { get; set; }

    /// <summary>
    /// User identifier.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Conversion timestamp.
    /// </summary>
    public DateTime ConvertedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional conversion value.
    /// </summary>
    public decimal? Value { get; set; }

    /// <summary>
    /// Additional metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];
}

/// <summary>
/// Summary report for multiple experiments.
/// </summary>
public class ExperimentReport
{
    /// <summary>
    /// Report title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Report generation date.
    /// </summary>
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date range start.
    /// </summary>
    public DateTime? DateRangeStart { get; set; }

    /// <summary>
    /// Date range end.
    /// </summary>
    public DateTime? DateRangeEnd { get; set; }

    /// <summary>
    /// Experiment results included in report.
    /// </summary>
    public List<ExperimentResults> Experiments { get; set; } = [];

    /// <summary>
    /// Summary statistics across all experiments.
    /// </summary>
    public ReportSummary Summary { get; set; } = new();
}

/// <summary>
/// Summary statistics for a report.
/// </summary>
public class ReportSummary
{
    /// <summary>
    /// Total experiments in report.
    /// </summary>
    public int TotalExperiments { get; set; }

    /// <summary>
    /// Experiments currently running.
    /// </summary>
    public int RunningExperiments { get; set; }

    /// <summary>
    /// Experiments that reached significance.
    /// </summary>
    public int SignificantExperiments { get; set; }

    /// <summary>
    /// Total visitors across all experiments.
    /// </summary>
    public long TotalVisitors { get; set; }

    /// <summary>
    /// Total conversions across all experiments.
    /// </summary>
    public long TotalConversions { get; set; }

    /// <summary>
    /// Average improvement of winning variants.
    /// </summary>
    public double AverageImprovement { get; set; }

    /// <summary>
    /// Total revenue impact (if tracked).
    /// </summary>
    public decimal? TotalRevenueImpact { get; set; }
}
