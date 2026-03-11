using CMS.Commerce;
using CMS.DataEngine;
using CMS.Globalization;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Baseline.Ecommerce.Admin.DataProviders;

/// <summary>
/// Data provider for country dropdown options.
/// </summary>
public class CountryDataProvider : IDropDownOptionsProvider
{
    public async Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var countries = await CountryInfo.Provider.Get()
            .OrderBy(nameof(CountryInfo.CountryDisplayName))
            .GetEnumerableTypedResultAsync();

        var options = new List<DropDownOptionItem>
        {
            new() { Value = "", Text = "Select a country" }
        };

        options.AddRange(countries.Select(country => new DropDownOptionItem
        {
            Value = country.CountryID.ToString(),
            Text = country.CountryDisplayName
        }));

        return options;
    }
}

/// <summary>
/// Data provider for shipping method dropdown options.
/// </summary>
public class ShippingMethodDataProvider : IDropDownOptionsProvider
{
    public async Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var methods = await ShippingMethodInfo.Provider.Get()
            .WhereTrue(nameof(ShippingMethodInfo.ShippingMethodEnabled))
            .OrderBy(nameof(ShippingMethodInfo.ShippingMethodDisplayName))
            .GetEnumerableTypedResultAsync();

        var options = new List<DropDownOptionItem>
        {
            new() { Value = "0", Text = "No shipping (digital delivery)" }
        };

        options.AddRange(methods.Select(method => new DropDownOptionItem
        {
            Value = method.ShippingMethodID.ToString(),
            Text = $"{method.ShippingMethodDisplayName} ({method.ShippingMethodPrice:C2})"
        }));

        return options;
    }
}

/// <summary>
/// Data provider for payment method dropdown options.
/// </summary>
public class PaymentMethodDataProvider : IDropDownOptionsProvider
{
    public async Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var methods = await PaymentMethodInfo.Provider.Get()
            .WhereTrue(nameof(PaymentMethodInfo.PaymentMethodEnabled))
            .OrderBy(nameof(PaymentMethodInfo.PaymentMethodDisplayName))
            .GetEnumerableTypedResultAsync();

        var options = new List<DropDownOptionItem>
        {
            new() { Value = "", Text = "Select a payment method" }
        };

        options.AddRange(methods.Select(method => new DropDownOptionItem
        {
            Value = method.PaymentMethodID.ToString(),
            Text = method.PaymentMethodDisplayName
        }));

        return options;
    }
}

/// <summary>
/// Data provider for order status dropdown options.
/// </summary>
public class OrderStatusDataProvider : IDropDownOptionsProvider
{
    public async Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var statuses = await OrderStatusInfo.Provider.Get()
            .OrderBy(nameof(OrderStatusInfo.OrderStatusOrder))
            .GetEnumerableTypedResultAsync();

        var options = new List<DropDownOptionItem>
        {
            new() { Value = "", Text = "Select an order status" }
        };

        options.AddRange(statuses.Select(status => new DropDownOptionItem
        {
            Value = status.OrderStatusID.ToString(),
            Text = status.OrderStatusDisplayName
        }));

        return options;
    }
}
