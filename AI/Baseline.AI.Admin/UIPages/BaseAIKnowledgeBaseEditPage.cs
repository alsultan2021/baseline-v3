using System.Text;
using System.Text.Json;

using Baseline.AI.Admin.Models;
using Baseline.AI.Data;

using CMS.ContentEngine;
using CMS.DataEngine;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using Microsoft.Extensions.Logging;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

namespace Baseline.AI.Admin.UIPages;

/// <summary>
/// Abstract base page for AI Knowledge Base create + edit — mirrors Lucene's
/// <c>BaseIndexEditPage</c>. Shared validation and save logic.
/// </summary>
public abstract class BaseAIKnowledgeBaseEditPage(
    IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IInfoProvider<AIKnowledgeBaseInfo> kbProvider,
    IInfoProvider<AIKnowledgeBasePathInfo> pathProvider,
    IInfoProvider<ChannelInfo> channelProvider,
    ILogger logger)
    : ModelEditPage<AIKnowledgeBaseConfigModel>(formItemCollectionProvider, formDataBinder)
{
    protected readonly IInfoProvider<AIKnowledgeBaseInfo> KBProvider = kbProvider;
    protected readonly IInfoProvider<AIKnowledgeBasePathInfo> PathProvider = pathProvider;
    protected readonly IInfoProvider<ChannelInfo> ChannelProvider = channelProvider;
    protected readonly ILogger Logger = logger;

    /// <summary>
    /// Validate and persist the configuration model. Returns the saved KB ID on success.
    /// </summary>
    protected async Task<(bool Success, int KnowledgeBaseId)> ValidateAndProcess(
        AIKnowledgeBaseConfigModel configuration)
    {
        try
        {
            var codeName = RemoveWhitespace(configuration.KnowledgeBaseName);

            AIKnowledgeBaseInfo kb;

            if (configuration.Id > 0)
            {
                // Update existing
                kb = KBProvider.Get()
                    .WhereEquals(nameof(AIKnowledgeBaseInfo.KnowledgeBaseId), configuration.Id)
                    .TopN(1)
                    .FirstOrDefault()!;

                if (kb is null)
                {
                    return (false, 0);
                }

                kb.KnowledgeBaseName = codeName;
                kb.KnowledgeBaseDisplayName = configuration.DisplayName;
                kb.KnowledgeBaseStrategyName = configuration.StrategyName;
                KBProvider.Set(kb);
            }
            else
            {
                // Insert new
                kb = new AIKnowledgeBaseInfo
                {
                    KnowledgeBaseName = codeName,
                    KnowledgeBaseDisplayName = configuration.DisplayName,
                    KnowledgeBaseStrategyName = configuration.StrategyName
                };
                KBProvider.Set(kb);
            }

            // ── Sync paths ──────────────────────────────────────────────────
            await SyncPathsAsync(kb.KnowledgeBaseId, (configuration.Paths ?? []).ToList());

            return (true, kb.KnowledgeBaseId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving knowledge base configuration");
            return (false, 0);
        }
    }

    // ── Path synchronization ────────────────────────────────────────────────

    private async Task SyncPathsAsync(int kbId, List<AIKBPathConfiguration> submitted)
    {
        var existing = PathProvider.Get()
            .WhereEquals(nameof(AIKnowledgeBasePathInfo.PathKnowledgeBaseId), kbId)
            .GetEnumerableTypedResult()
            .ToList();

        var existingIds = existing.Select(p => p.PathId).ToHashSet();
        var submittedIds = submitted
            .Where(p => p.Identifier is not null)
            .Select(p => p.Identifier!.Value)
            .ToHashSet();

        // Delete removed paths
        foreach (var removed in existing.Where(p => !submittedIds.Contains(p.PathId)))
        {
            PathProvider.Delete(removed);
        }

        // Insert or update
        foreach (var cfg in submitted)
        {
            var channelId = ChannelProvider.Get()
                .WhereEquals(nameof(ChannelInfo.ChannelName), cfg.ChannelName)
                .FirstOrDefault()?.ChannelID ?? 0;

            if (cfg.Identifier is not null && existingIds.Contains(cfg.Identifier.Value))
            {
                var info = existing.First(p => p.PathId == cfg.Identifier.Value);
                MapToInfo(info, cfg, channelId);
                PathProvider.Set(info);
            }
            else
            {
                var info = new AIKnowledgeBasePathInfo { PathKnowledgeBaseId = kbId };
                MapToInfo(info, cfg, channelId);
                PathProvider.Set(info);
            }
        }

        await Task.CompletedTask;
    }

    // ── Mapping helpers ─────────────────────────────────────────────────────

    protected static List<AIKBPathConfiguration> LoadPaths(
        IInfoProvider<AIKnowledgeBasePathInfo> provider, int kbId) =>
        provider.Get()
            .WhereEquals(nameof(AIKnowledgeBasePathInfo.PathKnowledgeBaseId), kbId)
            .GetEnumerableTypedResult()
            .Select(ToConfiguration)
            .ToList();

    private static AIKBPathConfiguration ToConfiguration(AIKnowledgeBasePathInfo info) => new()
    {
        Identifier = info.PathId,
        ChannelName = info.PathChannelName ?? "",
        ChannelDisplayName = info.PathChannelName ?? "",
        IncludePattern = info.PathIncludePattern ?? "/%",
        ExcludePattern = info.PathExcludePattern,
        ContentTypes = DeserializeContentTypes(info.PathContentTypes),
        Priority = info.PathPriority,
        IncludeChildren = info.PathIncludeChildren
    };

    private static void MapToInfo(AIKnowledgeBasePathInfo info, AIKBPathConfiguration cfg, int channelId)
    {
        info.PathChannelName = cfg.ChannelName;
        info.PathChannelId = channelId;
        info.PathIncludePattern = cfg.IncludePattern;
        info.PathExcludePattern = cfg.ExcludePattern;
        info.PathContentTypes = SerializeContentTypes(cfg.ContentTypes);
        info.PathPriority = cfg.Priority;
        info.PathIncludeChildren = cfg.IncludeChildren;
    }

    private static List<AIKBContentType> DeserializeContentTypes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var names = JsonSerializer.Deserialize<List<string>>(json);
            if (names is not null)
            {
                return names.Select(n => new AIKBContentType(n, n)).ToList();
            }
        }
        catch { /* try object array next */ }

        try
        {
            return JsonSerializer.Deserialize<List<AIKBContentType>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string SerializeContentTypes(List<AIKBContentType>? types) =>
        types is not null && types.Count > 0
            ? JsonSerializer.Serialize(types.Select(t => t.ContentTypeName))
            : "[]";

    protected static string RemoveWhitespace(string source)
    {
        var builder = new StringBuilder(source.Length);
        for (int i = 0; i < source.Length; i++)
        {
            char c = source[i];
            if (!char.IsWhiteSpace(c))
            {
                builder.Append(c);
            }
        }

        return source.Length == builder.Length ? source : builder.ToString();
    }
}
