using CMS.Membership;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;

[assembly:
    UIApplication(
    identifier: Baseline.AI.Admin.UIPages.BaselineAIApplication.IDENTIFIER,
    type: typeof(Baseline.AI.Admin.UIPages.BaselineAIApplication),
    slug: "baseline-ai",
    name: "Baseline AI",
    category: BaseApplicationCategories.DEVELOPMENT,
    icon: Icons.Magnifier,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Baseline.AI.Admin.UIPages;

/// <summary>
/// Application page for Baseline AI configuration in the admin interface.
/// Follows Lucene's LuceneApplicationPage pattern with full CRUD + Rebuild permissions.
/// </summary>
[UIPermission(SystemPermissions.VIEW)]
[UIPermission(SystemPermissions.CREATE)]
[UIPermission(SystemPermissions.UPDATE)]
[UIPermission(SystemPermissions.DELETE)]
[UIPermission(AIKnowledgeBasePermissions.REBUILD, "Rebuild")]
public class BaselineAIApplication : ApplicationPage
{
    /// <summary>
    /// Unique identifier for the Baseline AI application.
    /// </summary>
    public const string IDENTIFIER = "xperiencecommunity-baselineai-app";
}
