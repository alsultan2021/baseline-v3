using Baseline.Experiments.Models;

namespace Baseline.Experiments.Interfaces;

/// <summary>
/// Core service for managing A/B testing experiments.
/// </summary>
public interface IExperimentService
{
    /// <summary>
    /// Creates a new experiment.
    /// </summary>
    /// <param name="definition">Experiment definition.</param>
    /// <returns>The created experiment.</returns>
    Task<Experiment> CreateExperimentAsync(ExperimentDefinition definition);

    /// <summary>
    /// Gets an experiment by ID.
    /// </summary>
    /// <param name="experimentId">Experiment identifier.</param>
    /// <returns>The experiment, or null if not found.</returns>
    Task<Experiment?> GetExperimentAsync(Guid experimentId);

    /// <summary>
    /// Gets an experiment by name.
    /// </summary>
    /// <param name="name">Experiment name.</param>
    /// <returns>The experiment, or null if not found.</returns>
    Task<Experiment?> GetExperimentByNameAsync(string name);

    /// <summary>
    /// Gets all active (running) experiments.
    /// </summary>
    /// <returns>Collection of active experiments.</returns>
    Task<IEnumerable<Experiment>> GetActiveExperimentsAsync();

    /// <summary>
    /// Gets experiments matching the specified status.
    /// </summary>
    /// <param name="status">Status to filter by.</param>
    /// <returns>Collection of matching experiments.</returns>
    Task<IEnumerable<Experiment>> GetExperimentsByStatusAsync(ExperimentStatus status);

    /// <summary>
    /// Gets experiments for a specific page path.
    /// </summary>
    /// <param name="pagePath">Target page path.</param>
    /// <returns>Active experiments targeting the path.</returns>
    Task<IEnumerable<Experiment>> GetExperimentsForPageAsync(string pagePath);

    /// <summary>
    /// Updates an existing experiment.
    /// </summary>
    /// <param name="experiment">Updated experiment.</param>
    /// <returns>The updated experiment.</returns>
    Task<Experiment> UpdateExperimentAsync(Experiment experiment);

    /// <summary>
    /// Starts an experiment (changes status to Running).
    /// </summary>
    /// <param name="experimentId">Experiment identifier.</param>
    Task StartExperimentAsync(Guid experimentId);

    /// <summary>
    /// Pauses a running experiment.
    /// </summary>
    /// <param name="experimentId">Experiment identifier.</param>
    Task PauseExperimentAsync(Guid experimentId);

    /// <summary>
    /// Completes an experiment and declares a winner.
    /// </summary>
    /// <param name="experimentId">Experiment identifier.</param>
    /// <param name="winningVariantId">Optional winning variant ID.</param>
    Task CompleteExperimentAsync(Guid experimentId, Guid? winningVariantId = null);

    /// <summary>
    /// Archives an experiment.
    /// </summary>
    /// <param name="experimentId">Experiment identifier.</param>
    Task ArchiveExperimentAsync(Guid experimentId);

    /// <summary>
    /// Deletes an experiment (draft only).
    /// </summary>
    /// <param name="experimentId">Experiment identifier.</param>
    Task DeleteExperimentAsync(Guid experimentId);
}

/// <summary>
/// Service for assigning users to experiment variants.
/// </summary>
public interface IVariantAssignmentService
{
    /// <summary>
    /// Assigns a user to an experiment variant.
    /// </summary>
    /// <param name="experimentId">Experiment identifier.</param>
    /// <param name="userId">User identifier.</param>
    /// <returns>The assigned variant.</returns>
    Task<ExperimentVariant> AssignVariantAsync(Guid experimentId, string userId);

    /// <summary>
    /// Gets the user's current variant assignment for an experiment.
    /// </summary>
    /// <param name="experimentId">Experiment identifier.</param>
    /// <param name="userId">User identifier.</param>
    /// <returns>The assigned variant, or null if not assigned.</returns>
    Task<ExperimentVariant?> GetAssignmentAsync(Guid experimentId, string userId);

    /// <summary>
    /// Gets all assignments for a user.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <returns>Collection of experiment assignments.</returns>
    Task<IEnumerable<ExperimentAssignment>> GetUserAssignmentsAsync(string userId);

    /// <summary>
    /// Clears all experiment assignments for a user.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    Task ClearAssignmentsAsync(string userId);

    /// <summary>
    /// Forces a specific variant assignment (for testing).
    /// </summary>
    /// <param name="experimentId">Experiment identifier.</param>
    /// <param name="variantId">Variant identifier.</param>
    /// <param name="userId">User identifier.</param>
    Task ForceAssignmentAsync(Guid experimentId, Guid variantId, string userId);
}

/// <summary>
/// Service for tracking experiment conversions.
/// </summary>
public interface IConversionTrackingService
{
    /// <summary>
    /// Records a conversion for an experiment.
    /// </summary>
    /// <param name="experimentId">Experiment identifier.</param>
    /// <param name="userId">User identifier.</param>
    /// <param name="goalCodeName">Goal code name.</param>
    /// <param name="value">Optional conversion value.</param>
    /// <param name="metadata">Optional metadata.</param>
    Task RecordConversionAsync(
        Guid experimentId,
        string userId,
        string goalCodeName,
        decimal? value = null,
        Dictionary<string, string>? metadata = null);

