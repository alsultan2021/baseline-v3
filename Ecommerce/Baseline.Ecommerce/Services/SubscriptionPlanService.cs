using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using CMS.DataEngine;

namespace Baseline.Ecommerce.Services;

/// <summary>
/// Service for managing subscription plans.
/// </summary>
public class SubscriptionPlanService(
    IInfoProvider<SubscriptionPlanInfo> planProvider,
    IMemoryCache cache,
    ILogger<SubscriptionPlanService> logger) : ISubscriptionPlanService
{
    private const string CachePrefix = "subplan_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    /// <inheritdoc/>
    public async Task<IEnumerable<SubscriptionPlan>> GetActivePlansAsync(
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CachePrefix}active";

        if (cache.TryGetValue(cacheKey, out IEnumerable<SubscriptionPlan>? cached))
        {
            return cached!;
        }

        try
        {
            var infos = await planProvider
                .Get()
                .WhereTrue(nameof(SubscriptionPlanInfo.IsActive))
                .OrderBy(nameof(SubscriptionPlanInfo.TierLevel))
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            var plans = infos.Select(MapToSubscriptionPlan).ToList();
            cache.Set(cacheKey, plans, CacheDuration);
            return plans;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting active subscription plans");
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<SubscriptionPlan?> GetPlanByIdAsync(
        int planId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CachePrefix}id_{planId}";

        if (cache.TryGetValue(cacheKey, out SubscriptionPlan? cached))
        {
            return cached;
        }

        try
        {
            var results = await planProvider
                .Get()
                .WhereEquals(nameof(SubscriptionPlanInfo.SubscriptionPlanInfoID), planId)
                .TopN(1)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            var info = results.FirstOrDefault();
            if (info is null)
            {
                return null;
            }

            var plan = MapToSubscriptionPlan(info);
            cache.Set(cacheKey, plan, CacheDuration);
            return plan;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting subscription plan {PlanId}", planId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<SubscriptionPlan?> GetPlanByCodeAsync(
        string planCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(planCode))
        {
            return null;
        }

        var cacheKey = $"{CachePrefix}code_{planCode.ToLowerInvariant()}";

        if (cache.TryGetValue(cacheKey, out SubscriptionPlan? cached))
        {
            return cached;
        }

        try
        {
            var results = await planProvider
                .Get()
                .WhereEquals(nameof(SubscriptionPlanInfo.PlanCode), planCode)
                .TopN(1)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            var info = results.FirstOrDefault();
            if (info is null)
            {
                return null;
            }

            var plan = MapToSubscriptionPlan(info);
            cache.Set(cacheKey, plan, CacheDuration);
            return plan;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting subscription plan by code {PlanCode}", planCode);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SubscriptionPlan>> GetPlansByTierAsync(
        int tierLevel,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var infos = await planProvider
                .Get()
                .WhereTrue(nameof(SubscriptionPlanInfo.IsActive))
                .WhereEquals(nameof(SubscriptionPlanInfo.TierLevel), tierLevel)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            return infos.Select(MapToSubscriptionPlan);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting subscription plans for tier {TierLevel}", tierLevel);
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<SubscriptionPlan?> GetFeaturedPlanAsync(
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CachePrefix}featured";

        if (cache.TryGetValue(cacheKey, out SubscriptionPlan? cached))
        {
            return cached;
        }

        try
        {
            var results = await planProvider
                .Get()
                .WhereTrue(nameof(SubscriptionPlanInfo.IsActive))
                .WhereTrue(nameof(SubscriptionPlanInfo.IsFeatured))
                .TopN(1)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            var info = results.FirstOrDefault();
            if (info is null)
            {
                return null;
            }

            var plan = MapToSubscriptionPlan(info);
            cache.Set(cacheKey, plan, CacheDuration);
            return plan;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting featured subscription plan");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<PlanComparison> ComparePlansAsync(
        IEnumerable<int> planIds,
        CancellationToken cancellationToken = default)
    {
        var plans = new List<SubscriptionPlan>();

        foreach (var planId in planIds)
        {
            var plan = await GetPlanByIdAsync(planId, cancellationToken);
            if (plan is not null)
            {
                plans.Add(plan);
            }
        }

        // Get all unique feature names across plans
        var allFeatureNames = plans
            .SelectMany(p => p.Features)
            .Select(f => f.Name)
            .Distinct()
            .ToList();

        // Build feature comparison matrix
        var featureMatrix = new List<FeatureComparison>();
        foreach (var featureName in allFeatureNames)
        {
            var values = new Dictionary<int, FeatureValue>();
            foreach (var plan in plans)
            {
                var feature = plan.Features.FirstOrDefault(f => f.Name == featureName);
                values[plan.PlanId] = feature is not null
                    ? new FeatureValue
                    {
                        IsIncluded = feature.IsIncluded,
                        DisplayValue = feature.DisplayValue,
                        Limit = feature.Limit
                    }
                    : new FeatureValue { IsIncluded = false };
            }
            featureMatrix.Add(new FeatureComparison
            {
                FeatureName = featureName,
                PlanValues = values
            });
        }

        return new PlanComparison
        {
            Plans = plans,
            Features = featureMatrix
        };
    }

    private static SubscriptionPlan MapToSubscriptionPlan(SubscriptionPlanInfo info) =>
        new()
        {
            PlanId = info.SubscriptionPlanInfoID,
            PlanCode = info.PlanCode,
            Name = info.Name,
            Description = info.Description,
            Price = info.Price,
            Currency = info.Currency,
            BillingInterval = Enum.TryParse<BillingInterval>(info.BillingInterval, out var interval)
                ? interval
                : BillingInterval.Monthly,
            IntervalCount = info.IntervalCount,
            TrialPeriodDays = info.TrialPeriodDays,
            TierLevel = info.TierLevel,
            IsFeatured = info.IsFeatured,
            IsActive = info.IsActive,
            ExternalPlanId = info.ExternalPlanId,
            // Features would be loaded from a separate table in production
            Features = []
        };
}
