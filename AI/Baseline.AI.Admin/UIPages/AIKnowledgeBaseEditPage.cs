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
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(Baseline.AI.Admin.UIPages.AIKnowledgeBaseEditPage),
    name: "Edit Knowledge Base",
    templateName: TemplateNames.EDIT,
    order: 1)]

namespace Baseline.AI.Admin.UIPages;

/// <summary>
/// Single-page edit for AI Knowledge Base — mirrors Lucene's <c>IndexEditPage</c>.
/// All configuration (name, strategy, paths) on one form.
/// </summary>
[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class AIKnowledgeBaseEditPage(
    IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IInfoProvider<AIKnowledgeBaseInfo> kbProvider,
    IInfoProvider<AIKnowledgeBasePathInfo> pathProvider,
    IInfoProvider<ChannelInfo> channelProvider,
    ILogger<AIKnowledgeBaseEditPage> logger)
    : BaseAIKnowledgeBaseEditPage(formItemCollectionProvider, formDataBinder, kbProvider, pathProvider, channelProvider, logger)
{
    private AIKnowledgeBaseConfigModel? _model;

    [PageParameter(typeof(IntPageModelBinder))]
    public int ObjectId { get; set; }

    protected override AIKnowledgeBaseConfigModel Model
    {
        get
        {
            if (_model is not null)
            {
                return _model;
            }

            var kb = KBProvider.Get()
                .WhereEquals(nameof(AIKnowledgeBaseInfo.KnowledgeBaseId), ObjectId)
                .TopN(1)
                .FirstOrDefault();

            _model = kb is not null
                ? new AIKnowledgeBaseConfigModel(kb, LoadPaths(PathProvider, ObjectId))
                : new AIKnowledgeBaseConfigModel();

            return _model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(
        AIKnowledgeBaseConfigModel model,
        ICollection<IFormItem> formItems)
    {
        model.Id = ObjectId;
        var (success, _) = await ValidateAndProcess(model);

        var response = ResponseFrom(new FormSubmissionResult(
            success
                ? FormSubmissionStatus.ValidationSuccess
                : FormSubmissionStatus.ValidationFailure));

        _ = success
            ? response.AddSuccessMessage("Knowledge base saved.")
            : response.AddErrorMessage("Could not update knowledge base.");

        return response;
    }
}
