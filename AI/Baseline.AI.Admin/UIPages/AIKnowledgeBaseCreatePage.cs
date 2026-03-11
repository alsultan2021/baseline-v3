using Baseline.AI.Data;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Membership;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using Microsoft.Extensions.Logging;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.AI.Admin.UIPages.AIKnowledgeBaseListingPage),
    slug: "create",
    uiPageType: typeof(Baseline.AI.Admin.UIPages.AIKnowledgeBaseCreatePage),
    name: "Create Knowledge Base",
    templateName: TemplateNames.EDIT,
    order: 0)]

namespace Baseline.AI.Admin.UIPages;

/// <summary>
/// Create page for AI Knowledge Bases — mirrors Lucene's <c>IndexCreatePage</c>.
/// Same form as edit; on success navigates to the edit page.
/// </summary>
[UIEvaluatePermission(SystemPermissions.CREATE)]
public class AIKnowledgeBaseCreatePage(
    IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IInfoProvider<AIKnowledgeBaseInfo> kbProvider,
    IInfoProvider<AIKnowledgeBasePathInfo> pathProvider,
    IInfoProvider<ChannelInfo> channelProvider,
    IPageLinkGenerator pageLinkGenerator,
    ILogger<AIKnowledgeBaseCreatePage> logger)
    : BaseAIKnowledgeBaseEditPage(formItemCollectionProvider, formDataBinder, kbProvider, pathProvider, channelProvider, logger)
{
    private AIKnowledgeBaseConfigModel? _model;

    protected override AIKnowledgeBaseConfigModel Model => _model ??= new();

    protected override async Task<ICommandResponse> ProcessFormData(
        AIKnowledgeBaseConfigModel model,
        ICollection<IFormItem> formItems)
    {
        var (success, kbId) = await ValidateAndProcess(model);

        if (success)
        {
            var pageParams = new PageParameterValues
            {
                { typeof(AIKnowledgeBaseEditPage), kbId }
            };

            return NavigateTo(pageLinkGenerator.GetPath<AIKnowledgeBaseEditPage>(pageParams))
                .AddSuccessMessage("Knowledge base created.");
        }

        return ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure))
            .AddErrorMessage("Could not create knowledge base.");
    }
}
