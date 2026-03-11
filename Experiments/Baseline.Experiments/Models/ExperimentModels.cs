using System.Text.Json.Serialization;

namespace Baseline.Experiments.Models;

/// <summary>
/// Represents an A/B testing experiment.
/// </summary>
public class Experiment
{
    /// <summary>
    /// Unique identifier for the experiment.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Human-readable experiment name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the experiment's purpose and hypothesis.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Type of experiment (Page, Widget, Email, Custom).
    /// </summary>
    public ExperimentType Type { get; set; } = ExperimentType.Page;

    /// <summary>
    /// Current status of the experiment.
    /// </summary>
    public ExperimentStatus Status { get; set; } = ExperimentStatus.Draft;

    /// <summary>
    /// Collection of variants for this experiment.
    /// </summary>
    public List<ExperimentVariant> Variants { get; set; } = [];

    /// <summary>
    /// Collection of goals to track for this experiment.
    /// </summary>
    public List<ExperimentGoal> Goals { get; set; } = [];

    /// <summary>
    /// Traffic allocation settings.
    /// </summary>
    public TrafficAllocation TrafficAllocation { get; set; } = new();

    /// <summary>
    /// Experiment start date (UTC).
    /// </summary>
    public DateTime? StartDateUtc { get; set; }

    /// <summary>
    /// Experiment end date (UTC).
    /// </summary>
    public DateTime? EndDateUtc { get; set; }

    /// <summary>
    /// Minimum sample size before declaring results.
    /// </summary>
    public int MinimumSampleSize { get; set; } = 100;

    /// <summary>
    /// Required confidence level (0-1).
    /// </summary>
    public double ConfidenceLevel { get; set; } = 0.95;

    /// <summary>
    /// Target page URL or content path (for page experiments).
    /// </summary>
    public string? TargetPath { get; set; }

    /// <summary>
    /// Widget identifier (for widget experiments).
    /// </summary>
    public string? WidgetIdentifier { get; set; }

    /// <summary>
    /// Date the experiment was created (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date the experiment was last modified (UTC).
    /// </summary>
    public DateTime ModifiedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created the experiment.
    /// </summary>
    public string? CreatedBy { get; set; }
}

/// <summary>
/// Represents a variant within an experiment.
/// </summary>
public class ExperimentVariant
{
    /// <summary>
    /// Unique identifier for the variant.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Human-readable variant name (e.g., "Control", "Variant A").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this variant changes.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the control variant.
    /// </summary>
    public bool IsControl { get; set; }

    /// <summary>
    /// Traffic weight (percentage 0-100).
    /// </summary>
    public int Weight { get; set; } = 50;

    /// <summary>
    /// Variant-specific data (JSON configuration, page ID, etc.).
    /// </summary>
    public string? Configuration { get; set; }

    /// <summary>
    /// Alternative content path (for page variants).
    /// </summary>
    public string? ContentPath { get; set; }

    /// <summary>
    /// Widget configuration override (for widget variants).
    /// </summary>
    public string? WidgetConfiguration { get; set; }
}

/// <summary>
/// Represents a conversion goal for an experiment.
/// </summary>
public class ExperimentGoal
{
    /// <summary>
    /// Unique identifier for the goal.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Human-readable goal name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Goal code name for tracking.
    /// </summary>
    public string CodeName { get; set; } = string.Empty;

    /// <summary>
    /// Type of goal.
    /// </summary>
    public GoalType Type { get; set; } = GoalType.PageView;

    /// <summary>
    /// Target URL or event for the goal.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// Whether this is the primary goal.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Optional monetary value for revenue goals.
    /// </summary>
    public decimal? Value { get; set; }
}

/// <summary>
/// Traffic allocation settings for an experiment.
/// </summary>
public class TrafficAllocation
{
    /// <summary>
    /// Percentage of total traffic included in experiment (0-100).
    /// </summary>
    public int IncludedTrafficPercentage { get; set; } = 100;

    /// <summary>
    /// Target specific user segments.
    /// </summary>
    public List<string> TargetSegments { get; set; } = [];

    /// <summary>
    /// Exclude specific user segments.
    /// </summary>
    public List<string> ExcludedSegments { get; set; } = [];

