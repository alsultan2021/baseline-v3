using CMS.Commerce;
using CMS.ContactManagement;
using CMS.DataEngine;
using Baseline.Ecommerce.Installers;
using Microsoft.Extensions.Logging;

namespace Baseline.Ecommerce.Services;

/// <summary>
/// Syncs aggregated commerce metrics (total orders, total spent, last order date, average order value)
/// from order data onto <see cref="ContactInfo"/> custom fields for segmentation and automation.
/// </summary>
public interface ICommerceContactSyncService
{
    /// <summary>
    /// Syncs commerce metrics for a single contact by email.
    /// </summary>
    Task SyncContactAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs commerce metrics for all contacts that have at least one order.
    /// </summary>
    Task SyncAllContactsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation that queries <see cref="OrderInfo"/>/<see cref="OrderAddressInfo"/>
/// to compute per-email totals and writes them onto <see cref="ContactInfo"/> custom fields.
/// </summary>
public class CommerceContactSyncService(
    IInfoProvider<OrderInfo> orderProvider,
    IInfoProvider<OrderAddressInfo> addressProvider,
    IInfoProvider<ContactInfo> contactProvider,
    ILogger<CommerceContactSyncService> logger) : ICommerceContactSyncService
{
    /// <inheritdoc />
    public async Task SyncContactAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var metrics = await ComputeMetricsForEmailAsync(email);
        if (metrics is null)
        {
            return; // no orders — nothing to sync
        }

        var contact = (await contactProvider.Get()
            .WhereEquals(nameof(ContactInfo.ContactEmail), email)
            .TopN(1)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
            .FirstOrDefault();

        if (contact is null)
        {
            logger.LogDebug("CommerceContactSync: No contact found for {Email}", email);
            return;
        }

        ApplyMetrics(contact, metrics);
        contactProvider.Set(contact);

        logger.LogDebug("CommerceContactSync: Synced {Email} — {Orders} orders, {Spent:C} total",
            email, metrics.TotalOrders, metrics.TotalSpent);
    }

    /// <inheritdoc />
    public async Task SyncAllContactsAsync(CancellationToken cancellationToken = default)
    {
        // Get all distinct billing emails that have orders
        var billingAddresses = (await addressProvider.Get()
            .WhereEquals(nameof(OrderAddressInfo.OrderAddressType), "Billing")
            .WhereNotEmpty(nameof(OrderAddressInfo.OrderAddressEmail))
            .Columns(nameof(OrderAddressInfo.OrderAddressEmail))
            .Distinct()
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
            .ToList();

        var emails = billingAddresses
            .Select(a => a.OrderAddressEmail)
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        logger.LogInformation("CommerceContactSync: Syncing {Count} unique customer emails", emails.Count);

        int synced = 0;
        foreach (string email in emails)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var metrics = await ComputeMetricsForEmailAsync(email);
                if (metrics is null)
                {
                    continue;
                }

                var contact = (await contactProvider.Get()
                    .WhereEquals(nameof(ContactInfo.ContactEmail), email)
                    .TopN(1)
                    .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
                    .FirstOrDefault();

                if (contact is null)
                {
                    continue;
                }

                ApplyMetrics(contact, metrics);
                contactProvider.Set(contact);
                synced++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "CommerceContactSync: Failed to sync {Email}", email);
            }
        }

        logger.LogInformation("CommerceContactSync: Completed — {Synced}/{Total} contacts updated",
            synced, emails.Count);
    }

    private async Task<CommerceMetrics?> ComputeMetricsForEmailAsync(string email)
    {
        // Get order IDs for this email via billing address join
        var orders = (await orderProvider.Get()
            .Source(s => s.InnerJoin<OrderAddressInfo>(
                nameof(OrderInfo.OrderID),
                nameof(OrderAddressInfo.OrderAddressOrderID)))
            .WhereEquals(nameof(OrderAddressInfo.OrderAddressEmail), email)
            .WhereEquals(nameof(OrderAddressInfo.OrderAddressType), "Billing")
            .Columns(
                $"Commerce_Order.{nameof(OrderInfo.OrderID)}",
                nameof(OrderInfo.OrderGrandTotal),
                nameof(OrderInfo.OrderCreatedWhen))
            .GetEnumerableTypedResultAsync())
            .ToList();

        if (orders.Count == 0)
        {
            return null;
        }

        return new CommerceMetrics
        {
            TotalOrders = orders.Count,
            TotalSpent = orders.Sum(o => o.OrderGrandTotal),
            LastOrderDate = orders.Max(o => o.OrderCreatedWhen),
            AverageOrderValue = orders.Average(o => o.OrderGrandTotal)
        };
    }

    private static void ApplyMetrics(ContactInfo contact, CommerceMetrics metrics)
    {
        contact.SetValue(ContactCommerceFieldsInstaller.FIELD_TOTAL_ORDERS, metrics.TotalOrders);
        contact.SetValue(ContactCommerceFieldsInstaller.FIELD_TOTAL_SPENT, metrics.TotalSpent);
        contact.SetValue(ContactCommerceFieldsInstaller.FIELD_LAST_ORDER_DATE, metrics.LastOrderDate);
        contact.SetValue(ContactCommerceFieldsInstaller.FIELD_AVERAGE_ORDER_VALUE, metrics.AverageOrderValue);
        contact.SetValue(ContactCommerceFieldsInstaller.FIELD_LAST_SYNCED_AT, DateTime.UtcNow);
    }

    private sealed class CommerceMetrics
    {
        public int TotalOrders { get; init; }
        public decimal TotalSpent { get; init; }
        public DateTime LastOrderDate { get; init; }
        public decimal AverageOrderValue { get; init; }
    }
}
