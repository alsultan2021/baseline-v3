using Baseline.AI.Data;
using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.Websites;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Baseline.AI.Indexing;

/// <summary>
/// Default implementation of content scanning for AI indexing.
/// </summary>
public class DefaultAIContentScanner : IAIContentScanner
{
    private readonly IContentQueryExecutor _contentQueryExecutor;
    private readonly ILogger<DefaultAIContentScanner> _logger;

    public DefaultAIContentScanner(
        IContentQueryExecutor contentQueryExecutor,
        ILogger<DefaultAIContentScanner> logger)
    {
        _contentQueryExecutor = contentQueryExecutor;
        _logger = logger;
    }

    public async IAsyncEnumerable<IAIIndexableItem> ScanAllAsync(
        int knowledgeBaseId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var kb = AIKnowledgeBaseInfo.Provider.Get()
            .WhereEquals(nameof(AIKnowledgeBaseInfo.KnowledgeBaseId), knowledgeBaseId)
            .TopN(1)
            .FirstOrDefault();
        if (kb == null)
        {
            _logger.LogError("Knowledge base not found: {KBId}", knowledgeBaseId);
            yield break;
        }

        var options = new ContentQueryExecutionOptions
        {
            ForPreview = false,
            IncludeSecuredItems = false
        };

        // Get paths
        var paths = AIKnowledgeBasePathInfo.Provider.Get()
            .WhereEquals(nameof(AIKnowledgeBasePathInfo.PathKnowledgeBaseId), kb.KnowledgeBaseId)
            .ToList();

        if (!paths.Any())
        {
            _logger.LogWarning("No paths configured for KB {KBName}", kb.KnowledgeBaseName);
            yield break;
        }

        // Get channel info
        var channelInfo = ChannelInfo.Provider.Get()
            .WhereEquals(nameof(ChannelInfo.ChannelName), ParseFirstChannel(kb.KnowledgeBaseChannels))
            .TopN(1)
            .FirstOrDefault();

        if (channelInfo == null)
        {
            _logger.LogError("Channel not found: {ChannelName}", ParseFirstChannel(kb.KnowledgeBaseChannels));
            yield break;
        }

        // Scan each language
        var languages = ParseJsonArray(kb.KnowledgeBaseLanguages);
        foreach (var lang in languages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Scan reusable content
            if (paths.Any(p => !string.IsNullOrEmpty(p.PathContentTypes) && p.PathContentTypes != "[]"))
            {
                await foreach (var item in ScanReusableContentAsync(paths, lang, options, cancellationToken))
                {
                    yield return item;
                }
            }

            // Scan page content
            if (paths.Any(p => !string.IsNullOrEmpty(p.PathIncludePattern)))
            {
                await foreach (var item in ScanPageContentAsync(paths, lang, channelInfo.ChannelID, ParseFirstChannel(kb.KnowledgeBaseChannels) ?? string.Empty, options, cancellationToken))
                {
                    yield return item;
                }
            }
        }

        _logger.LogInformation("Scan complete for KB {KBName}", kb.KnowledgeBaseName);
    }

