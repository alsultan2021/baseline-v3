using Baseline.Experiments.Interfaces;
using Baseline.Experiments.Models;
using Microsoft.Extensions.Logging;

namespace Baseline.Experiments.Services;

/// <summary>
/// Service for page-level A/B testing experiments.
/// </summary>
public class PageExperimentService(
    IExperimentService experimentService,
    IVariantAssignmentService assignmentService,
    ITrafficSplitService trafficSplitService,
    ILogger<PageExperimentService> logger) : IPageExperimentService
{
    private readonly IExperimentService _experimentService = experimentService;
    private readonly IVariantAssignmentService _assignmentService = assignmentService;
    private readonly ITrafficSplitService _trafficSplitService = trafficSplitService;
    private readonly ILogger<PageExperimentService> _logger = logger;

    /// <inheritdoc />
    public async Task<PageVariantInfo?> GetPageVariantAsync(string pagePath, string userId)
    {
        var experiments = await _experimentService.GetExperimentsForPageAsync(pagePath);

        foreach (var experiment in experiments)
        {
            // Check if user should be included
            if (!_trafficSplitService.ShouldIncludeUser(experiment, userId))
            {
                continue;
            }

            // Get or assign variant
            var variant = await _assignmentService.GetAssignmentAsync(experiment.Id, userId)
                ?? await _assignmentService.AssignVariantAsync(experiment.Id, userId);

            if (variant != null)
            {
                var renderPath = variant.IsControl
                    ? experiment.TargetPath ?? pagePath
                    : variant.ContentPath ?? experiment.TargetPath ?? pagePath;

                _logger.LogDebug(
                    "Page experiment {ExperimentId}: User {UserId} assigned to variant {VariantName}, rendering {RenderPath}",
                    experiment.Id, userId, variant.Name, renderPath);

                return new PageVariantInfo
                {
                    ExperimentId = experiment.Id,
                    ExperimentName = experiment.Name,
                    VariantId = variant.Id,
                    VariantName = variant.Name,
                    IsControl = variant.IsControl,
                    RenderPath = renderPath,
                    OriginalPath = pagePath
                };
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Experiment>> GetActivePageExperimentsAsync()
    {
        var activeExperiments = await _experimentService.GetActiveExperimentsAsync();
        return activeExperiments.Where(e => e.Type == ExperimentType.Page);
    }

    /// <inheritdoc />
    public async Task<Experiment> CreatePageExperimentAsync(
        string name,
        string targetPath,
        string variantPath,
        string goalCodeName)
    {
        var definition = new ExperimentDefinition
        {
            Name = name,
            Description = $"A/B test for page: {targetPath}",
            Type = ExperimentType.Page,
            TargetPath = targetPath,
            Variants =
            [
                new VariantDefinition
                {
                    Name = "Control",
                    Description = $"Original page: {targetPath}",
                    IsControl = true,
                    Weight = 50
                },
                new VariantDefinition
                {
                    Name = "Variant A",
                    Description = $"Alternative page: {variantPath}",
                    IsControl = false,
                    Weight = 50,
                    Configuration = variantPath
                }
            ],
            Goals =
            [
                new GoalDefinition
                {
                    Name = "Primary Goal",
                    CodeName = goalCodeName,
                    Type = GoalType.PageView,
                    IsPrimary = true
                }
            ]
        };

        // Set variant content path
        var experiment = await _experimentService.CreateExperimentAsync(definition);

        // Update variant with content path
        var variantToUpdate = experiment.Variants.FirstOrDefault(v => !v.IsControl);
        if (variantToUpdate != null)
        {
            variantToUpdate.ContentPath = variantPath;
            await _experimentService.UpdateExperimentAsync(experiment);
        }

        _logger.LogInformation(
            "Created page experiment {ExperimentId}: {ExperimentName} for path {TargetPath}",
            experiment.Id, experiment.Name, targetPath);

        return experiment;
    }
}

/// <summary>
/// Service for widget-level A/B testing experiments.
/// </summary>
public class WidgetExperimentService(
    IExperimentService experimentService,
    IVariantAssignmentService assignmentService,
    ITrafficSplitService trafficSplitService,
    ILogger<WidgetExperimentService> logger) : IWidgetExperimentService
{
    private readonly IExperimentService _experimentService = experimentService;
    private readonly IVariantAssignmentService _assignmentService = assignmentService;
    private readonly ITrafficSplitService _trafficSplitService = trafficSplitService;
    private readonly ILogger<WidgetExperimentService> _logger = logger;

    /// <inheritdoc />
    public async Task<WidgetVariantInfo?> GetWidgetVariantAsync(string widgetIdentifier, string userId)
    {
        var activeExperiments = await _experimentService.GetActiveExperimentsAsync();
        var widgetExperiments = activeExperiments
            .Where(e => e.Type == ExperimentType.Widget
                     && e.WidgetIdentifier?.Equals(widgetIdentifier, StringComparison.OrdinalIgnoreCase) == true);

        foreach (var experiment in widgetExperiments)
        {
            // Check if user should be included
            if (!_trafficSplitService.ShouldIncludeUser(experiment, userId))
            {
                continue;
            }

            // Get or assign variant
            var variant = await _assignmentService.GetAssignmentAsync(experiment.Id, userId)
                ?? await _assignmentService.AssignVariantAsync(experiment.Id, userId);

            if (variant != null)
            {
                _logger.LogDebug(
                    "Widget experiment {ExperimentId}: User {UserId} assigned to variant {VariantName} for widget {WidgetIdentifier}",
                    experiment.Id, userId, variant.Name, widgetIdentifier);

                return new WidgetVariantInfo
                {
                    ExperimentId = experiment.Id,
                    VariantId = variant.Id,
                    IsControl = variant.IsControl,
                    Configuration = variant.WidgetConfiguration
                };
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<Experiment> CreateWidgetExperimentAsync(
        string name,
        string widgetIdentifier,
        IEnumerable<WidgetVariantConfiguration> variants,
        string goalCodeName)
    {
        var variantList = variants.ToList();

        var definition = new ExperimentDefinition
        {
            Name = name,
            Description = $"A/B test for widget: {widgetIdentifier}",
            Type = ExperimentType.Widget,
            Variants = variantList.Select(v => new VariantDefinition
            {
                Name = v.Name,
                Description = $"Widget variant: {v.Name}",
                IsControl = v.IsControl,
                Weight = v.Weight,
                Configuration = v.Configuration
            }).ToList(),
            Goals =
            [
                new GoalDefinition
                {
                    Name = "Primary Goal",
                    CodeName = goalCodeName,
                    Type = GoalType.Click,
                    IsPrimary = true
                }
            ]
        };

        var experiment = await _experimentService.CreateExperimentAsync(definition);
        experiment.WidgetIdentifier = widgetIdentifier;

        // Update variants with widget configuration
        for (var i = 0; i < experiment.Variants.Count && i < variantList.Count; i++)
        {
            experiment.Variants[i].WidgetConfiguration = variantList[i].Configuration;
        }

        await _experimentService.UpdateExperimentAsync(experiment);

        _logger.LogInformation(
            "Created widget experiment {ExperimentId}: {ExperimentName} for widget {WidgetIdentifier}",
            experiment.Id, experiment.Name, widgetIdentifier);

        return experiment;
    }
}
