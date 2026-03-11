using Baseline.Experiments.Configuration;
using Baseline.Experiments.Interfaces;
using Baseline.Experiments.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Experiments.Services;

/// <summary>
/// Service for tracking experiment conversions.
/// </summary>
public class ConversionTrackingService(
    IExperimentService experimentService,
    IVariantAssignmentService assignmentService,
    IOptions<BaselineExperimentsOptions> options,
    ILogger<ConversionTrackingService> logger) : IConversionTrackingService
{
    private readonly IExperimentService _experimentService = experimentService;
    private readonly IVariantAssignmentService _assignmentService = assignmentService;
    private readonly BaselineExperimentsOptions _options = options.Value;
    private readonly ILogger<ConversionTrackingService> _logger = logger;

    // In-memory storage (would be database in production)
    private static readonly List<ConversionEvent> _conversions = [];
    private static readonly object _lock = new();

    /// <inheritdoc />
    public async Task RecordConversionAsync(
        Guid experimentId,
        string userId,
        string goalCodeName,
        decimal? value = null,
        Dictionary<string, string>? metadata = null)
    {
        var experiment = await _experimentService.GetExperimentAsync(experimentId);
        if (experiment == null)
        {
            _logger.LogWarning("Cannot record conversion: Experiment {ExperimentId} not found", experimentId);
            return;
        }

        if (experiment.Status != ExperimentStatus.Running)
        {
            _logger.LogDebug("Skipping conversion for non-running experiment {ExperimentId}", experimentId);
            return;
        }

        var goal = experiment.Goals.FirstOrDefault(g =>
            g.CodeName.Equals(goalCodeName, StringComparison.OrdinalIgnoreCase));
        if (goal == null)
        {
            _logger.LogWarning("Goal {GoalCodeName} not found in experiment {ExperimentId}",
                goalCodeName, experimentId);
            return;
        }

        // Get user's variant assignment
        var variant = await _assignmentService.GetAssignmentAsync(experimentId, userId);
        if (variant == null)
        {
            _logger.LogDebug("User {UserId} not assigned to experiment {ExperimentId}, skipping conversion",
                userId, experimentId);
            return;
        }

        var conversionEvent = new ConversionEvent
        {
            Id = Guid.NewGuid(),
            ExperimentId = experimentId,
            VariantId = variant.Id,
            GoalId = goal.Id,
            UserId = userId,
            ConvertedAtUtc = DateTime.UtcNow,
            Value = value ?? goal.Value,
            Metadata = metadata ?? []
        };

        lock (_lock)
        {
            _conversions.Add(conversionEvent);
        }

        _logger.LogInformation(
            "Recorded conversion for experiment {ExperimentId}, variant {VariantId}, goal {GoalCodeName}, user {UserId}",
            experimentId, variant.Id, goalCodeName, userId);
    }

    /// <inheritdoc />
    public async Task RecordConversionByGoalAsync(string goalCodeName, string userId, decimal? value = null)
    {
        // Get all active experiments with this goal
        var activeExperiments = await _experimentService.GetActiveExperimentsAsync();

        foreach (var experiment in activeExperiments)
        {
            if (experiment.Goals.Any(g => g.CodeName.Equals(goalCodeName, StringComparison.OrdinalIgnoreCase)))
            {
                await RecordConversionAsync(experiment.Id, userId, goalCodeName, value);
            }
        }
    }

    /// <inheritdoc />
    public Task<IEnumerable<ConversionEvent>> GetConversionsAsync(
        Guid experimentId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        lock (_lock)
        {
            var query = _conversions.Where(c => c.ExperimentId == experimentId);

            if (startDate.HasValue)
            {
                query = query.Where(c => c.ConvertedAtUtc >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(c => c.ConvertedAtUtc <= endDate.Value);
            }

            return Task.FromResult<IEnumerable<ConversionEvent>>(query.ToList());
        }
    }

    /// <inheritdoc />
    public Task<IEnumerable<ConversionEvent>> GetUserConversionsAsync(string userId)
    {
        lock (_lock)
        {
            var conversions = _conversions
                .Where(c => c.UserId == userId)
                .ToList();
            return Task.FromResult<IEnumerable<ConversionEvent>>(conversions);
        }
    }
}

/// <summary>
/// Service for statistical analysis of experiment results.
/// </summary>
public class StatisticsService(
    IExperimentService experimentService,
    IConversionTrackingService conversionService,
    IOptions<BaselineExperimentsOptions> options,
    ILogger<StatisticsService> logger) : IStatisticsService
{
    private readonly IExperimentService _experimentService = experimentService;
    private readonly IConversionTrackingService _conversionService = conversionService;
    private readonly BaselineExperimentsOptions _options = options.Value;
    private readonly ILogger<StatisticsService> _logger = logger;

    // In-memory visitor tracking (would be database in production)
    private static readonly Dictionary<string, HashSet<string>> _variantVisitors = new();
    private static readonly object _lock = new();

    /// <inheritdoc />
    public async Task<ExperimentResults> GetResultsAsync(Guid experimentId)
    {
        var experiment = await _experimentService.GetExperimentAsync(experimentId)
            ?? throw new InvalidOperationException($"Experiment {experimentId} not found");

        var conversions = await _conversionService.GetConversionsAsync(experimentId);
        var conversionsList = conversions.ToList();

        var results = new ExperimentResults
        {
            ExperimentId = experimentId,
            ExperimentName = experiment.Name,
            Status = experiment.Status,
            CalculatedAtUtc = DateTime.UtcNow
        };

        // Get primary goal
        var primaryGoal = experiment.Goals.FirstOrDefault(g => g.IsPrimary)
            ?? experiment.Goals.FirstOrDefault();

        // Calculate results for each variant
        ExperimentVariant? controlVariant = null;
        VariantResults? controlResults = null;

        foreach (var variant in experiment.Variants)
        {
            var variantConversions = conversionsList
                .Where(c => c.VariantId == variant.Id)
                .ToList();

            var visitors = GetVariantVisitors(experimentId, variant.Id);
            var primaryConversions = primaryGoal != null
                ? variantConversions.Count(c => c.GoalId == primaryGoal.Id)
                : variantConversions.Count;

            var variantResults = new VariantResults
            {
                VariantId = variant.Id,
                VariantName = variant.Name,
                IsControl = variant.IsControl,
                Visitors = visitors,
                Conversions = primaryConversions,
                ConversionRate = visitors > 0 ? (double)primaryConversions / visitors : 0,
                TotalRevenue = variantConversions.Sum(c => c.Value ?? 0),
                AverageRevenuePerVisitor = visitors > 0
                    ? variantConversions.Sum(c => c.Value ?? 0) / visitors
                    : 0
            };

            // Calculate goal-level results
            foreach (var goal in experiment.Goals)
            {
                var goalConversions = variantConversions.Count(c => c.GoalId == goal.Id);
                variantResults.GoalResults.Add(new GoalResults
                {
                    GoalId = goal.Id,
                    GoalName = goal.Name,
                    Conversions = goalConversions,
                    ConversionRate = visitors > 0 ? (double)goalConversions / visitors : 0,
                    TotalValue = variantConversions.Where(c => c.GoalId == goal.Id).Sum(c => c.Value ?? 0)
                });
            }

            if (variant.IsControl)
            {
                controlVariant = variant;
                controlResults = variantResults;
            }

            results.VariantResults.Add(variantResults);
            results.TotalVisitors += visitors;
            results.TotalConversions += primaryConversions;
        }

        // Calculate improvement over control and statistics
        if (controlResults != null)
        {
            foreach (var variantResult in results.VariantResults.Where(v => !v.IsControl))
            {
                if (controlResults.ConversionRate > 0)
                {
                    variantResult.ImprovementOverControl =
                        ((variantResult.ConversionRate - controlResults.ConversionRate) / controlResults.ConversionRate) * 100;
                }

                // Calculate statistical significance
                var analysis = CalculateSignificance(
                    controlResults.Conversions,
                    controlResults.Visitors,
                    variantResult.Conversions,
                    variantResult.Visitors,
                    experiment.ConfidenceLevel);

                if (analysis.IsSignificant && variantResult.ConversionRate > controlResults.ConversionRate)
                {
                    results.IsStatisticallySignificant = true;
                    results.WinningVariantId = variantResult.VariantId;
                    results.WinningConfidence = 1 - analysis.PValue;
                }

                // Set confidence intervals
                variantResult.ConfidenceIntervalLower = variantResult.ConversionRate - (1.96 * Math.Sqrt(variantResult.ConversionRate * (1 - variantResult.ConversionRate) / Math.Max(1, variantResult.Visitors)));
                variantResult.ConfidenceIntervalUpper = variantResult.ConversionRate + (1.96 * Math.Sqrt(variantResult.ConversionRate * (1 - variantResult.ConversionRate) / Math.Max(1, variantResult.Visitors)));

                results.Statistics = analysis;
            }
        }

        // Estimate days remaining
        results.EstimatedDaysRemaining = await EstimateDaysRemainingAsync(experimentId);

        return results;
    }

    /// <inheritdoc />
    public StatisticalAnalysis CalculateSignificance(
        int controlConversions,
        int controlVisitors,
        int treatmentConversions,
        int treatmentVisitors,
        double confidenceLevel = 0.95)
    {
        var analysis = new StatisticalAnalysis
        {
            ConfidenceLevel = confidenceLevel,
            CurrentSampleSize = Math.Min(controlVisitors, treatmentVisitors)
        };

        if (controlVisitors == 0 || treatmentVisitors == 0)
        {
            return analysis;
        }

        var p1 = (double)controlConversions / controlVisitors;
        var p2 = (double)treatmentConversions / treatmentVisitors;
        var pPooled = (double)(controlConversions + treatmentConversions) / (controlVisitors + treatmentVisitors);

        var standardError = Math.Sqrt(pPooled * (1 - pPooled) * (1.0 / controlVisitors + 1.0 / treatmentVisitors));

        if (standardError > 0)
        {
            analysis.ZScore = (p2 - p1) / standardError;
            analysis.PValue = 2 * (1 - NormalCdf(Math.Abs(analysis.ZScore)));
        }

        // Calculate effect size (Cohen's h)
        analysis.EffectSize = 2 * (Math.Asin(Math.Sqrt(p2)) - Math.Asin(Math.Sqrt(p1)));

        // Check significance
        var alpha = 1 - confidenceLevel;
        analysis.IsSignificant = analysis.PValue < alpha;

        // Calculate required sample size
        analysis.RequiredSampleSize = CalculateRequiredSampleSize(
            p1,
            _options.Statistics.MinimumDetectableEffect / 100,
            confidenceLevel,
            _options.Statistics.StatisticalPower);

        // Calculate sample progress
        analysis.SampleProgress = analysis.RequiredSampleSize > 0
            ? (double)analysis.CurrentSampleSize / analysis.RequiredSampleSize * 100
            : 100;

        return analysis;
    }

    /// <inheritdoc />
    public int CalculateRequiredSampleSize(
        double baselineConversionRate,
        double minimumDetectableEffect,
        double confidenceLevel = 0.95,
        double power = 0.8)
    {
        if (baselineConversionRate <= 0 || baselineConversionRate >= 1)
        {
            return _options.MinimumSampleSize;
        }

        var p1 = baselineConversionRate;
        var p2 = p1 * (1 + minimumDetectableEffect);

        var zAlpha = NormalInverseCdf(1 - (1 - confidenceLevel) / 2);
        var zBeta = NormalInverseCdf(power);

        var pooledP = (p1 + p2) / 2;
        var numerator = Math.Pow(zAlpha * Math.Sqrt(2 * pooledP * (1 - pooledP)) + zBeta * Math.Sqrt(p1 * (1 - p1) + p2 * (1 - p2)), 2);
        var denominator = Math.Pow(p2 - p1, 2);

        return (int)Math.Ceiling(numerator / denominator);
    }

    /// <inheritdoc />
    public async Task<int?> EstimateDaysRemainingAsync(Guid experimentId)
    {
        var experiment = await _experimentService.GetExperimentAsync(experimentId);
        if (experiment?.StartDateUtc == null)
        {
            return null;
        }

        var results = await GetResultsAsync(experimentId);
        if (results.TotalVisitors == 0)
        {
            return null;
        }

        var daysSinceStart = (DateTime.UtcNow - experiment.StartDateUtc.Value).TotalDays;
        if (daysSinceStart < 1)
        {
            return null;
        }

        var dailyVisitors = results.TotalVisitors / daysSinceStart;
        var controlResult = results.VariantResults.FirstOrDefault(v => v.IsControl);

        if (controlResult == null || dailyVisitors == 0)
        {
            return null;
        }

        var requiredSampleSize = CalculateRequiredSampleSize(
            controlResult.ConversionRate,
            _options.Statistics.MinimumDetectableEffect / 100);

        var remainingSamples = requiredSampleSize - results.Statistics.CurrentSampleSize;
        if (remainingSamples <= 0)
        {
            return 0;
        }

        return (int)Math.Ceiling(remainingSamples / dailyVisitors);
    }

    /// <inheritdoc />
    public async Task<ExperimentReport> GenerateReportAsync(
        IEnumerable<Guid>? experimentIds = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var report = new ExperimentReport
        {
            Title = $"Experiment Report - {DateTime.UtcNow:yyyy-MM-dd}",
            GeneratedAtUtc = DateTime.UtcNow,
            DateRangeStart = startDate,
            DateRangeEnd = endDate
        };

        IEnumerable<Experiment> experiments;
        if (experimentIds != null && experimentIds.Any())
        {
            var experimentList = new List<Experiment>();
            foreach (var id in experimentIds)
            {
                var exp = await _experimentService.GetExperimentAsync(id);
                if (exp != null)
                {
                    experimentList.Add(exp);
                }
            }
            experiments = experimentList;
        }
        else
        {
            var allExperiments = new List<Experiment>();
            foreach (var status in Enum.GetValues<ExperimentStatus>())
            {
                allExperiments.AddRange(await _experimentService.GetExperimentsByStatusAsync(status));
            }
            experiments = allExperiments;
        }

        foreach (var experiment in experiments)
        {
            var results = await GetResultsAsync(experiment.Id);
            report.Experiments.Add(results);

            report.Summary.TotalExperiments++;
            if (results.Status == ExperimentStatus.Running)
            {
                report.Summary.RunningExperiments++;
            }
            if (results.IsStatisticallySignificant)
            {
                report.Summary.SignificantExperiments++;
            }
            report.Summary.TotalVisitors += results.TotalVisitors;
            report.Summary.TotalConversions += results.TotalConversions;
        }

        // Calculate average improvement
        var improvements = report.Experiments
            .Where(e => e.IsStatisticallySignificant && e.WinningVariantId.HasValue)
            .Select(e => e.VariantResults.FirstOrDefault(v => v.VariantId == e.WinningVariantId)?.ImprovementOverControl ?? 0)
            .ToList();

        if (improvements.Count > 0)
        {
            report.Summary.AverageImprovement = improvements.Average();
        }

        return report;
    }

    private static int GetVariantVisitors(Guid experimentId, Guid variantId)
    {
        var key = $"{experimentId}:{variantId}";
        lock (_lock)
        {
            if (_variantVisitors.TryGetValue(key, out var visitors))
            {
                return visitors.Count;
            }
        }
        return 0;
    }

    /// <summary>
    /// Standard normal cumulative distribution function.
    /// </summary>
    private static double NormalCdf(double x)
    {
        const double a1 = 0.254829592;
        const double a2 = -0.284496736;
        const double a3 = 1.421413741;
        const double a4 = -1.453152027;
        const double a5 = 1.061405429;
        const double p = 0.3275911;

        var sign = x < 0 ? -1 : 1;
        x = Math.Abs(x) / Math.Sqrt(2);

        var t = 1.0 / (1.0 + p * x);
        var y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

        return 0.5 * (1.0 + sign * y);
    }

    /// <summary>
    /// Inverse of standard normal CDF (approximation).
    /// </summary>
    private static double NormalInverseCdf(double p)
    {
        if (p <= 0) return double.NegativeInfinity;
        if (p >= 1) return double.PositiveInfinity;

        // Rational approximation
        var a = new[] { -3.969683028665376e+01, 2.209460984245205e+02, -2.759285104469687e+02, 1.383577518672690e+02, -3.066479806614716e+01, 2.506628277459239e+00 };
        var b = new[] { -5.447609879822406e+01, 1.615858368580409e+02, -1.556989798598866e+02, 6.680131188771972e+01, -1.328068155288572e+01 };
        var c = new[] { -7.784894002430293e-03, -3.223964580411365e-01, -2.400758277161838e+00, -2.549732539343734e+00, 4.374664141464968e+00, 2.938163982698783e+00 };
        var d = new[] { 7.784695709041462e-03, 3.224671290700398e-01, 2.445134137142996e+00, 3.754408661907416e+00 };

        const double pLow = 0.02425;
        const double pHigh = 1 - pLow;

        double q, r;

        if (p < pLow)
        {
            q = Math.Sqrt(-2 * Math.Log(p));
            return (((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) /
                   ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1);
        }

        if (p <= pHigh)
        {
            q = p - 0.5;
            r = q * q;
            return (((((a[0] * r + a[1]) * r + a[2]) * r + a[3]) * r + a[4]) * r + a[5]) * q /
                   (((((b[0] * r + b[1]) * r + b[2]) * r + b[3]) * r + b[4]) * r + 1);
        }

        q = Math.Sqrt(-2 * Math.Log(1 - p));
        return -(((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) /
               ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1);
    }
}