    /// <summary>
    /// Target specific countries (ISO codes).
    /// </summary>
    public List<string> TargetCountries { get; set; } = [];

    /// <summary>
    /// Target specific device types.
    /// </summary>
    public List<DeviceType> TargetDevices { get; set; } = [];
}

/// <summary>
/// Types of experiments.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExperimentType
{
    /// <summary>
    /// A/B test entire pages.
    /// </summary>
    Page,

    /// <summary>
    /// A/B test individual widgets.
    /// </summary>
    Widget,

    /// <summary>
    /// A/B test email content.
    /// </summary>
    Email,

    /// <summary>
    /// Custom experiment type.
    /// </summary>
    Custom
}

/// <summary>
/// Experiment lifecycle status.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExperimentStatus
{
    /// <summary>
    /// Experiment is being configured.
    /// </summary>
    Draft,

    /// <summary>
    /// Experiment is scheduled to start.
    /// </summary>
    Scheduled,

    /// <summary>
    /// Experiment is actively running.
    /// </summary>
    Running,

    /// <summary>
    /// Experiment is paused.
    /// </summary>
    Paused,

    /// <summary>
    /// Experiment has completed.
    /// </summary>
    Completed,

    /// <summary>
    /// Experiment was archived.
    /// </summary>
    Archived
}

/// <summary>
/// Types of conversion goals.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GoalType
{
    /// <summary>
    /// Page view goal.
    /// </summary>
    PageView,

    /// <summary>
    /// Form submission goal.
    /// </summary>
    FormSubmission,

    /// <summary>
    /// Click event goal.
    /// </summary>
    Click,

    /// <summary>
    /// Purchase/transaction goal.
    /// </summary>
    Purchase,

    /// <summary>
    /// Custom event goal.
    /// </summary>
    CustomEvent,

    /// <summary>
    /// Time on page goal.
    /// </summary>
    TimeOnPage,

    /// <summary>
    /// Scroll depth goal.
    /// </summary>
    ScrollDepth
}

/// <summary>
/// Device types for targeting.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DeviceType
{
    Desktop,
    Mobile,
    Tablet
}

/// <summary>
/// Definition for creating a new experiment.
/// </summary>
public class ExperimentDefinition
{
    /// <summary>
    /// Human-readable experiment name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the experiment's purpose.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Type of experiment.
    /// </summary>
    public ExperimentType Type { get; set; } = ExperimentType.Page;

    /// <summary>
    /// Variant definitions.
    /// </summary>
    public List<VariantDefinition> Variants { get; set; } = [];

    /// <summary>
    /// Goal definitions.
    /// </summary>
    public List<GoalDefinition> Goals { get; set; } = [];

    /// <summary>
    /// Traffic allocation settings.
    /// </summary>
    public TrafficAllocation TrafficAllocation { get; set; } = new();

    /// <summary>
    /// Scheduled start date.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Scheduled end date.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Minimum sample size.
    /// </summary>
    public int? MinimumSampleSize { get; set; }

    /// <summary>
    /// Confidence level (0-1).
    /// </summary>
    public double ConfidenceLevel { get; set; } = 0.95;

    /// <summary>
    /// Target page path.
    /// </summary>
    public string? TargetPath { get; set; }
}

/// <summary>
/// Definition for creating a variant.
/// </summary>
public class VariantDefinition
{
    /// <summary>
    /// Variant name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Variant description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the control.
    /// </summary>
    public bool IsControl { get; set; }

    /// <summary>
    /// Traffic weight (percentage).
    /// </summary>
    public int Weight { get; set; } = 50;

    /// <summary>
    /// Configuration data.
    /// </summary>
    public string? Configuration { get; set; }
}

/// <summary>
/// Definition for creating a goal.
/// </summary>
public class GoalDefinition
{
    /// <summary>
    /// Goal name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Goal code name.
    /// </summary>
    public string CodeName { get; set; } = string.Empty;

    /// <summary>
    /// Goal type.
    /// </summary>
    public GoalType Type { get; set; } = GoalType.PageView;

    /// <summary>
    /// Target URL or event.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// Whether this is the primary goal.
    /// </summary>
    public bool IsPrimary { get; set; }
}