    public async Task<IAIIndexableItem?> GetItemAsync(
        Guid contentItemGuid,
        string languageCode,
        int channelId,
        CancellationToken cancellationToken = default)
    {
        var options = new ContentQueryExecutionOptions
        {
            ForPreview = false,
            IncludeSecuredItems = false
        };

        // Try as webpage
        if (channelId > 0)
        {
            var channelName = GetChannelName(channelId);
            if (!string.IsNullOrEmpty(channelName))
            {
                var pageBuilder = new ContentItemQueryBuilder()
                    .ForContentTypes(query => query
                        .WithLinkedItems(1, opts => opts.IncludeWebPageData(true))
                        .ForWebsite(channelName))
                    .InLanguage(languageCode)
                    .Parameters(query => query
                        .Where(where => where.WhereEquals(nameof(ContentItemFields.ContentItemGUID), contentItemGuid)));

                var pages = await _contentQueryExecutor.GetMappedWebPageResult<IWebPageFieldsSource>(pageBuilder, options);
                var page = pages.FirstOrDefault();
                if (page != null)
                {
                    return new IndexableWebPageItem(page, channelId, channelName);
                }
            }
        }

        // Try as reusable content
        var contentBuilder = new ContentItemQueryBuilder()
            .ForContentTypes(query => query.WithContentTypeFields())
            .InLanguage(languageCode)
            .Parameters(query => query
                .Where(where => where.WhereEquals(nameof(ContentItemFields.ContentItemGUID), contentItemGuid)));

        var items = await _contentQueryExecutor.GetMappedResult<IContentItemFieldsSource>(contentBuilder, options);
        var item = items.FirstOrDefault();
        if (item != null)
        {
            return new IndexableContentItem(item);
        }

        return null;
    }

