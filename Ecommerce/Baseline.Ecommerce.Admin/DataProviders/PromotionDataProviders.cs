using Baseline.Ecommerce.Models;
using CMS.DataEngine;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Baseline.Ecommerce.Admin.ViewModels;

/// <summary>
/// Data provider for Promotion selection dropdown.
/// </summary>
public class PromotionDataProvider : IDropDownOptionsProvider
{
    public async Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var promotions = await Provider<PromotionInfo>.Instance
            .Get()
            .WhereEquals(nameof(PromotionInfo.PromotionEnabled), true)
            .OrderBy(nameof(PromotionInfo.PromotionDisplayName))
            .GetEnumerableTypedResultAsync();

        return promotions.Select(p => new DropDownOptionItem
        {
            Text = p.PromotionDisplayName,
            Value = p.PromotionID.ToString()
        });
    }
}

/// <summary>
/// Data provider for Promotion Type dropdown.
/// </summary>
public class PromotionTypeDataProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
        Task.FromResult<IEnumerable<DropDownOptionItem>>(
        [
            new() { Text = "Catalog (Product-level)", Value = "0" },
            new() { Text = "Order (Order-level)", Value = "1" },
            new() { Text = "Shipping", Value = "2" },
            new() { Text = "Buy X Get Y", Value = "3" }
        ]);
}

/// <summary>
/// Data provider for Shipping Discount Type dropdown.
/// </summary>
public class ShippingDiscountTypeDataProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
        Task.FromResult<IEnumerable<DropDownOptionItem>>(
        [
            new() { Text = "Free Shipping", Value = "0" },
            new() { Text = "Reduced Rate (%)", Value = "1" },
            new() { Text = "Flat Rate ($)", Value = "2" }
        ]);
}

/// <summary>
/// Data provider for Discount Type dropdown.
/// </summary>
public class DiscountTypeDataProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
        Task.FromResult<IEnumerable<DropDownOptionItem>>(
        [
            new() { Text = "Percentage (%)", Value = "0" },
            new() { Text = "Fixed Amount ($)", Value = "1" }
        ]);
}

/// <summary>
/// Data provider for Minimum Requirement Type dropdown.
/// </summary>
public class MinimumRequirementTypeDataProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
        Task.FromResult<IEnumerable<DropDownOptionItem>>(
        [
            new() { Text = "No Minimum", Value = "0" },
            new() { Text = "Minimum Purchase Amount", Value = "1" },
            new() { Text = "Minimum Quantity of Items", Value = "2" }
        ]);
}

/// <summary>
/// Data provider for Coupon Type dropdown.
/// </summary>
public class CouponTypeDataProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
        Task.FromResult<IEnumerable<DropDownOptionItem>>(
        [
            new() { Text = "Single Use (one per customer)", Value = "0" },
            new() { Text = "Multi-Use (limited total)", Value = "1" },
            new() { Text = "Unlimited", Value = "2" }
        ]);
}
