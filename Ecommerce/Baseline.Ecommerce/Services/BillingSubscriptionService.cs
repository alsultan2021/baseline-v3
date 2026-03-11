using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using CMS.DataEngine;

namespace Baseline.Ecommerce.Services;

/// <summary>
/// Service for managing customer billing subscriptions (SaaS).
/// Optionally integrates with an <see cref="ISubscriptionPaymentProvider"/> (Stripe, PayPal, etc.).
/// When no provider is registered the service manages local DB records only.
/// </summary>
public class BillingSubscriptionService(
    IInfoProvider<CustomerSubscriptionInfo> subscriptionProvider,
    IInfoProvider<SubscriptionPlanInfo> planProvider,
    IMemoryCache cache,
    ILogger<BillingSubscriptionService> logger,
    ISubscriptionPaymentProvider? paymentProvider = null) : IBillingSubscriptionService
{
    private const string CachePrefix = "subscription_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    /// <inheritdoc/>
    public async Task<CustomerSubscription?> GetCurrentSubscriptionAsync(CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would get the current user's subscription
        // For now, return null as we don't have user context
        await Task.Delay(1, cancellationToken);
        logger.LogDebug("GetCurrentSubscriptionAsync requires user context");
        return null;
    }

    /// <inheritdoc/>
    public async Task<CustomerSubscription?> GetSubscriptionByIdAsync(
        int subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CachePrefix}id_{subscriptionId}";

        if (cache.TryGetValue(cacheKey, out CustomerSubscription? cached))
        {
            return cached;
        }

        try
        {
            var results = await subscriptionProvider
                .Get()
                .WhereEquals(nameof(CustomerSubscriptionInfo.CustomerSubscriptionInfoID), subscriptionId)
                .TopN(1)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            var info = results.FirstOrDefault();
            if (info is null)
            {
                return null;
            }

            var subscription = MapToCustomerSubscription(info);

            // Load plan details
            if (subscription.PlanId > 0)
            {
                var plan = await GetPlanByIdAsync(subscription.PlanId, cancellationToken);
                subscription = subscription with { Plan = plan };
            }

            cache.Set(cacheKey, subscription, CacheDuration);
            return subscription;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting subscription {SubscriptionId}", subscriptionId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CustomerSubscription>> GetCustomerSubscriptionsAsync(
        int customerId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = subscriptionProvider
                .Get()
                .WhereEquals(nameof(CustomerSubscriptionInfo.CustomerId), customerId);

            if (!includeInactive)
            {
                query = query.WhereIn(nameof(CustomerSubscriptionInfo.Status), [
                    nameof(UserSubscriptionState.Active),
                    nameof(UserSubscriptionState.Trialing),
                    nameof(UserSubscriptionState.PastDue),
                    nameof(UserSubscriptionState.Paused)
                ]);
            }

            var infos = await query.GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
            var subscriptions = new List<CustomerSubscription>();

            foreach (var info in infos)
            {
                var subscription = MapToCustomerSubscription(info);

                // Load plan details
                if (subscription.PlanId > 0)
                {
                    var plan = await GetPlanByIdAsync(subscription.PlanId, cancellationToken);
                    subscription = subscription with { Plan = plan };
                }

                subscriptions.Add(subscription);
            }

            return subscriptions;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting subscriptions for customer {CustomerId}", customerId);
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<CustomerSubscription?> GetSubscriptionByExternalIdAsync(
        string externalSubscriptionId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CachePrefix}ext_{externalSubscriptionId}";

        if (cache.TryGetValue(cacheKey, out CustomerSubscription? cached))
        {
            return cached;
        }

        try
        {
            var results = await subscriptionProvider
                .Get()
                .WhereEquals(nameof(CustomerSubscriptionInfo.ExternalSubscriptionId), externalSubscriptionId)
                .TopN(1)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            var info = results.FirstOrDefault();
            if (info is null)
            {
                return null;
            }

            var subscription = MapToCustomerSubscription(info);

            // Load plan details
            if (subscription.PlanId > 0)
            {
                var plan = await GetPlanByIdAsync(subscription.PlanId, cancellationToken);
                subscription = subscription with { Plan = plan };
            }

            cache.Set(cacheKey, subscription, CacheDuration);
            return subscription;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting subscription by external ID {ExternalId}", externalSubscriptionId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<CreateSubscriptionResult> CreateSubscriptionAsync(
        CreateSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "Creating subscription for customer {CustomerId} with plan {PlanId}",
                request.CustomerId,
                request.PlanId);

            // Get the plan
            var plan = await GetPlanByIdAsync(request.PlanId, cancellationToken);
            if (plan is null)
            {
                return CreateSubscriptionResult.Failed("Invalid plan ID");
            }

            var now = DateTimeOffset.UtcNow;
            var trialDays = request.TrialDays ?? plan.TrialPeriodDays;
            var status = trialDays > 0 ? UserSubscriptionState.Trialing : UserSubscriptionState.Active;
            var trialEnd = trialDays > 0 ? now.AddDays(trialDays) : (DateTimeOffset?)null;

            // Calculate current period end based on billing interval
            var periodEnd = CalculatePeriodEnd(now, plan.BillingInterval, plan.IntervalCount);

            var info = new CustomerSubscriptionInfo
            {
                CustomerId = request.CustomerId,
                PlanId = request.PlanId,
                Status = status.ToString(),
                StartDate = now.DateTime,
                CurrentPeriodEnd = periodEnd.DateTime,
                TrialEnd = trialEnd?.DateTime,
                CouponCode = request.CouponCode,
                ExternalSubscriptionId = null, // Set by payment provider
                CreatedOn = now.DateTime
            };

            // Integrate with payment provider when available
            if (paymentProvider is { IsEnabled: true } && plan.ExternalPlanId is not null)
            {
                var providerResult = await paymentProvider.CreateSubscriptionAsync(
                    new ProviderSubscriptionRequest
                    {
                        ExternalCustomerId = request.ExternalCustomerId ?? "",
                        ExternalPlanId = plan.ExternalPlanId,
                        PaymentMethodId = request.PaymentMethodId,
                        CouponId = request.CouponCode,
                        TrialDays = trialDays > 0 ? trialDays : null,
                        Metadata = new Dictionary<string, string>
                        {
                            ["customerId"] = request.CustomerId.ToString(),
                            ["planId"] = request.PlanId.ToString()
                        }
                    }, cancellationToken);

                if (!providerResult.Success)
                {
                    if (providerResult.RequiresAction)
                    {
                        logger.LogInformation(
                            "Subscription creation requires payment action for customer {CustomerId}",
                            request.CustomerId);
                    }

                    return CreateSubscriptionResult.Failed(
                        providerResult.ErrorMessage ?? "Payment provider error");
                }

                info.ExternalSubscriptionId = providerResult.ExternalSubscriptionId;
            }

            await subscriptionProvider.SetAsync(info, cancellationToken);

            var subscription = MapToCustomerSubscription(info) with { Plan = plan };

            logger.LogInformation(
                "Created subscription {SubscriptionId} for customer {CustomerId}",
                info.CustomerSubscriptionInfoID,
                request.CustomerId);

            return CreateSubscriptionResult.Succeeded(subscription);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating subscription for customer {CustomerId}", request.CustomerId);
            return CreateSubscriptionResult.Failed($"Failed to create subscription: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ChangePlanResult> ChangePlanAsync(
        ChangePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await subscriptionProvider
                .Get()
                .WhereEquals(nameof(CustomerSubscriptionInfo.CustomerSubscriptionInfoID), request.SubscriptionId)
                .TopN(1)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            var info = results.FirstOrDefault();
            if (info is null)
            {
                return ChangePlanResult.Failed("Subscription not found");
            }

            var newPlan = await GetPlanByIdAsync(request.NewPlanId, cancellationToken);
            if (newPlan is null)
            {
                return ChangePlanResult.Failed("Invalid plan ID");
            }

            var oldPlanId = info.PlanId;

            // Calculate proration if needed
            decimal? prorationAmount = null;
            if (request.Prorate)
            {
                var oldPlan = await GetPlanByIdAsync(oldPlanId, cancellationToken);
                if (oldPlan is not null)
                {
                    prorationAmount = CalculateProration(
                        oldPlan.Price,
                        newPlan.Price,
                        info.CurrentPeriodEnd,
                        info.StartDate);
                }
            }

            // Update subscription
            info.PlanId = request.NewPlanId;
            info.ModifiedOn = DateTime.UtcNow;

            // Sync plan change to payment provider
            if (paymentProvider is { IsEnabled: true }
                && !string.IsNullOrEmpty(info.ExternalSubscriptionId)
                && newPlan.ExternalPlanId is not null)
            {
                var providerResult = await paymentProvider.UpdateSubscriptionAsync(
                    info.ExternalSubscriptionId,
                    new ProviderSubscriptionUpdateRequest
                    {
                        NewExternalPlanId = newPlan.ExternalPlanId,
                        Prorate = request.Prorate
                    }, cancellationToken);

                if (!providerResult.Success)
                {
                    return ChangePlanResult.Failed(
                        providerResult.ErrorMessage ?? "Payment provider error");
                }
            }

            await subscriptionProvider.SetAsync(info, cancellationToken);

            // Invalidate cache
            ClearSubscriptionCache(request.SubscriptionId);

            var subscription = MapToCustomerSubscription(info) with { Plan = newPlan };

            logger.LogInformation(
                "Changed plan for subscription {SubscriptionId} from {OldPlanId} to {NewPlanId}",
                request.SubscriptionId,
                oldPlanId,
                request.NewPlanId);

            return ChangePlanResult.Succeeded(subscription, prorationAmount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error changing plan for subscription {SubscriptionId}", request.SubscriptionId);
            return ChangePlanResult.Failed($"Failed to change plan: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<CancelSubscriptionResult> CancelSubscriptionAsync(
        CancelSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await subscriptionProvider
                .Get()
                .WhereEquals(nameof(CustomerSubscriptionInfo.CustomerSubscriptionInfoID), request.SubscriptionId)
                .TopN(1)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            var info = results.FirstOrDefault();
            if (info is null)
            {
                return CancelSubscriptionResult.Failed("Subscription not found");
            }

            var now = DateTime.UtcNow;
            DateTimeOffset effectiveDate;

            if (request.CancelImmediately)
            {
                info.Status = nameof(UserSubscriptionState.Cancelled);
                info.CancelledAt = now;
                info.CancelAt = now;
                effectiveDate = now;
            }
            else
            {
                // Cancel at period end
                info.CancelAtPeriodEnd = true;
                info.CancelledAt = now;
                info.CancelAt = info.CurrentPeriodEnd;
                effectiveDate = info.CurrentPeriodEnd;
            }

            info.CancellationReason = request.Reason;
            info.ModifiedOn = now;

            // Cancel via payment provider
            if (paymentProvider is { IsEnabled: true }
                && !string.IsNullOrEmpty(info.ExternalSubscriptionId))
            {
                var cancelled = await paymentProvider.CancelSubscriptionAsync(
                    info.ExternalSubscriptionId,
                    request.CancelImmediately,
                    cancellationToken);

                if (!cancelled)
                {
                    return CancelSubscriptionResult.Failed("Payment provider cancellation failed");
                }
            }

            await subscriptionProvider.SetAsync(info, cancellationToken);

            // Invalidate cache
            ClearSubscriptionCache(request.SubscriptionId);

            var subscription = MapToCustomerSubscription(info);

            logger.LogInformation(
                "Cancelled subscription {SubscriptionId}, effective {EffectiveDate}",
                request.SubscriptionId,
                effectiveDate);

            return CancelSubscriptionResult.Succeeded(subscription, effectiveDate);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling subscription {SubscriptionId}", request.SubscriptionId);
            return CancelSubscriptionResult.Failed($"Failed to cancel subscription: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<CreateSubscriptionResult> ReactivateSubscriptionAsync(
        int subscriptionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await subscriptionProvider
                .Get()
                .WhereEquals(nameof(CustomerSubscriptionInfo.CustomerSubscriptionInfoID), subscriptionId)
                .TopN(1)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            var info = results.FirstOrDefault();
            if (info is null)
            {
                return CreateSubscriptionResult.Failed("Subscription not found");
            }

            if (info.Status != nameof(UserSubscriptionState.Cancelled) && !info.CancelAtPeriodEnd)
            {
                return CreateSubscriptionResult.Failed("Subscription is not cancelled");
            }

            // Reactivate
            info.Status = nameof(UserSubscriptionState.Active);
            info.CancelAtPeriodEnd = false;
            info.CancelledAt = null;
            info.CancelAt = null;
            info.CancellationReason = null;
            info.ModifiedOn = DateTime.UtcNow;

            // Reactivate via payment provider (remove cancel_at_period_end flag)
            if (paymentProvider is { IsEnabled: true }
                && !string.IsNullOrEmpty(info.ExternalSubscriptionId))
            {
                var providerResult = await paymentProvider.UpdateSubscriptionAsync(
                    info.ExternalSubscriptionId,
                    new ProviderSubscriptionUpdateRequest
                    {
                        CancelAtPeriodEnd = false
                    }, cancellationToken);

                if (!providerResult.Success)
                {
                    return CreateSubscriptionResult.Failed(
                        providerResult.ErrorMessage ?? "Payment provider reactivation failed");
                }
            }

            await subscriptionProvider.SetAsync(info, cancellationToken);

            // Invalidate cache
            ClearSubscriptionCache(subscriptionId);

            var plan = await GetPlanByIdAsync(info.PlanId, cancellationToken);
            var subscription = MapToCustomerSubscription(info) with { Plan = plan };

            logger.LogInformation("Reactivated subscription {SubscriptionId}", subscriptionId);

            return CreateSubscriptionResult.Succeeded(subscription);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reactivating subscription {SubscriptionId}", subscriptionId);
            return CreateSubscriptionResult.Failed($"Failed to reactivate subscription: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<CustomerSubscription?> PauseSubscriptionAsync(
        PauseSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await subscriptionProvider
                .Get()
                .WhereEquals(nameof(CustomerSubscriptionInfo.CustomerSubscriptionInfoID), request.SubscriptionId)
                .TopN(1)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            var info = results.FirstOrDefault();
            if (info is null)
            {
                return null;
            }

            info.Status = nameof(UserSubscriptionState.Paused);
            info.PausedAt = DateTime.UtcNow;
            info.ResumeAt = request.ResumeAt?.DateTime;
            info.PauseReason = request.Reason;
            info.ModifiedOn = DateTime.UtcNow;

            // Pause via payment provider (Stripe uses pause_collection)
            if (paymentProvider is { IsEnabled: true }
                && !string.IsNullOrEmpty(info.ExternalSubscriptionId))
            {
                var providerResult = await paymentProvider.UpdateSubscriptionAsync(
                    info.ExternalSubscriptionId,
                    new ProviderSubscriptionUpdateRequest
                    {
                        Metadata = new Dictionary<string, string>
                        {
                            ["paused"] = "true",
                            ["pauseReason"] = request.Reason ?? "",
                            ["resumeAt"] = request.ResumeAt?.ToString("o") ?? ""
                        }
                    }, cancellationToken);

                if (!providerResult.Success)
                {
                    logger.LogWarning(
                        "Payment provider pause failed for subscription {SubscriptionId}: {Error}",
                        request.SubscriptionId,
                        providerResult.ErrorMessage);
                }
            }

            await subscriptionProvider.SetAsync(info, cancellationToken);

            // Invalidate cache
            ClearSubscriptionCache(request.SubscriptionId);

            var plan = await GetPlanByIdAsync(info.PlanId, cancellationToken);
            var subscription = MapToCustomerSubscription(info) with { Plan = plan };

            logger.LogInformation("Paused subscription {SubscriptionId}", request.SubscriptionId);

            return subscription;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error pausing subscription {SubscriptionId}", request.SubscriptionId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<CustomerSubscription?> ResumeSubscriptionAsync(
        int subscriptionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await subscriptionProvider
                .Get()
                .WhereEquals(nameof(CustomerSubscriptionInfo.CustomerSubscriptionInfoID), subscriptionId)
                .TopN(1)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            var info = results.FirstOrDefault();
            if (info is null || info.Status != nameof(UserSubscriptionState.Paused))
            {
                return null;
            }

            info.Status = nameof(UserSubscriptionState.Active);
            info.PausedAt = null;
            info.ResumeAt = null;
            info.PauseReason = null;
            info.ModifiedOn = DateTime.UtcNow;

            // Resume via payment provider
            if (paymentProvider is { IsEnabled: true }
                && !string.IsNullOrEmpty(info.ExternalSubscriptionId))
            {
                var providerResult = await paymentProvider.UpdateSubscriptionAsync(
                    info.ExternalSubscriptionId,
                    new ProviderSubscriptionUpdateRequest
                    {
                        Metadata = new Dictionary<string, string>
                        {
                            ["paused"] = "false"
                        }
                    }, cancellationToken);

                if (!providerResult.Success)
                {
                    logger.LogWarning(
                        "Payment provider resume failed for subscription {SubscriptionId}: {Error}",
                        subscriptionId,
                        providerResult.ErrorMessage);
                }
            }

            await subscriptionProvider.SetAsync(info, cancellationToken);

            // Invalidate cache
            ClearSubscriptionCache(subscriptionId);

            var plan = await GetPlanByIdAsync(info.PlanId, cancellationToken);
            var subscription = MapToCustomerSubscription(info) with { Plan = plan };

            logger.LogInformation("Resumed subscription {SubscriptionId}", subscriptionId);

            return subscription;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resuming subscription {SubscriptionId}", subscriptionId);
            return null;
        }
    }

    #region Private Helpers

    private async Task<SubscriptionPlan?> GetPlanByIdAsync(int planId, CancellationToken cancellationToken)
    {
        var cacheKey = $"plan_{planId}";

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
            cache.Set(cacheKey, plan, TimeSpan.FromMinutes(30));
            return plan;
        }
        catch
        {
            return null;
        }
    }

    private static CustomerSubscription MapToCustomerSubscription(CustomerSubscriptionInfo info) =>
        new()
        {
            SubscriptionId = info.CustomerSubscriptionInfoID,
            CustomerId = info.CustomerId,
            PlanId = info.PlanId,
            Status = Enum.TryParse<UserSubscriptionState>(info.Status, out var status)
                ? status
                : UserSubscriptionState.Active,
            StartDate = info.StartDate,
            CurrentPeriodEnd = info.CurrentPeriodEnd,
            TrialEnd = info.TrialEnd,
            CancelledAt = info.CancelledAt,
            CancelAt = info.CancelAt,
            CancelAtPeriodEnd = info.CancelAtPeriodEnd,
            ExternalSubscriptionId = info.ExternalSubscriptionId,
            CouponCode = info.CouponCode
        };

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
            ExternalPlanId = info.ExternalPlanId
        };

    private static DateTimeOffset CalculatePeriodEnd(DateTimeOffset start, BillingInterval interval, int count) =>
        interval switch
        {
            BillingInterval.Daily => start.AddDays(count),
            BillingInterval.Weekly => start.AddDays(7 * count),
            BillingInterval.Monthly => start.AddMonths(count),
            BillingInterval.Quarterly => start.AddMonths(3 * count),
            BillingInterval.Yearly => start.AddYears(count),
            _ => start.AddMonths(count)
        };

    private static decimal CalculateProration(
        decimal oldPrice,
        decimal newPrice,
        DateTime periodEnd,
        DateTime startDate)
    {
        var now = DateTime.UtcNow;
        var totalDays = (periodEnd - startDate).TotalDays;
        var remainingDays = (periodEnd - now).TotalDays;

        if (totalDays <= 0 || remainingDays <= 0)
        {
            return 0;
        }

        var unusedRatio = remainingDays / totalDays;
        var oldRemaining = oldPrice * (decimal)unusedRatio;
        var newRemaining = newPrice * (decimal)unusedRatio;

        return newRemaining - oldRemaining;
    }

    private void ClearSubscriptionCache(int subscriptionId)
    {
        cache.Remove($"{CachePrefix}id_{subscriptionId}");
    }

    #endregion
}
