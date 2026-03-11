using System.Text.Json;
using Baseline.Experiments.Classes;
using Baseline.Experiments.Configuration;
using Baseline.Experiments.Interfaces;
using Baseline.Experiments.Models;
using CMS.DataEngine;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Experiments.Services;

/// <summary>
/// Core service for managing A/B testing experiments.
/// Persists to database via Kentico Info/Provider pattern.
/// </summary>
public class ExperimentService(
    IInfoProvider<ExperimentInfo> experimentProvider,
    IInfoProvider<ExperimentVariantInfo> variantProvider,
    IInfoProvider<ExperimentGoalInfo> goalProvider,
    IMemoryCache cache,
    IOptions<BaselineExperimentsOptions> options,
    ILogger<ExperimentService> logger) : IExperimentService
{
    private readonly BaselineExperimentsOptions _options = options.Value;
    private const string CacheKeyPrefix = "experiment:";

    /// <inheritdoc />
    public async Task<Experiment> CreateExperimentAsync(ExperimentDefinition definition)
    {
        var experimentGuid = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var info = new ExperimentInfo
        {
            ExperimentGUID = experimentGuid,
            Name = definition.Name,
            Description = definition.Description,
            ExperimentType = definition.Type.ToString(),
            Status = ExperimentStatus.Draft.ToString(),
            ConfidenceLevel = definition.ConfidenceLevel,
            MinimumSampleSize = definition.MinimumSampleSize ?? _options.MinimumSampleSize,
            StartDateUtc = definition.StartDate?.ToUniversalTime(),
            EndDateUtc = definition.EndDate?.ToUniversalTime(),
            TargetPath = definition.TargetPath,
            TrafficAllocationJson = definition.TrafficAllocation != null
                ? JsonSerializer.Serialize(definition.TrafficAllocation)
                : null,
            CreatedAtUtc = now,
            ModifiedAtUtc = now,
        };

        experimentProvider.Set(info);

        // Create variants
        var variants = new List<ExperimentVariant>();
        foreach (var variantDef in definition.Variants)
        {
            var vi = new ExperimentVariantInfo
            {
                ExperimentVariantGUID = Guid.NewGuid(),
                ExperimentID = info.ExperimentID,
                Name = variantDef.Name,
                Description = variantDef.Description,
                IsControl = variantDef.IsControl,
                Weight = variantDef.Weight,
                Configuration = variantDef.Configuration,
            };
            variantProvider.Set(vi);
            variants.Add(MapVariant(vi));
        }

        // Ensure at least one control
        if (variants.Count > 0 && !variants.Any(v => v.IsControl))
        {
            variants[0].IsControl = true;
            var firstVi = variantProvider.Get()
                .WhereEquals(nameof(ExperimentVariantInfo.ExperimentID), info.ExperimentID)
                .FirstOrDefault();
            if (firstVi != null)
            {
                firstVi.IsControl = true;
                variantProvider.Set(firstVi);
            }
        }

        // Create goals
        var goals = new List<ExperimentGoal>();
        foreach (var goalDef in definition.Goals)
        {
            var gi = new ExperimentGoalInfo
            {
                ExperimentGoalGUID = Guid.NewGuid(),
                ExperimentID = info.ExperimentID,
                Name = goalDef.Name,
                CodeName = goalDef.CodeName,
                GoalType = goalDef.Type.ToString(),
                Target = goalDef.Target,
                IsPrimary = goalDef.IsPrimary,
                GoalValue = 0,
            };
            goalProvider.Set(gi);
            goals.Add(MapGoal(gi));
        }

        // Ensure at least one primary goal
        if (goals.Count > 0 && !goals.Any(g => g.IsPrimary))
        {
            goals[0].IsPrimary = true;
            var firstGi = goalProvider.Get()
                .WhereEquals(nameof(ExperimentGoalInfo.ExperimentID), info.ExperimentID)
                .FirstOrDefault();
            if (firstGi != null)
            {
                firstGi.IsPrimary = true;
                goalProvider.Set(firstGi);
            }
        }

        var experiment = MapExperiment(info, variants, goals);

        logger.LogInformation("Created experiment {ExperimentId}: {ExperimentName} with {VariantCount} variants",
            experiment.Id, experiment.Name, experiment.Variants.Count);

        return experiment;
    }

    /// <inheritdoc />
    public async Task<Experiment?> GetExperimentAsync(Guid experimentId)
    {
        var cacheKey = $"{CacheKeyPrefix}{experimentId}";

        if (cache.TryGetValue(cacheKey, out Experiment? cached))
        {
            return cached;
        }

        var info = experimentProvider.Get()
            .WhereEquals(nameof(ExperimentInfo.ExperimentGUID), experimentId)
            .FirstOrDefault();

        if (info == null) return null;

        var experiment = LoadFull(info);
        cache.Set(cacheKey, experiment, TimeSpan.FromMinutes(_options.ExperimentCacheDurationMinutes));
        return experiment;
    }

    /// <inheritdoc />
    public async Task<Experiment?> GetExperimentByNameAsync(string name)
    {
        var info = experimentProvider.Get()
            .WhereEquals(nameof(ExperimentInfo.Name), name)
            .FirstOrDefault();

        return info == null ? null : LoadFull(info);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Experiment>> GetActiveExperimentsAsync()
        => GetExperimentsByStatusAsync(ExperimentStatus.Running);

    /// <inheritdoc />
    public async Task<IEnumerable<Experiment>> GetExperimentsByStatusAsync(ExperimentStatus status)
    {
        var infos = experimentProvider.Get()
            .WhereEquals(nameof(ExperimentInfo.Status), status.ToString())
            .ToList();

        return infos.Select(i => LoadFull(i)).ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Experiment>> GetExperimentsForPageAsync(string pagePath)
    {
        var infos = experimentProvider.Get()
            .WhereEquals(nameof(ExperimentInfo.Status), ExperimentStatus.Running.ToString())
            .WhereEquals(nameof(ExperimentInfo.ExperimentType), ExperimentType.Page.ToString())
            .WhereNotEmpty(nameof(ExperimentInfo.TargetPath))
            .ToList();

        // Filter by path prefix in-memory (SQL LIKE would also work)
        return infos
            .Where(i => pagePath.StartsWith(i.TargetPath!, StringComparison.OrdinalIgnoreCase))
            .Select(i => LoadFull(i))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<Experiment> UpdateExperimentAsync(Experiment experiment)
    {
        var info = experimentProvider.Get()
            .WhereEquals(nameof(ExperimentInfo.ExperimentGUID), experiment.Id)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"Experiment {experiment.Id} not found");

        info.Name = experiment.Name;
        info.Description = experiment.Description;
        info.ExperimentType = experiment.Type.ToString();
        info.Status = experiment.Status.ToString();
        info.ConfidenceLevel = experiment.ConfidenceLevel;
        info.MinimumSampleSize = experiment.MinimumSampleSize;
        info.StartDateUtc = experiment.StartDateUtc;
        info.EndDateUtc = experiment.EndDateUtc;
        info.TargetPath = experiment.TargetPath;
        info.WidgetIdentifier = experiment.WidgetIdentifier;
        info.TrafficAllocationJson = JsonSerializer.Serialize(experiment.TrafficAllocation);
        info.ModifiedAtUtc = DateTime.UtcNow;
        info.CreatedBy = experiment.CreatedBy;

        experimentProvider.Set(info);
        InvalidateCache(experiment.Id);

        logger.LogInformation("Updated experiment {ExperimentId}: {ExperimentName}",
            experiment.Id, experiment.Name);

        return experiment;
    }

    /// <inheritdoc />
    public async Task StartExperimentAsync(Guid experimentId)
    {
        var experiment = await GetExperimentAsync(experimentId)
            ?? throw new InvalidOperationException($"Experiment {experimentId} not found");

        if (experiment.Status != ExperimentStatus.Draft && experiment.Status != ExperimentStatus.Paused)
            throw new InvalidOperationException($"Cannot start experiment in {experiment.Status} status");

        experiment.Status = ExperimentStatus.Running;
        experiment.StartDateUtc ??= DateTime.UtcNow;

        await UpdateExperimentAsync(experiment);
        logger.LogInformation("Started experiment {ExperimentId}: {Name}", experimentId, experiment.Name);
    }

    /// <inheritdoc />
    public async Task PauseExperimentAsync(Guid experimentId)
    {
        var experiment = await GetExperimentAsync(experimentId)
            ?? throw new InvalidOperationException($"Experiment {experimentId} not found");

        if (experiment.Status != ExperimentStatus.Running)
            throw new InvalidOperationException($"Cannot pause experiment in {experiment.Status} status");

        experiment.Status = ExperimentStatus.Paused;
        await UpdateExperimentAsync(experiment);
        logger.LogInformation("Paused experiment {ExperimentId}: {Name}", experimentId, experiment.Name);
    }

    /// <inheritdoc />
    public async Task CompleteExperimentAsync(Guid experimentId, Guid? winningVariantId = null)
    {
        var experiment = await GetExperimentAsync(experimentId)
            ?? throw new InvalidOperationException($"Experiment {experimentId} not found");

        experiment.Status = ExperimentStatus.Completed;
        experiment.EndDateUtc = DateTime.UtcNow;

        await UpdateExperimentAsync(experiment);
        logger.LogInformation("Completed experiment {ExperimentId}: {Name}, Winner: {WinningVariantId}",
            experimentId, experiment.Name, winningVariantId);
    }

    /// <inheritdoc />
    public async Task ArchiveExperimentAsync(Guid experimentId)
    {
        var experiment = await GetExperimentAsync(experimentId)
            ?? throw new InvalidOperationException($"Experiment {experimentId} not found");

        experiment.Status = ExperimentStatus.Archived;
        await UpdateExperimentAsync(experiment);
        logger.LogInformation("Archived experiment {ExperimentId}: {Name}", experimentId, experiment.Name);
    }

    /// <inheritdoc />
    public async Task DeleteExperimentAsync(Guid experimentId)
    {
        var info = experimentProvider.Get()
            .WhereEquals(nameof(ExperimentInfo.ExperimentGUID), experimentId)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"Experiment {experimentId} not found");

        if (info.Status != ExperimentStatus.Draft.ToString())
            throw new InvalidOperationException("Can only delete experiments in Draft status");

        // Delete children first
        var variants = variantProvider.Get()
            .WhereEquals(nameof(ExperimentVariantInfo.ExperimentID), info.ExperimentID)
            .ToList();
        foreach (var v in variants) variantProvider.Delete(v);

        var goals = goalProvider.Get()
            .WhereEquals(nameof(ExperimentGoalInfo.ExperimentID), info.ExperimentID)
            .ToList();
        foreach (var g in goals) goalProvider.Delete(g);

        experimentProvider.Delete(info);
        InvalidateCache(experimentId);

        logger.LogInformation("Deleted experiment {ExperimentId}: {Name}", experimentId, info.Name);
    }

    #region Mapping helpers

    private Experiment LoadFull(ExperimentInfo info)
    {
        var variants = variantProvider.Get()
            .WhereEquals(nameof(ExperimentVariantInfo.ExperimentID), info.ExperimentID)
            .ToList()
            .Select(MapVariant)
            .ToList();

        var goals = goalProvider.Get()
            .WhereEquals(nameof(ExperimentGoalInfo.ExperimentID), info.ExperimentID)
            .ToList()
            .Select(MapGoal)
            .ToList();

        return MapExperiment(info, variants, goals);
    }

    private static Experiment MapExperiment(ExperimentInfo info, List<ExperimentVariant> variants, List<ExperimentGoal> goals)
    {
        var traffic = string.IsNullOrEmpty(info.TrafficAllocationJson)
            ? new TrafficAllocation()
            : JsonSerializer.Deserialize<TrafficAllocation>(info.TrafficAllocationJson) ?? new TrafficAllocation();

        return new Experiment
        {
            Id = info.ExperimentGUID,
            Name = info.Name,
            Description = info.Description,
            Type = Enum.TryParse<ExperimentType>(info.ExperimentType, out var t) ? t : ExperimentType.Page,
            Status = Enum.TryParse<ExperimentStatus>(info.Status, out var s) ? s : ExperimentStatus.Draft,
            ConfidenceLevel = info.ConfidenceLevel,
            MinimumSampleSize = info.MinimumSampleSize,
            StartDateUtc = info.StartDateUtc,
            EndDateUtc = info.EndDateUtc,
            TargetPath = info.TargetPath,
            WidgetIdentifier = info.WidgetIdentifier,
            TrafficAllocation = traffic,
            CreatedAtUtc = info.CreatedAtUtc,
            ModifiedAtUtc = info.ModifiedAtUtc,
            CreatedBy = info.CreatedBy,
            Variants = variants,
            Goals = goals,
        };
    }

    private static ExperimentVariant MapVariant(ExperimentVariantInfo vi) => new()
    {
        Id = vi.ExperimentVariantGUID,
        Name = vi.Name,
        Description = vi.Description,
        IsControl = vi.IsControl,
        Weight = vi.Weight,
        Configuration = vi.Configuration,
        ContentPath = vi.ContentPath,
        WidgetConfiguration = vi.WidgetConfiguration,
    };

    private static ExperimentGoal MapGoal(ExperimentGoalInfo gi) => new()
    {
        Id = gi.ExperimentGoalGUID,
        Name = gi.Name,
        CodeName = gi.CodeName,
        Type = Enum.TryParse<GoalType>(gi.GoalType, out var gt) ? gt : GoalType.PageView,
        Target = gi.Target,
        IsPrimary = gi.IsPrimary,
        Value = gi.GoalValue > 0 ? (decimal)gi.GoalValue : null,
    };

    #endregion

    private void InvalidateCache(Guid experimentId)
    {
        cache.Remove($"{CacheKeyPrefix}{experimentId}");
    }
}
