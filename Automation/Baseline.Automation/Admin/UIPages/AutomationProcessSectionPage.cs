using Baseline.Automation.Models;

using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(Baseline.Automation.Admin.UIPages.AutomationProcessListPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(Baseline.Automation.Admin.UIPages.AutomationProcessSectionPage),
    name: "Edit process",
    templateName: TemplateNames.SIDE_NAVIGATION_LAYOUT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Automation.Admin.UIPages;

/// <summary>
/// Section page for individual automation process editing — provides side-nav tabs
/// (Builder, Statistics, Contacts). General settings open as a side panel from the Builder.
/// </summary>
public class AutomationProcessSectionPage : EditSectionPage<AutomationProcessInfo>
{
}
