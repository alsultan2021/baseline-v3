using System.Security.Cryptography;
using System.Text;
using Baseline.Experiments.Configuration;
using Baseline.Experiments.Interfaces;
using Baseline.Experiments.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Experiments.Services;

/// <summary>
/// Service for traffic splitting and variant selection using consistent hashing.
/// </summary>
public class TrafficSplitService(
    IOptions<BaselineExperimentsOptions> options,
    ILogger<TrafficSplitService> logger) : ITrafficSplitService
{
    private readonly BaselineExperimentsOptions _options = options.Value;
    private readonly ILogger<TrafficSplitService> _logger = logger;

    /// <inheritdoc />
    public bool ShouldIncludeUser(Experiment experiment, string userId, TrafficContext? context = null)
    {
        // Check if experiment is active
        if (experiment.Status != ExperimentStatus.Running)
        {
            return false;
        }

        // Check date range
        var now = DateTime.UtcNow;
        if (experiment.StartDateUtc.HasValue && now < experiment.StartDateUtc.Value)
        {
            return false;
        }
        if (experiment.EndDateUtc.HasValue && now > experiment.EndDateUtc.Value)
        {
            return false;
        }

        // Check traffic allocation
        var allocation = experiment.TrafficAllocation;

        // Check included traffic percentage
        if (allocation.IncludedTrafficPercentage < 100)
        {
            var bucket = GetUserBucket(userId, $"{experiment.Id}-inclusion");
            if (bucket >= allocation.IncludedTrafficPercentage)
            {
                if (_options.EnableDebugLogging)
                {
                    _logger.LogDebug("User {UserId} excluded from experiment {ExperimentId} by traffic allocation",
                        userId, experiment.Id);
                }
                return false;
            }
        }

        // Apply targeting rules if context provided
        if (context != null)
        {
            // Check preview mode
            if (context.IsPreviewMode && !_options.EnablePreviewMode)
            {
                return false;
            }

            // Check device targeting
            if (allocation.TargetDevices.Count > 0 && context.DeviceType.HasValue)
            {
                if (!allocation.TargetDevices.Contains(context.DeviceType.Value))
                {
                    return false;
                }
            }

            // Check country targeting
            if (allocation.TargetCountries.Count > 0 && !string.IsNullOrEmpty(context.CountryCode))
            {
                if (!allocation.TargetCountries.Contains(context.CountryCode, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Check segment targeting
            if (allocation.TargetSegments.Count > 0)
            {
                var userSegments = context.UserSegments.ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (!allocation.TargetSegments.Any(s => userSegments.Contains(s)))
                {
                    return false;
                }
            }

            // Check excluded segments
            if (allocation.ExcludedSegments.Count > 0)
            {
                var userSegments = context.UserSegments.ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (allocation.ExcludedSegments.Any(s => userSegments.Contains(s)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <inheritdoc />
    public ExperimentVariant SelectVariant(Experiment experiment, string userId)
    {
        if (experiment.Variants.Count == 0)
        {
            throw new InvalidOperationException($"Experiment {experiment.Id} has no variants");
        }

        if (experiment.Variants.Count == 1)
        {
            return experiment.Variants[0];
        }

        // Use consistent hashing for deterministic assignment
        var bucket = GetUserBucket(userId, $"{experiment.Id}-variant");

        // Calculate cumulative weights
        var totalWeight = experiment.Variants.Sum(v => v.Weight);
        var normalizedBucket = bucket * totalWeight / 100;

        var cumulativeWeight = 0;
        foreach (var variant in experiment.Variants.OrderBy(v => v.IsControl ? 0 : 1))
        {
            cumulativeWeight += variant.Weight;
            if (normalizedBucket < cumulativeWeight)
            {
                if (_options.EnableDebugLogging)
                {
                    _logger.LogDebug("User {UserId} assigned to variant {VariantName} ({VariantId}) for experiment {ExperimentId}",
                        userId, variant.Name, variant.Id, experiment.Id);
                }
                return variant;
            }
        }

        // Fallback to last variant
        return experiment.Variants[^1];
    }

    /// <inheritdoc />
    public int GetUserBucket(string userId, string? salt = null)
    {
        var input = string.IsNullOrEmpty(salt)
            ? $"{userId}-{_options.TrafficAllocation.HashingSalt}"
            : $"{userId}-{salt}";

        var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        var value = BitConverter.ToUInt32(hash, 0);
        return (int)(value % 100);
    }
}

/// <summary>
/// Service for assigning users to experiment variants.
/// </summary>
public class VariantAssignmentService(
    IExperimentService experimentService,
    ITrafficSplitService trafficSplitService,
    IMemoryCache cache,
    IOptions<BaselineExperimentsOptions> options,
    ILogger<VariantAssignmentService> logger) : IVariantAssignmentService
{
    private readonly IExperimentService _experimentService = experimentService;
    private readonly ITrafficSplitService _trafficSplitService = trafficSplitService;
    private readonly IMemoryCache _cache = cache;
    private readonly BaselineExperimentsOptions _options = options.Value;
    private readonly ILogger<VariantAssignmentService> _logger = logger;

    // In-memory storage for assignments (would be database in production)
    private static readonly Dictionary<string, List<ExperimentAssignment>> _assignments = new();
    private static readonly object _lock = new();

    /// <inheritdoc />
    public async Task<ExperimentVariant> AssignVariantAsync(Guid experimentId, string userId)
    {
        // Check for existing assignment
        var existing = await GetAssignmentAsync(experimentId, userId);
        if (existing != null)
        {
            return existing;
        }

        // Get experiment
        var experiment = await _experimentService.GetExperimentAsync(experimentId)
            ?? throw new InvalidOperationException($"Experiment {experimentId} not found");

        // Select variant
        var variant = _trafficSplitService.SelectVariant(experiment, userId);

        // Store assignment
        var assignment = new ExperimentAssignment
        {
            ExperimentId = experimentId,
            VariantId = variant.Id,
            UserId = userId,
            AssignedAtUtc = DateTime.UtcNow
        };

        lock (_lock)
        {
            var key = GetAssignmentKey(userId);
            if (!_assignments.TryGetValue(key, out var userAssignments))
            {
                userAssignments = [];
                _assignments[key] = userAssignments;
            }
            userAssignments.Add(assignment);
        }

        _logger.LogInformation("Assigned user {UserId} to variant {VariantId} for experiment {ExperimentId}",
            userId, variant.Id, experimentId);

        return variant;
    }

    /// <inheritdoc />
    public Task<ExperimentVariant?> GetAssignmentAsync(Guid experimentId, string userId)
    {
        lock (_lock)
        {
            var key = GetAssignmentKey(userId);
            if (_assignments.TryGetValue(key, out var userAssignments))
            {
                var assignment = userAssignments.FirstOrDefault(a => a.ExperimentId == experimentId);
                if (assignment != null)
                {
                    // Return the variant from cache or fetch
                    return GetVariantFromAssignment(assignment);
                }
            }
        }
        return Task.FromResult<ExperimentVariant?>(null);
    }

    /// <inheritdoc />
    public Task<IEnumerable<ExperimentAssignment>> GetUserAssignmentsAsync(string userId)
    {
        lock (_lock)
        {
            var key = GetAssignmentKey(userId);
            if (_assignments.TryGetValue(key, out var userAssignments))
            {
                return Task.FromResult<IEnumerable<ExperimentAssignment>>(userAssignments.ToList());
            }
        }
        return Task.FromResult<IEnumerable<ExperimentAssignment>>([]);
    }

    /// <inheritdoc />
    public Task ClearAssignmentsAsync(string userId)
    {
        lock (_lock)
        {
            var key = GetAssignmentKey(userId);
            _assignments.Remove(key);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ForceAssignmentAsync(Guid experimentId, Guid variantId, string userId)
    {
        // Remove existing assignment if any
        lock (_lock)
        {
            var key = GetAssignmentKey(userId);
            if (_assignments.TryGetValue(key, out var userAssignments))
            {
                userAssignments.RemoveAll(a => a.ExperimentId == experimentId);
            }
        }

        // Add forced assignment
        var assignment = new ExperimentAssignment
        {
            ExperimentId = experimentId,
            VariantId = variantId,
            UserId = userId,
            AssignedAtUtc = DateTime.UtcNow,
            AssignmentSource = "forced"
        };

        lock (_lock)
        {
            var key = GetAssignmentKey(userId);
            if (!_assignments.TryGetValue(key, out var userAssignments))
            {
                userAssignments = [];
                _assignments[key] = userAssignments;
            }
            userAssignments.Add(assignment);
        }

        _logger.LogInformation("Force-assigned user {UserId} to variant {VariantId} for experiment {ExperimentId}",
            userId, variantId, experimentId);

        await Task.CompletedTask;
    }

    private static string GetAssignmentKey(string userId) => $"assignments:{userId}";

    private async Task<ExperimentVariant?> GetVariantFromAssignment(ExperimentAssignment assignment)
    {
        var experiment = await _experimentService.GetExperimentAsync(assignment.ExperimentId);
        return experiment?.Variants.FirstOrDefault(v => v.Id == assignment.VariantId);
    }
}
