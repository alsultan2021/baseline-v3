using CMS.Base;
using CMS.Membership;
using CMS.DataEngine;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalCommerce.UIPages;

[assembly: UIPage(
    parentType: typeof(CommerceConfigurationApplication),
    slug: "wallets",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.WalletListPage),
    name: "Wallets",
    templateName: TemplateNames.LISTING,
    order: 400,
    Icon = Icons.DollarSign)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin listing page for member Wallets.
/// Provides management of store credit, loyalty points, prepaid funds, and gift cards.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class WalletListPage : ListingPage
{
    protected override string ObjectType => WalletInfo.OBJECT_TYPE;

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        // Initialize callouts collection if null
        PageConfiguration.Callouts ??= [];

        // Check if the data class exists
        var dataClassExists = DataClassInfoProvider.GetDataClassInfo("Baseline.Wallet") != null;

        if (!dataClassExists)
        {
            PageConfiguration.Callouts.Add(new CalloutConfiguration
            {
                Headline = "Wallets Not Yet Available",
                Content = "The Wallet data class has not been installed yet. Please restart the application to complete the installation.",
                Type = CalloutType.QuickTip,
                Placement = CalloutPlacement.OnDesk
            });

            return;
        }

        PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
        PageConfiguration.HeaderActions.AddLink<WalletCreatePage>("Add wallet");
        PageConfiguration.AddEditRowAction<WalletEditPage>();

        var enabledStatusFormatter = new Func<object, IDataContainer, object>(EnabledStatusFormatter);
        var frozenStatusFormatter = new Func<object, IDataContainer, object>(FrozenStatusFormatter);
        var balanceFormatter = new Func<object, IDataContainer, string>(BalanceFormatter);

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(WalletInfo.WalletMemberID), "Member ID", searchable: true, maxWidth: 10)
            .AddColumn(nameof(WalletInfo.WalletType), "Type", searchable: true, maxWidth: 15)
            .AddColumn(nameof(WalletInfo.WalletBalance), "Balance", maxWidth: 12, formatter: balanceFormatter)
            .AddColumn(nameof(WalletInfo.WalletHeldBalance), "Held", maxWidth: 10, formatter: balanceFormatter)
            .AddComponentColumn(nameof(WalletInfo.WalletEnabled), NamedComponentCellComponentNames.SIMPLE_STATUS_COMPONENT, "Enabled", maxWidth: 10, modelRetriever: enabledStatusFormatter)
            .AddComponentColumn(nameof(WalletInfo.WalletFrozen), NamedComponentCellComponentNames.SIMPLE_STATUS_COMPONENT, "Frozen", maxWidth: 10, modelRetriever: frozenStatusFormatter)
            .AddColumn(nameof(WalletInfo.WalletLastModified), "Last Modified", maxWidth: 15);
    }

    /// <summary>
    /// Formats the enabled status as a visual component with checkmark or X icon.
    /// </summary>
    private static object EnabledStatusFormatter(object formattedValue, IDataContainer allValues)
    {
        var isEnabled = formattedValue is bool b && b;
        var (color, label, icon) = isEnabled
            ? (Color.SuccessText, "Enabled", Icons.CheckCircle)
            : (Color.AlertText, "Disabled", Icons.BanSign);

        return new SimpleStatusNamedComponentCellProps
        {
            IconName = icon,
            Label = label,
            IconColor = color,
            LabelColor = color
        };
    }

    /// <summary>
    /// Formats the frozen status as a visual component.
    /// </summary>
    private static object FrozenStatusFormatter(object formattedValue, IDataContainer allValues)
    {
        var isFrozen = formattedValue is bool b && b;
        var (color, label, icon) = isFrozen
            ? (Color.AlertText, "Frozen", Icons.BanSign)
            : (Color.SuccessText, "No", Icons.CheckCircle);

        return new SimpleStatusNamedComponentCellProps
        {
            IconName = icon,
            Label = label,
            IconColor = color,
            LabelColor = color
        };
    }

    /// <summary>
    /// Formats balance values to show 2 decimal places.
    /// </summary>
    private static string BalanceFormatter(object formattedValue, IDataContainer allValues)
    {
        if (formattedValue is decimal d)
        {
            return d.ToString("N2");
        }
        return formattedValue?.ToString() ?? "0.00";
    }
}