    /// <summary>
    /// Records a conversion by goal name.
    /// </summary>
    /// <param name="goalCodeName">Goal code name (will match across experiments).</param>
    /// <param name="userId">User identifier.</param>
    /// <param name="value">Optional conversion value.</param>
    Task RecordConversionByGoalAsync(string goalCodeName, string userId, decimal? value = null);

    /// <summary>
    /// Gets conversion events for an experiment.
    /// </summary>
    /// <param name="experimentId">Experiment identifier.</param>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <returns>Collection of conversion events.</returns>
    Task<IEnumerable<ConversionEvent>> GetConversionsAsync(
        Guid experimentId,
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// Gets conversions for a specific user.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <returns>Collection of conversion events.</returns>
    Task<IEnumerable<ConversionEvent>> GetUserConversionsAsync(string userId);
}

/// <summary>
/// Service for statistical analysis of experiment results.
/// </summary>
public interface IStatisticsService
{
    /// <summary>
    /// Gets complete results for an experiment.
    /// </summary>
    /// <param name="experimentId">Experiment identifier.</param>
    /// <returns>Experiment results with statistical analysis.</returns>
    Task<ExperimentResults> GetResultsAsync(Guid experimentId);

    /// <summary>
    /// Calculates statistical significance between two variants.
    /// </summary>
    /// <param name="controlConversions">Control variant conversions.</param>
    /// <param name="controlVisitors">Control variant visitors.</param>
    /// <param name="treatmentConversions">Treatment variant conversions.</param>
    /// <param name="treatmentVisitors">Treatment variant visitors.</param>
    /// <param name="confidenceLevel">Desired confidence level.</param>
    /// <returns>Statistical analysis.</returns>
    StatisticalAnalysis CalculateSignificance(
        int controlConversions,
        int controlVisitors,
        int treatmentConversions,
        int treatmentVisitors,
        double confidenceLevel = 0.95);

    /// <summary>
    /// Calculates required sample size for an experiment.
    /// </summary>
    /// <param name="baselineConversionRate">Expected baseline conversion rate.</param>
    /// <param name="minimumDetectableEffect">Minimum relative improvement to detect.</param>
    /// <param name="confidenceLevel">Desired confidence level.</param>
    /// <param name="power">Desired statistical power.</param>
    /// <returns>Required sample size per variant.</returns>
    int CalculateRequiredSampleSize(
        double baselineConversionRate,
        double minimumDetectableEffect,
        double confidenceLevel = 0.95,
        double power = 0.8);

    /// <summary>
    /// Estimates days remaining to reach significance.
    /// </summary>
    /// <param name="experimentId">Experiment identifier.</param>
    /// <returns>Estimated days, or null if cannot estimate.</returns>
    Task<int?> EstimateDaysRemainingAsync(Guid experimentId);

    /// <summary>
    /// Generates a report for multiple experiments.
    /// </summary>
    /// <param name="experimentIds">Experiment identifiers to include.</param>
    /// <param name="startDate">Optional date range start.</param>
    /// <param name="endDate">Optional date range end.</param>
    /// <returns>Experiment report.</returns>
    Task<ExperimentReport> GenerateReportAsync(
        IEnumerable<Guid>? experimentIds = null,
        DateTime? startDate = null,
        DateTime? endDate = null);
}

/// <summary>
/// Service for traffic splitting and targeting.
/// </summary>
public interface ITrafficSplitService
{
    /// <summary>
    /// Determines if a user should be included in an experiment.
    /// </summary>
    /// <param name="experiment">The experiment.</param>
    /// <param name="userId">User identifier.</param>
    /// <param name="context">Optional context for targeting.</param>
    /// <returns>True if user should be included.</returns>
    bool ShouldIncludeUser(Experiment experiment, string userId, TrafficContext? context = null);

    /// <summary>
    /// Selects a variant for a user based on traffic allocation.
    /// </summary>
    /// <param name="experiment">The experiment.</param>
    /// <param name="userId">User identifier.</param>
    /// <returns>Selected variant.</returns>
    ExperimentVariant SelectVariant(Experiment experiment, string userId);

    /// <summary>
    /// Gets the bucket number for a user (0-99).
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="salt">Optional salt for hashing.</param>
    /// <returns>Bucket number (0-99).</returns>
    int GetUserBucket(string userId, string? salt = null);
}

/// <summary>
/// Context for traffic targeting decisions.
/// </summary>
public class TrafficContext
{
    /// <summary>
    /// User's device type.
    /// </summary>
    public DeviceType? DeviceType { get; set; }

    /// <summary>
    /// User's country code (ISO).
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// User segments the visitor belongs to.
    /// </summary>
    public IEnumerable<string> UserSegments { get; set; } = [];

    /// <summary>
    /// Request URL.
    /// </summary>
    public string? RequestUrl { get; set; }

    /// <summary>
    /// User agent string.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Is preview/admin mode.
    /// </summary>
    public bool IsPreviewMode { get; set; }
}
