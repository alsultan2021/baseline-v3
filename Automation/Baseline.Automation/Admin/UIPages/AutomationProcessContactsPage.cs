using CMS.Base;
using CMS.DataEngine;
using CMS.Membership;

using Baseline.Automation.Models;

using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(Baseline.Automation.Admin.UIPages.AutomationProcessSectionPage),
    slug: "automation-contacts",
    uiPageType: typeof(Baseline.Automation.Admin.UIPages.AutomationProcessContactsPage),
    name: "Contacts",
    templateName: TemplateNames.LISTING,
    order: 300,
    Icon = Icons.PersonalisationVariants)]

namespace Baseline.Automation.Admin.UIPages;

/// <summary>
/// Contacts tab for an automation process — lists contacts currently in the process.
/// </summary>
[UINavigation(false)]
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class AutomationProcessContactsPage : ListingPage
{
    // Force rebuild marker
    protected override string ObjectType => AutomationProcessContactStateInfo.OBJECT_TYPE;

    /// <summary>Object ID from the parent section page.</summary>
    [PageParameter(typeof(IntPageModelBinder))]
    public int ObjectId { get; set; }

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        PageConfiguration.QueryModifiers.AddModifier((query, settings) =>
            query.WhereEquals(
                nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateProcessID),
                ObjectId));

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateContactID),
                "Contact ID", searchable: true, defaultSortDirection: SortTypeEnum.Asc)
            .AddComponentColumn(
                nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateStatus),
                NamedComponentCellComponentNames.SIMPLE_STATUS_COMPONENT, "Status",
                modelRetriever: ContactStatusModelRetriever)
            .AddColumn(nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateStartedAt),
                "Started")
            .AddColumn(nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateStepEnteredAt),
                "Step Entered")
            .AddColumn(nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateLastModified),
                "Last Modified");
    }

    private static object ContactStatusModelRetriever(object value, IDataContainer dataContainer)
    {
        var status = value?.ToString() ?? "Active";
        var (color, label, icon) = status switch
        {
            "Active" => (Color.SuccessText, "Active", Icons.RotateRight),
            "Waiting" => (Color.SuccessText, "Waiting", Icons.Clock),
            "Completed" => (Color.SuccessText, "Completed", Icons.CheckCircle),
            "Failed" => (Color.AlertText, "Failed", Icons.BanSign),
            "Removed" => (Color.AlertText, "Removed", Icons.BanSign),
            _ => (Color.SuccessText, status, Icons.Circle)
        };

        return new SimpleStatusNamedComponentCellProps
        {
            IconName = icon,
            Label = label,
            IconColor = color,
            LabelColor = color
        };
    }
}
