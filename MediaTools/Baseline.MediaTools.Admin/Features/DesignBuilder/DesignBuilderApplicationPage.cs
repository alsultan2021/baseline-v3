using Baseline.MediaTools.Admin.Features.DesignBuilder;
using CMS.Membership;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: UIApplication(
    identifier: DesignBuilderApplicationPage.IDENTIFIER,
    type: typeof(DesignBuilderApplicationPage),
    slug: "design-builder",
    name: "Design Builder",
    category: DigitalMarketingApplicationCategories.DIGITAL_MARKETING,
    icon: Icons.Layout,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Baseline.MediaTools.Admin.Features.DesignBuilder;

[UIPermission(SystemPermissions.VIEW)]
[UIPermission(SystemPermissions.CREATE)]
[UIPermission(SystemPermissions.UPDATE)]
[UIPermission(SystemPermissions.DELETE)]
public class DesignBuilderApplicationPage : ApplicationPage
{
    public const string IDENTIFIER = "design-builder-app";
}
