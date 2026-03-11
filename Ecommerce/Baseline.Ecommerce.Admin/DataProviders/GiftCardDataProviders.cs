using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Baseline.Ecommerce.Admin.DataProviders;

/// <summary>
/// Data provider for Gift Card Status dropdown.
/// </summary>
public class GiftCardStatusDataProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
        Task.FromResult<IEnumerable<DropDownOptionItem>>(
        [
            new() { Text = "Active", Value = GiftCardStatuses.Active },
            new() { Text = "Partially Redeemed", Value = GiftCardStatuses.PartiallyRedeemed },
            new() { Text = "Fully Redeemed", Value = GiftCardStatuses.FullyRedeemed },
            new() { Text = "Expired", Value = GiftCardStatuses.Expired },
            new() { Text = "Cancelled", Value = GiftCardStatuses.Cancelled }
        ]);
}

/// <summary>
/// Data provider for Gift Card Amount presets dropdown.
/// Common denominations for quick gift card creation.
/// </summary>
public class GiftCardAmountDataProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
        Task.FromResult<IEnumerable<DropDownOptionItem>>(
        [
            new() { Text = "$10", Value = "10" },
            new() { Text = "$25", Value = "25" },
            new() { Text = "$50", Value = "50" },
            new() { Text = "$75", Value = "75" },
            new() { Text = "$100", Value = "100" },
            new() { Text = "$150", Value = "150" },
            new() { Text = "$200", Value = "200" },
            new() { Text = "$250", Value = "250" },
            new() { Text = "$500", Value = "500" },
            new() { Text = "Custom", Value = "0" }
        ]);
}