    public bool MatchesConfiguration(
        int knowledgeBaseId,
        string contentTypeName,
        int channelId,
        string? urlPath)
    {
        var kb = AIKnowledgeBaseInfo.Provider.Get()
            .WhereEquals(nameof(AIKnowledgeBaseInfo.KnowledgeBaseId), knowledgeBaseId)
            .TopN(1)
            .FirstOrDefault();
        if (kb == null)
        {
            return false;
        }

        var paths = AIKnowledgeBasePathInfo.Provider.Get()
            .WhereEquals(nameof(AIKnowledgeBasePathInfo.PathKnowledgeBaseId), knowledgeBaseId)
            .ToList();

        foreach (var path in paths)
        {
            // Check content type
            if (!string.IsNullOrEmpty(path.PathContentTypes) && path.PathContentTypes != "[]")
            {
                // Parse JSON array of content types
                if (path.PathContentTypes.Contains(contentTypeName))
                {
                    return true;
                }
            }

            // Check path pattern for web pages
            if (!string.IsNullOrEmpty(path.PathIncludePattern) && !string.IsNullOrEmpty(urlPath))
            {
                if (MatchesPattern(urlPath, path.PathIncludePattern))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private async IAsyncEnumerable<IAIIndexableItem> ScanReusableContentAsync(
        IEnumerable<AIKnowledgeBasePathInfo> paths,
        string language,
        ContentQueryExecutionOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var contentTypePaths = paths.Where(p => !string.IsNullOrEmpty(p.PathContentTypes) && p.PathContentTypes != "[]").ToList();

        foreach (var path in contentTypePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Parse PathContentTypes JSON array - simple approach
            var contentTypes = path.PathContentTypes
                .Trim('[', ']')
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim('"', ' '))
                .Where(t => !string.IsNullOrWhiteSpace(t));

            foreach (var contentTypeName in contentTypes)
            {
                var builder = new ContentItemQueryBuilder()
                    .ForContentType(contentTypeName)
                    .InLanguage(language);

                var items = await _contentQueryExecutor.GetMappedResult<IContentItemFieldsSource>(builder, options);

                foreach (var item in items)
                {
                    yield return new IndexableContentItem(item, language);
                }
            }
        }
    }

    private async IAsyncEnumerable<IAIIndexableItem> ScanPageContentAsync(
        IEnumerable<AIKnowledgeBasePathInfo> paths,
        string language,
        int channelId,
        string channelName,
        ContentQueryExecutionOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var pathPatterns = paths
            .Where(p => !string.IsNullOrEmpty(p.PathIncludePattern))
            .Select(p => p.PathIncludePattern!)
            .ToList();

        if (!pathPatterns.Any())
        {
            yield break;
        }

        var builder = new ContentItemQueryBuilder()
            .ForContentTypes(query => query
                .WithLinkedItems(1, opts => opts.IncludeWebPageData(true))
                .ForWebsite(channelName))
            .InLanguage(language);

        var pages = await _contentQueryExecutor.GetMappedWebPageResult<IWebPageFieldsSource>(builder, options);

        foreach (var page in pages)
        {
            var treePath = page.SystemFields.WebPageItemTreePath ?? string.Empty;
            if (pathPatterns.Any(pattern => MatchesPattern(treePath, pattern)))
            {
                yield return new IndexableWebPageItem(page, channelId, channelName, language);
            }
        }
    }

    private bool MatchesPattern(string path, string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\%", ".*") + "$";
        return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
    }

    private string? GetChannelName(int channelId)
    {
        return ChannelInfo.Provider.Get()
            .WhereEquals(nameof(ChannelInfo.ChannelID), channelId)
            .TopN(1)
            .FirstOrDefault()
            ?.ChannelName;
    }

    private string? GetContentTypeName(int contentTypeId)
    {
        return DataClassInfoProvider.GetDataClassInfo(contentTypeId)?.ClassName;
    }

    private string[] ParseJsonArray(string json)
    {
        // Simple JSON array parsing for ["val1", "val2"]
        return json.Trim('[', ']')
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim('\"', ' '))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();
    }

    private string? ParseFirstChannel(string channelsJson)
    {
        var channels = ParseJsonArray(channelsJson);
        return channels.FirstOrDefault();
    }

    // Wrapper implementations

    private class IndexableContentItem : IAIIndexableItem
    {
        private readonly IContentItemFieldsSource _source;
        private readonly string _languageCode;

        public IndexableContentItem(IContentItemFieldsSource source, string languageCode = "en")
        {
            _source = source;
            _languageCode = languageCode;
        }

        public int ContentItemId => _source.SystemFields.ContentItemID;
        public Guid ContentItemGuid => _source.SystemFields.ContentItemGUID;
        public string ContentTypeName => _source.GetType().Name;
        public string LanguageCode => _languageCode;
        public int ChannelId => 0; // Reusable content
        public string? ChannelName => null;
        public bool IsWebPageItem => false;
        public string? UrlPath => null;

        public object? GetFieldValue(string fieldName)
        {
            try
            {
                var prop = _source.GetType().GetProperty(fieldName);
                return prop?.GetValue(_source);
            }
            catch
            {
                return null;
            }
        }

        public T? GetFieldValue<T>(string fieldName)
        {
            var value = GetFieldValue(fieldName);
            return value is T typedValue ? typedValue : default;
        }

        public IReadOnlyList<string> GetFieldNames()
        {
            return _source.GetType()
                .GetProperties()
                .Select(p => p.Name)
                .ToList();
        }
    }

    private class IndexableWebPageItem : IAIIndexableItem
    {
        private readonly IWebPageFieldsSource _source;
        private readonly string _languageCode;

        public IndexableWebPageItem(IWebPageFieldsSource source, int channelId, string channelName, string languageCode = "en")
        {
            _source = source;
            ChannelId = channelId;
            ChannelName = channelName;
            _languageCode = languageCode;
        }

        public int ContentItemId => _source.SystemFields.ContentItemID;
        public Guid ContentItemGuid => _source.SystemFields.ContentItemGUID;
        public string ContentTypeName => _source.GetType().Name;
        public string LanguageCode => _languageCode;
        public int ChannelId { get; }
        public string? ChannelName { get; }
        public bool IsWebPageItem => true;
        public string? UrlPath => _source.SystemFields.WebPageItemTreePath;

        public object? GetFieldValue(string fieldName)
        {
            try
            {
                var prop = _source.GetType().GetProperty(fieldName);
                return prop?.GetValue(_source);
            }
            catch
            {
                return null;
            }
        }

        public T? GetFieldValue<T>(string fieldName)
        {
            var value = GetFieldValue(fieldName);
            return value is T typedValue ? typedValue : default;
        }

        public IReadOnlyList<string> GetFieldNames()
        {
            return _source.GetType()
                .GetProperties()
                .Select(p => p.Name)
                .ToList();
        }
    }
}
