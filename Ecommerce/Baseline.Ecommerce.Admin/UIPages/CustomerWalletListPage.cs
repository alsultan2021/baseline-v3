using CMS.Base;
using CMS.Membership;
using CMS.DataEngine;
using CMS.Commerce;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalCommerce.UIPages;

[assembly: UIPage(
    parentType: typeof(CustomerEditSection),
    slug: "wallet",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.CustomerWalletListPage),
    name: "Wallet",
    templateName: TemplateNames.LISTING,
    order: 400)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin listing page for customer wallets.
/// Shows wallet balances for a specific customer under their profile.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class CustomerWalletListPage : ListingPage
{
    private readonly IInfoProvider<WalletInfo> _walletProvider;
    private readonly IInfoProvider<MemberInfo> _memberProvider;
    private readonly IInfoProvider<CustomerInfo> _customerProvider;

    /// <summary>
    /// Customer ID from the URL path parameter.
    /// </summary>
    [PageParameter(typeof(IntPageModelBinder), typeof(CustomerEditSection))]
    public int CustomerId { get; set; }

    protected override string ObjectType => WalletInfo.OBJECT_TYPE;

    public CustomerWalletListPage(
        IInfoProvider<WalletInfo> walletProvider,
        IInfoProvider<MemberInfo> memberProvider,
        IInfoProvider<CustomerInfo> customerProvider)
    {
        _walletProvider = walletProvider;
        _memberProvider = memberProvider;
        _customerProvider = customerProvider;
    }

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

        // Get the member ID from the customer - CustomerID is NOT the same as MemberID
        // CustomerInfo has a CustomerMemberID property that links to the actual member
        var customer = await _customerProvider.GetAsync(CustomerId);

        if (customer == null)
        {
            PageConfiguration.Callouts.Add(new CalloutConfiguration
            {
                Headline = "Customer Not Found",
                Content = "The requested customer could not be found.",
                Type = CalloutType.QuickTip,
                Placement = CalloutPlacement.OnDesk
            });

            return;
        }

        var memberId = customer.CustomerMemberID;

        if (memberId <= 0)
        {
            PageConfiguration.Callouts.Add(new CalloutConfiguration
            {
                Headline = "No Member Association",
                Content = "This customer is not associated with a member account and cannot have wallets.",
                Type = CalloutType.QuickTip,
                Placement = CalloutPlacement.OnDesk
            });

            return;
        }

        // Filter wallets by member ID
        PageConfiguration.QueryModifiers.AddModifier((query, _) =>
            query.WhereEquals(nameof(WalletInfo.WalletMemberID), memberId));

        // Note: "Add wallet" header action removed because creating a wallet
        // for a customer should be done through the main Wallets listing
        // or programmatically when needed

        // Add edit row action to navigate to wallet section page
        // We pass the CustomerId parameter since CustomerWalletSectionPage
        // has a PageParameter referencing CustomerEditSection
        PageConfiguration.AddEditRowAction<CustomerWalletSectionPage>(
            parameters: new PageParameterValues
            {
                { typeof(CustomerEditSection), CustomerId }
            });

        // Configure columns
        var enabledStatusFormatter = new Func<object, IDataContainer, object>(EnabledStatusFormatter);
        var frozenStatusFormatter = new Func<object, IDataContainer, object>(FrozenStatusFormatter);
        var balanceFormatter = new Func<object, IDataContainer, string>(BalanceFormatter);

        PageConfiguration.ColumnConfigurations
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
