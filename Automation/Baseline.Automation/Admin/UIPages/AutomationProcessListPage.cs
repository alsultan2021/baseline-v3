using System.Text.Json;

using CMS.Base;
using CMS.DataEngine;
using CMS.Membership;

using Baseline.Automation.Models;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: UIPage(
    parentType: typeof(AutomationApplication),
    slug: "baseline-automation-processes",
    uiPageType: typeof(Baseline.Automation.Admin.UIPages.AutomationProcessListPage),
    name: "Automation processes",
    templateName: TemplateNames.LISTING,
    order: UIPageOrder.NoOrder,
    Icon = Icons.Cogwheels)]

namespace Baseline.Automation.Admin.UIPages;

/// <summary>
/// Listing page for all Baseline automation processes.
/// Registers under Kentico's AutomationApplication so it appears in the Digital Marketing → Automation section.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class AutomationProcessListPage(
    IInfoProvider<AutomationProcessInfo> processInfoProvider)
    : ListingPage
{
    protected override string ObjectType => AutomationProcessInfo.OBJECT_TYPE;

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        PageConfiguration.HeaderActions.AddLink<AutomationProcessCreatePage>("New process");

        PageConfiguration.AddEditRowAction<AutomationProcessSectionPage>();

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(AutomationProcessInfo.AutomationProcessDisplayName), "Process name",
                searchable: true, defaultSortDirection: SortTypeEnum.Asc)
            .AddComponentColumn(nameof(AutomationProcessInfo.AutomationProcessTriggerJson),
                NamedComponentCellComponentNames.SIMPLE_STATUS_COMPONENT, "Trigger",
                modelRetriever: TriggerModelRetriever)
            .AddComponentColumn(nameof(AutomationProcessInfo.AutomationProcessIsEnabled),
                NamedComponentCellComponentNames.SIMPLE_STATUS_COMPONENT, "Status",
                modelRetriever: StatusModelRetriever)
            .AddComponentColumn(nameof(AutomationProcessInfo.AutomationProcessRecurrence),
                NamedComponentCellComponentNames.SIMPLE_STATUS_COMPONENT, "Triggered",
                modelRetriever: TriggeredModelRetriever);

        PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
    }

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override async Task<ICommandResponse<RowActionResult>> Delete(int id)
    {
        var info = processInfoProvider.Get()
            .WhereEquals(nameof(AutomationProcessInfo.AutomationProcessID), id)
            .FirstOrDefault();

        if (info != null)
        {
            processInfoProvider.Delete(info);
        }

        return ResponseFrom(new RowActionResult(true))
            .AddSuccessMessage("Process deleted.");
    }

    private static object StatusModelRetriever(object value, IDataContainer dataContainer)
    {
        var isEnabled = value is bool b && b;
        var (color, label, icon) = isEnabled
            ? (Color.SuccessText, "Enabled", Icons.CheckCircle)
            : (Color.TextLowEmphasis, "Disabled", Icons.BanSign);

        return new SimpleStatusNamedComponentCellProps
        {
            IconName = icon,
            Label = label,
            IconColor = color,
            LabelColor = color
        };
    }

    private static object TriggerModelRetriever(object value, IDataContainer dataContainer)
    {
        var json = value as string;
        if (string.IsNullOrEmpty(json))
            return new SimpleStatusNamedComponentCellProps
            {
                Label = "",
                LabelColor = Color.TextDefaultOnLight
            };

        try
        {
            var trigger = JsonSerializer.Deserialize<AutomationTrigger>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (trigger != null)
            {
                var displayText = trigger.TriggerType switch
                {
                    AutomationTriggerType.FormSubmission =>
                        string.IsNullOrEmpty(trigger.Name) ? "Form" : $"Form: {trigger.Name}",
                    AutomationTriggerType.MemberRegistration =>
                        "Activity type: Member registration",
                    AutomationTriggerType.CustomActivity =>
                        string.IsNullOrEmpty(trigger.Name) ? "Activity type" : $"Activity type: {trigger.Name}",
                    _ => trigger.Name ?? trigger.TriggerType.ToString()
                };

                return new SimpleStatusNamedComponentCellProps
                {
                    Label = displayText,
                    LabelColor = Color.TextDefaultOnLight
                };
            }
        }
        catch { /* ignore malformed JSON */ }

        return new SimpleStatusNamedComponentCellProps
        {
            Label = "",
            LabelColor = Color.TextDefaultOnLight
        };
    }

    private static object TriggeredModelRetriever(object value, IDataContainer dataContainer)
    {
        return new SimpleStatusNamedComponentCellProps
        {
            Label = "0",
            LabelColor = Color.TextDefaultOnLight
        };
    }
}
