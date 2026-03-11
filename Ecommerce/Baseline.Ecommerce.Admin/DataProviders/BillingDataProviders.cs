using Baseline.Ecommerce;
using Baseline.Ecommerce.Models;

using CMS.DataEngine;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Baseline.Ecommerce.Admin.DataProviders;

/// <summary>
/// Data provider for billing interval dropdown.
/// </summary>
public class BillingIntervalDataProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
        Task.FromResult<IEnumerable<DropDownOptionItem>>(
        [
            new() { Text = "Daily", Value = "Daily" },
            new() { Text = "Weekly", Value = "Weekly" },
            new() { Text = "Monthly", Value = "Monthly" },
            new() { Text = "Quarterly", Value = "Quarterly" },
            new() { Text = "Yearly", Value = "Yearly" }
        ]);
}

/// <summary>
/// Data provider for subscription status dropdown.
/// </summary>
public class SubscriptionStatusDataProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
        Task.FromResult<IEnumerable<DropDownOptionItem>>(
        [
            new() { Text = "Active", Value = "Active" },
            new() { Text = "Trialing", Value = "Trialing" },
            new() { Text = "PastDue", Value = "PastDue" },
            new() { Text = "Cancelled", Value = "Cancelled" },
            new() { Text = "Expired", Value = "Expired" },
            new() { Text = "Paused", Value = "Paused" }
        ]);
}

/// <summary>
/// Data provider for subscription plan selection dropdown.
/// </summary>
public class SubscriptionPlanDataProvider : IDropDownOptionsProvider
{
    public async Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var plans = await Provider<SubscriptionPlanInfo>.Instance
            .Get()
            .WhereEquals(nameof(SubscriptionPlanInfo.IsActive), true)
            .OrderBy(nameof(SubscriptionPlanInfo.Name))
            .GetEnumerableTypedResultAsync();

        return plans.Select(p => new DropDownOptionItem
        {
            Text = $"{p.Name} ({p.Price:C}/{p.BillingInterval})",
            Value = p.SubscriptionPlanInfoID.ToString()
        });
    }
}

/// <summary>
/// Data provider for currency dropdown using Baseline CurrencyInfo.
/// </summary>
public class CurrencyDataProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var currencies = Provider<CurrencyInfo>.Instance.Get()
            .WhereEquals(nameof(CurrencyInfo.CurrencyEnabled), true)
            .OrderBy(nameof(CurrencyInfo.CurrencyDisplayName))
            .ToList();

        if (currencies.Count == 0)
        {
            return Task.FromResult<IEnumerable<DropDownOptionItem>>(
            [
                new() { Value = "USD", Text = "USD - US Dollar" },
                new() { Value = "CAD", Text = "CAD - Canadian Dollar" }
            ]);
        }

        var items = currencies.Select(c => new DropDownOptionItem
        {
            Value = c.CurrencyCode,
            Text = $"{c.CurrencyCode} - {c.CurrencyDisplayName}"
        });

        return Task.FromResult(items);
    }
}

/// <summary>
/// Data provider for external plan/price IDs from payment providers.
/// </summary>
public class ExternalPlanIdDataProvider(
    IEnumerable<ISubscriptionPaymentProvider> providers) : IDropDownOptionsProvider
{
    public async Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var items = new List<DropDownOptionItem>
        {
            new() { Value = "", Text = "(none)" }
        };

        foreach (var provider in providers.Where(p => p.IsEnabled))
        {
            try
            {
                var plans = await provider.ListExternalPlansAsync();
                items.AddRange(plans.Where(p => p.IsActive).Select(p => new DropDownOptionItem
                {
                    Value = p.ExternalPlanId,
                    Text = $"{p.DisplayName} ({p.ExternalPlanId})"
                }));
            }
            catch
            {
                // Provider may not be configured yet
            }
        }

        return items;
    }
}
