using Baseline.AI.Data;
using Baseline.AI.Indexing;
using CMS.Core;
using CMS.DataEngine;
using CMS.Membership;

using Kentico.Xperience.Admin.Base;

using Microsoft.Extensions.Logging;

[assembly: UIPage(
    parentType: typeof(Baseline.AI.Admin.UIPages.BaselineAIApplication),
    slug: "knowledge-bases",
    uiPageType: typeof(Baseline.AI.Admin.UIPages.AIKnowledgeBaseListingPage),
    name: "Knowledge Bases",
    templateName: TemplateNames.LISTING,
    order: UIPageOrder.First)]

namespace Baseline.AI.Admin.UIPages;

/// <summary>
/// Listing page for AI Knowledge Bases — follows Lucene's IndexListingPage pattern
/// with Rebuild/Delete row actions, live statistics, and empty-state callout.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class AIKnowledgeBaseListingPage(
    IInfoProvider<AIKnowledgeBaseInfo> knowledgeBaseProvider,
    IInfoProvider<AIKnowledgeBasePathInfo> pathProvider,
    IInfoProvider<AIIndexQueueInfo> queueProvider,
    IInfoProvider<AIContentFingerprintInfo> fingerprintProvider,
    IInfoProvider<AIRebuildJobInfo> rebuildJobProvider,
    IPageLinkGenerator pageLinkGenerator,
    IAIIndexManager indexManager,
    ILogger<AIKnowledgeBaseListingPage> logger) : ListingPage
{
    /// <inheritdoc/>
    protected override string ObjectType => AIKnowledgeBaseInfo.OBJECT_TYPE;

    /// <inheritdoc/>
    public override Task ConfigurePage()
    {
        // Empty-state callout — mirrors Lucene's "No indexes" pattern
        var anyKbs = knowledgeBaseProvider.Get().TopN(1).Any();
        if (!anyKbs)
        {
            PageConfiguration.Callouts =
            [
                new()
                {
                    Headline = "No knowledge bases",
                    Content = "No AI knowledge bases configured. Create one to start indexing content for AI-powered search and chatbot features.",
                    ContentAsHtml = true,
                    Type = CalloutType.FriendlyWarning,
                    Placement = CalloutPlacement.OnDesk
                }
            ];
        }

        // Columns — mirrors Lucene's ID, Name, Strategy, Entries, Last Updated
        PageConfiguration.ColumnConfigurations
            .AddColumn(
                nameof(AIKnowledgeBaseInfo.KnowledgeBaseId),
                "ID",
                defaultSortDirection: SortTypeEnum.Asc,
                sortable: true)
            .AddColumn(
                nameof(AIKnowledgeBaseInfo.KnowledgeBaseDisplayName),
                "Name",
                maxWidth: 30,
                searchable: true,
                sortable: true)
            .AddColumn(
                nameof(AIKnowledgeBaseInfo.KnowledgeBaseStrategyName),
                "Strategy",
                maxWidth: 20,
                searchable: true,
                sortable: true)
            .AddColumn(
                nameof(AIKnowledgeBaseInfo.KnowledgeBaseStatus),
                "Status",
                maxWidth: 12,
                sortable: true)
            .AddColumn(
                nameof(AIKnowledgeBaseInfo.KnowledgeBaseDocumentCount),
                "Items",
                maxWidth: 8,
                sortable: true)
            .AddColumn(
                nameof(AIKnowledgeBaseInfo.KnowledgeBaseChunkCount),
                "Chunks",
                maxWidth: 8,
                sortable: true)
            .AddColumn(
                nameof(AIKnowledgeBaseInfo.KnowledgeBaseLastRebuild),
                "Last Rebuild",
                maxWidth: 15,
                sortable: true);

        // Row + header actions — mirrors Lucene's Edit, Rebuild, Delete, Create
        PageConfiguration.AddEditRowAction<AIKnowledgeBaseEditPage>();
        PageConfiguration.TableActions.AddCommand("Rebuild", nameof(Rebuild), icon: Icons.RotateRight);
        PageConfiguration.TableActions.AddDeleteAction(nameof(Delete), "Delete");
        PageConfiguration.HeaderActions.AddLink<AIKnowledgeBaseCreatePage>("Create");

        return base.ConfigurePage();
    }

    /// <summary>
    /// Rebuild command — mirrors Lucene's IndexListingPage.Rebuild pattern.
    /// </summary>
    [PageCommand(Permission = AIKnowledgeBasePermissions.REBUILD)]
    public async Task<ICommandResponse<RowActionResult>> Rebuild(int id, CancellationToken cancellationToken)
    {
        var result = new RowActionResult(false);

        var kb = knowledgeBaseProvider.Get()
            .WhereEquals(nameof(AIKnowledgeBaseInfo.KnowledgeBaseId), id)
            .FirstOrDefault();

        if (kb is null)
        {
            return ResponseFrom(result)
                .AddErrorMessage($"Error loading knowledge base with identifier {id}.");
        }

        try
        {
            await indexManager.StartRebuildAsync(kb.KnowledgeBaseId);

            return ResponseFrom(result)
                .AddSuccessMessage($"Rebuild started for '{kb.KnowledgeBaseDisplayName}'. Check the Operations tab for progress.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error rebuilding knowledge base {KbId}", id);
            return ResponseFrom(result)
                .AddErrorMessage($"Error rebuilding '{kb.KnowledgeBaseDisplayName}'. Check the Event Log for details.");
        }
    }

    /// <summary>
    /// Delete command — mirrors Lucene's IndexListingPage.Delete with cascade delete.
    /// </summary>
    [PageCommand(Permission = SystemPermissions.DELETE)]
    public Task<ICommandResponse> Delete(int id, CancellationToken _)
    {
        var kb = knowledgeBaseProvider.Get()
            .WhereEquals(nameof(AIKnowledgeBaseInfo.KnowledgeBaseId), id)
            .FirstOrDefault();

        if (kb is null)
        {
            return Task.FromResult<ICommandResponse>(
                ResponseFrom(new RowActionResult(false))
                    .AddErrorMessage($"Error loading knowledge base with identifier {id}."));
        }

        // Delete all dependent objects first using BulkDelete to bypass dependency checks
        pathProvider.BulkDelete(new WhereCondition(
            $"{nameof(AIKnowledgeBasePathInfo.PathKnowledgeBaseId)} = {id}"));
        queueProvider.BulkDelete(new WhereCondition(
            $"{nameof(AIIndexQueueInfo.QueueKnowledgeBaseId)} = {id}"));
        fingerprintProvider.BulkDelete(new WhereCondition(
            $"{nameof(AIContentFingerprintInfo.FingerprintKnowledgeBaseId)} = {id}"));
        rebuildJobProvider.BulkDelete(new WhereCondition(
            $"{nameof(AIRebuildJobInfo.JobKnowledgeBaseId)} = {id}"));

        // Now delete the parent knowledge base
        knowledgeBaseProvider.BulkDelete(new WhereCondition(
            $"{nameof(AIKnowledgeBaseInfo.KnowledgeBaseId)} = {id}"));

        logger.LogInformation("Deleted knowledge base {KbId} '{KbName}'", id, kb.KnowledgeBaseDisplayName);

        // Navigate back to listing — mirrors Lucene's pattern
        var response = NavigateTo(pageLinkGenerator.GetPath<AIKnowledgeBaseListingPage>());
        return Task.FromResult<ICommandResponse>(response);
    }
}
