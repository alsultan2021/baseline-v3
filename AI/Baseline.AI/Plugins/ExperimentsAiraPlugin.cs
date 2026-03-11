using System.ComponentModel;
using System.Text;
using System.Text.Json;

using Baseline.Experiments.Interfaces;
using Baseline.Experiments.Models;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Baseline.AI.Plugins;

/// <summary>
/// AIRA plugin for managing A/B testing experiments — listing, creating,
/// controlling lifecycle, viewing results, and calculating sample sizes.
/// </summary>
[Description("Manages A/B testing experiments: list, create, start/pause/complete, " +
             "view statistical results, and calculate required sample sizes.")]
public sealed class ExperimentsAiraPlugin(
    IServiceProvider serviceProvider,
    ILogger<ExperimentsAiraPlugin> logger) : IAiraPlugin
{
    /// <inheritdoc />
    public string PluginName => "Experiments";

    // ──────────────────────────────────────────────────────────────
    //  List / query experiments
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Lists experiments filtered by status.
    /// </summary>
    [KernelFunction("list_experiments")]
    [Description("Lists A/B testing experiments. Optionally filter by status " +
                 "(Draft, Running, Paused, Completed, Archived) or page path.")]
    public async Task<string> ListExperimentsAsync(
        [Description("Status filter (Draft, Running, Paused, Completed, Archived). " +
                     "Omit for all statuses.")] string? status = null,
        [Description("Page path to filter by (e.g. /blog/post). Omit for all pages.")] string? pagePath = null)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var experimentService = scope.ServiceProvider.GetService<IExperimentService>();
            if (experimentService is null)
            {
                return "Error: Experiment service not available. Ensure Baseline.Experiments is configured.";
            }

            IEnumerable<Experiment> experiments;

            if (!string.IsNullOrWhiteSpace(pagePath))
            {
                experiments = await experimentService.GetExperimentsForPageAsync(pagePath);
            }
            else if (!string.IsNullOrWhiteSpace(status) &&
                     Enum.TryParse<ExperimentStatus>(status, ignoreCase: true, out var parsedStatus))
            {
                experiments = await experimentService.GetExperimentsByStatusAsync(parsedStatus);
            }
            else
            {
                // Return active by default when no filter
                experiments = await experimentService.GetActiveExperimentsAsync();
            }

            var list = experiments.ToList();
            if (list.Count == 0)
            {
                return "No experiments found matching the criteria.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## Experiments ({list.Count})");
            sb.AppendLine();
            sb.AppendLine("| Name | Type | Status | Variants | Target | Created |");
            sb.AppendLine("|------|------|--------|----------|--------|---------|");

            foreach (var exp in list)
            {
                string target = exp.TargetPath ?? exp.WidgetIdentifier ?? "—";
                sb.AppendLine($"| {exp.Name} | {exp.Type} | {exp.Status} | " +
                    $"{exp.Variants.Count} | {Truncate(target, 30)} | {exp.CreatedAtUtc:yyyy-MM-dd} |");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Experiments: list failed");
            return $"Error: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Get experiment results
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Retrieves statistical results for an experiment.
    /// </summary>
    [KernelFunction("get_experiment_results")]
    [Description("Gets detailed statistical results for an experiment including conversion " +
                 "rates, significance, confidence intervals, and winner recommendation. " +
                 "Provide experiment name or ID.")]
    public async Task<string> GetExperimentResultsAsync(
        [Description("Experiment name or GUID")] string experimentIdentifier)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var experimentService = scope.ServiceProvider.GetService<IExperimentService>();
            var statisticsService = scope.ServiceProvider.GetService<IStatisticsService>();

            if (experimentService is null || statisticsService is null)
            {
                return "Error: Experiment or statistics services not available.";
            }

            var experiment = await ResolveExperimentAsync(experimentService, experimentIdentifier);
            if (experiment is null)
            {
                return $"Error: Experiment '{experimentIdentifier}' not found.";
            }

            var results = await statisticsService.GetResultsAsync(experiment.Id);
            int? daysRemaining = await statisticsService.EstimateDaysRemainingAsync(experiment.Id);

            var sb = new StringBuilder();
            sb.AppendLine($"## Experiment Results: {results.ExperimentName}");
            sb.AppendLine($"**Status**: {results.Status} | **Total Visitors**: {results.TotalVisitors:N0} | **Total Conversions**: {results.TotalConversions:N0}");
            sb.AppendLine();

            // Variant table
            sb.AppendLine("### Variant Performance");
            sb.AppendLine("| Variant | Visitors | Conversions | Rate | vs Control | CI (95%) |");
            sb.AppendLine("|---------|----------|-------------|------|------------|----------|");

            foreach (var v in results.VariantResults)
            {
                string improvement = v.ImprovementOverControl.HasValue
                    ? $"{v.ImprovementOverControl:+0.0;-0.0}%"
                    : "—";
                string ci = $"[{v.ConfidenceIntervalLower:P1}, {v.ConfidenceIntervalUpper:P1}]";
                string control = v.IsControl ? " (control)" : "";

                sb.AppendLine($"| {v.VariantName}{control} | {v.Visitors:N0} | {v.Conversions:N0} | " +
                    $"{v.ConversionRate:P2} | {improvement} | {ci} |");
            }

            sb.AppendLine();

            // Statistical summary
            var stats = results.Statistics;
            sb.AppendLine("### Statistical Analysis");
            sb.AppendLine($"- **P-value**: {stats.PValue:F4}");
            sb.AppendLine($"- **Z-score**: {stats.ZScore:F3}");
            sb.AppendLine($"- **Significant**: {(stats.IsSignificant ? "✅ Yes" : "❌ Not yet")} (at {stats.ConfidenceLevel:P0})");
            sb.AppendLine($"- **Effect size**: {stats.EffectSize:F3} (Cohen's d)");
            sb.AppendLine($"- **Sample progress**: {stats.SampleProgress:P0} ({stats.CurrentSampleSize}/{stats.RequiredSampleSize} per variant)");

            if (stats.BayesianProbabilityToBeatControl.HasValue)
            {
                sb.AppendLine($"- **Bayesian prob. to beat control**: {stats.BayesianProbabilityToBeatControl:P1}");
            }

            if (daysRemaining.HasValue)
            {
                sb.AppendLine($"- **Est. days remaining**: {daysRemaining}");
            }

            // Winner
            if (results.WinningVariantId.HasValue)
            {
                var winner = results.VariantResults
                    .FirstOrDefault(v => v.VariantId == results.WinningVariantId);
                sb.AppendLine();
                sb.AppendLine($"### Recommendation: **{winner?.VariantName ?? "Unknown"}** " +
                    $"(confidence: {results.WinningConfidence:P1})");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Experiments: results failed for {Id}", experimentIdentifier);
            return $"Error: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Create page experiment
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a page-level A/B test.
    /// </summary>
    [KernelFunction("create_page_experiment")]
    [Description("Creates a new page A/B test experiment. Specify the original page, variant " +
                 "page, and what to measure. The experiment starts in Draft status.")]
    public async Task<string> CreatePageExperimentAsync(
        [Description("Experiment name (e.g. 'Homepage Hero Test')")] string name,
        [Description("Original page path (e.g. /)")] string originalPath,
        [Description("Variant page path (e.g. /homepage-variant-b)")] string variantPath,
        [Description("Goal to track (e.g. 'form_submission', 'purchase', 'page_view')")] string goalCodeName,
        [Description("Description / hypothesis for the experiment")] string? description = null)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var pageExpService = scope.ServiceProvider.GetService<IPageExperimentService>();
            if (pageExpService is null)
            {
                return "Error: Page experiment service not available.";
            }

            var experiment = await pageExpService.CreatePageExperimentAsync(
                name, originalPath, variantPath, goalCodeName);

            if (!string.IsNullOrWhiteSpace(description))
            {
                var experimentService = scope.ServiceProvider.GetService<IExperimentService>();
                if (experimentService is not null)
                {
                    experiment.Description = description;
                    await experimentService.UpdateExperimentAsync(experiment);
                }
            }

            logger.LogInformation("Experiments: Created page experiment '{Name}' ({Id})", name, experiment.Id);

            var sb = new StringBuilder();
            sb.AppendLine($"## Experiment Created: {experiment.Name}");
            sb.AppendLine($"- **ID**: {experiment.Id}");
            sb.AppendLine($"- **Type**: {experiment.Type}");
            sb.AppendLine($"- **Status**: {experiment.Status} (use `start_experiment` to begin)");
            sb.AppendLine($"- **Control**: {originalPath}");
            sb.AppendLine($"- **Variant**: {variantPath}");
            sb.AppendLine($"- **Goal**: {goalCodeName}");

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Experiments: create failed for '{Name}'", name);
            return $"Error: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Lifecycle control
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Controls experiment lifecycle — start, pause, complete, archive, or delete.
    /// </summary>
    [KernelFunction("control_experiment")]
    [Description("Control experiment lifecycle. Actions: start (begin collecting data), " +
                 "pause (temporarily stop), complete (declare winner), archive, delete (draft only).")]
    public async Task<string> ControlExperimentAsync(
        [Description("Experiment name or GUID")] string experimentIdentifier,
        [Description("Action: start, pause, complete, archive, delete")] string action,
        [Description("Winning variant name or ID (required for 'complete' action)")] string? winningVariant = null)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var experimentService = scope.ServiceProvider.GetService<IExperimentService>();
            if (experimentService is null)
            {
                return "Error: Experiment service not available.";
            }

            var experiment = await ResolveExperimentAsync(experimentService, experimentIdentifier);
            if (experiment is null)
            {
                return $"Error: Experiment '{experimentIdentifier}' not found.";
            }

            switch (action.ToLowerInvariant())
            {
                case "start":
                    await experimentService.StartExperimentAsync(experiment.Id);
                    return $"Experiment '{experiment.Name}' started. Collecting data.";

                case "pause":
                    await experimentService.PauseExperimentAsync(experiment.Id);
                    return $"Experiment '{experiment.Name}' paused.";

                case "complete":
                    Guid? winnerId = null;
                    if (!string.IsNullOrWhiteSpace(winningVariant))
                    {
                        var variant = experiment.Variants.FirstOrDefault(v =>
                            v.Name.Equals(winningVariant, StringComparison.OrdinalIgnoreCase) ||
                            (Guid.TryParse(winningVariant, out var vid) && v.Id == vid));
                        winnerId = variant?.Id;
                    }

                    await experimentService.CompleteExperimentAsync(experiment.Id, winnerId);
                    return $"Experiment '{experiment.Name}' completed." +
                        (winnerId.HasValue ? $" Winner: {winningVariant}" : "");

                case "archive":
                    await experimentService.ArchiveExperimentAsync(experiment.Id);
                    return $"Experiment '{experiment.Name}' archived.";

                case "delete":
                    await experimentService.DeleteExperimentAsync(experiment.Id);
                    return $"Experiment '{experiment.Name}' deleted.";

                default:
                    return $"Unknown action '{action}'. Use: start, pause, complete, archive, delete.";
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Experiments: {Action} failed for {Id}", action, experimentIdentifier);
            return $"Error: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Sample size calculator
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Calculates the sample size needed for an experiment.
    /// </summary>
    [KernelFunction("calculate_sample_size")]
    [Description("Calculates required sample size per variant to achieve statistical " +
                 "significance. Input your baseline conversion rate and the minimum improvement " +
                 "you want to detect.")]
    public string CalculateSampleSize(
        [Description("Current baseline conversion rate (e.g. 0.05 for 5%)")] double baselineRate,
        [Description("Minimum detectable effect as relative change (e.g. 0.2 for 20% improvement)")] double minimumDetectableEffect,
        [Description("Confidence level 0-1. Default 0.95")] double? confidenceLevel = null,
        [Description("Statistical power 0-1. Default 0.8")] double? power = null)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var statsService = scope.ServiceProvider.GetService<IStatisticsService>();
            if (statsService is null)
            {
                return "Error: Statistics service not available.";
            }

            double conf = confidenceLevel ?? 0.95;
            double pow = power ?? 0.8;

            int sampleSize = statsService.CalculateRequiredSampleSize(
                baselineRate, minimumDetectableEffect, conf, pow);

            double targetRate = baselineRate * (1 + minimumDetectableEffect);

            var sb = new StringBuilder();
            sb.AppendLine("## Sample Size Calculation");
            sb.AppendLine($"- **Baseline rate**: {baselineRate:P2}");
            sb.AppendLine($"- **Target rate**: {targetRate:P2} ({minimumDetectableEffect:P0} improvement)");
            sb.AppendLine($"- **Confidence**: {conf:P0}");
            sb.AppendLine($"- **Power**: {pow:P0}");
            sb.AppendLine();
            sb.AppendLine($"### Required: **{sampleSize:N0}** visitors per variant");
            sb.AppendLine($"Total needed: **{sampleSize * 2:N0}** visitors (2-variant test)");

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Experiments: sample size calc failed");
            return $"Error: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Experiment report
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a summary report of all or selected experiments.
    /// </summary>
    [KernelFunction("experiment_report")]
    [Description("Generates a summary report across multiple experiments. " +
                 "Shows overall performance, winners, and active tests.")]
    public async Task<string> ExperimentReportAsync(
        [Description("Date range start (yyyy-MM-dd). Omit for all time.")] string? startDate = null,
        [Description("Date range end (yyyy-MM-dd). Omit for today.")] string? endDate = null)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var statsService = scope.ServiceProvider.GetService<IStatisticsService>();
            if (statsService is null)
            {
                return "Error: Statistics service not available.";
            }

            DateTime? start = startDate is not null ? DateTime.Parse(startDate) : null;
            DateTime? end = endDate is not null ? DateTime.Parse(endDate) : null;

            var report = await statsService.GenerateReportAsync(startDate: start, endDate: end);

            var sb = new StringBuilder();
            sb.AppendLine($"## Experiment Report");
            if (report.DateRangeStart.HasValue)
            {
                sb.AppendLine($"Period: {report.DateRangeStart:yyyy-MM-dd} — {report.DateRangeEnd:yyyy-MM-dd}");
            }

            sb.AppendLine();
            sb.AppendLine($"- **Total experiments**: {report.Summary.TotalExperiments}");
            sb.AppendLine();

            if (report.Experiments.Count == 0)
            {
                sb.AppendLine("No experiment data available for this period.");
                return sb.ToString();
            }

            sb.AppendLine("| Experiment | Status | Visitors | Conv. Rate | Significant | Winner |");
            sb.AppendLine("|------------|--------|----------|------------|-------------|--------|");

            foreach (var exp in report.Experiments)
            {
                string significant = exp.IsStatisticallySignificant ? "✅" : "❌";
                var bestVariant = exp.VariantResults
                    .OrderByDescending(v => v.ConversionRate)
                    .FirstOrDefault();
                string winner = exp.WinningVariantId.HasValue
                    ? exp.VariantResults.FirstOrDefault(v => v.VariantId == exp.WinningVariantId)?.VariantName ?? "—"
                    : "—";
                string rate = bestVariant is not null ? $"{bestVariant.ConversionRate:P2}" : "—";

                sb.AppendLine($"| {exp.ExperimentName} | {exp.Status} | {exp.TotalVisitors:N0} | " +
                    $"{rate} | {significant} | {winner} |");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Experiments: report generation failed");
            return $"Error: {ex.Message}";
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  Private helpers
    // ══════════════════════════════════════════════════════════════

    private static async Task<Experiment?> ResolveExperimentAsync(
        IExperimentService service, string identifier)
    {
        if (Guid.TryParse(identifier, out var guid))
        {
            return await service.GetExperimentAsync(guid);
        }

        return await service.GetExperimentByNameAsync(identifier);
    }

    private static string Truncate(string text, int max) =>
        text.Length > max ? string.Concat(text.AsSpan(0, max - 3), "...") : text;
}
