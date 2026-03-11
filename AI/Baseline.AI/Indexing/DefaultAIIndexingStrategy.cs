using Baseline.AI.Indexing;

namespace Baseline.AI;

/// <summary>
/// Default AI indexing strategy that indexes all text content.
/// Similar to DefaultLuceneIndexingStrategy.
/// </summary>
public class DefaultAIIndexingStrategy : IAIIndexingStrategy
{
    /// <inheritdoc />
    public virtual string StrategyName => "Default";

    /// <inheritdoc />
    public virtual string DisplayName => "Default AI Indexing Strategy";

    /// <summary>
    /// Field names to look for when building content.
    /// </summary>
    protected virtual string[] ContentFieldNames =>
    [
        "Content",
        "Description",
        "Summary",
        "Body",
        "Text",
        "RichText",
        "ArticleContent",
        "PageContent",
        "BlogPostContent",
        "ProductDescription"
    ];

    /// <summary>
    /// Field names to look for title.
    /// </summary>
    protected virtual string[] TitleFieldNames =>
    [
        "Title",
        "Name",
        "Headline",
        "PageTitle",
        "ArticleTitle",
        "ProductName"
    ];

    /// <inheritdoc />
    public virtual Task<AIDocument?> MapToDocumentAsync(
        IAIIndexableItem item,
        CancellationToken cancellationToken = default)
    {
        var content = ExtractContent(item);
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult<AIDocument?>(null);
        }

        var title = ExtractTitle(item);
        var url = item.UrlPath;

        var document = new AIDocument
        {
            Id = $"{item.ContentItemGuid}_{item.LanguageCode}",
            ContentItemId = item.ContentItemId,
            ContentItemGuid = item.ContentItemGuid,
            Content = content,
            Title = title,
            Url = url,
            ContentTypeName = item.ContentTypeName,
            LanguageCode = item.LanguageCode,
            LastModified = DateTime.UtcNow
        };

        return Task.FromResult<AIDocument?>(document);
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<string> GetContentTypes() => [];

    /// <inheritdoc />
    public virtual bool ShouldProcess(string contentTypeName) => true;

    #region V3 Methods - ExtractAsync pattern

    /// <inheritdoc />
    public virtual Task<AIExtractResult?> ExtractAsync(
        IAIIndexableItem item,
        CancellationToken cancellationToken = default)
    {
        var content = ExtractContent(item);
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult<AIExtractResult?>(null);
        }

        var title = ExtractTitle(item);
        var metadata = new Dictionary<string, object>
        {
            ["contentItemId"] = item.ContentItemId,
            ["contentItemGuid"] = item.ContentItemGuid.ToString(),
            ["contentTypeName"] = item.ContentTypeName,
            ["languageCode"] = item.LanguageCode
        };

        if (!string.IsNullOrEmpty(title))
        {
            metadata["title"] = title;
        }

        var result = new AIExtractResult(
            Content: content,
            Title: title,
            Url: item.UrlPath,
            UrlPath: item.UrlPath,
            ContentTypeName: item.ContentTypeName,
            LanguageCode: item.LanguageCode,
            ChannelId: item.ChannelId,
            ChannelName: item.ChannelName,
            ContentItemId: item.ContentItemId,
            ContentItemGuid: item.ContentItemGuid,
            LastModified: DateTime.UtcNow,
            Metadata: metadata
        );

        return Task.FromResult<AIExtractResult?>(result);
    }

    /// <inheritdoc />
    public virtual ChunkingOptions GetChunkingOptions() => new();

    /// <inheritdoc />
    public virtual string? ComputeStrategyHash() => null; // Use default registry hash

    #endregion

    /// <inheritdoc />
    public virtual IReadOnlyList<AIFieldDefinition> GetFieldDefinitions()
    {
        var definitions = new List<AIFieldDefinition>();

        foreach (var field in TitleFieldNames)
        {
            definitions.Add(new AIFieldDefinition
            {
                FieldName = field,
                IncludeInEmbedding = true,
                StoreAsMetadata = true,
                Weight = 1.5 // Boost title
            });
        }

        foreach (var field in ContentFieldNames)
        {
            definitions.Add(new AIFieldDefinition
            {
                FieldName = field,
                IncludeInEmbedding = true,
                Weight = 1.0
            });
        }

        return definitions;
    }

    /// <inheritdoc />
    public virtual Task<string> PreprocessTextAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        // Default preprocessing: clean HTML, normalize whitespace
        var cleaned = StripHtml(text);
        cleaned = NormalizeWhitespace(cleaned);
        return Task.FromResult(cleaned);
    }

    /// <inheritdoc />
    public virtual Task<Dictionary<string, object>> GetMetadataAsync(
        IAIIndexableItem item,
        CancellationToken cancellationToken = default)
    {
        var metadata = new Dictionary<string, object>
        {
            ["contentItemId"] = item.ContentItemId,
            ["contentItemGuid"] = item.ContentItemGuid.ToString(),
            ["contentTypeName"] = item.ContentTypeName,
            ["languageCode"] = item.LanguageCode
        };

        if (!string.IsNullOrEmpty(item.ChannelName))
        {
            metadata["channelName"] = item.ChannelName;
        }

        if (!string.IsNullOrEmpty(item.UrlPath))
        {
            metadata["url"] = item.UrlPath;
        }

        var title = ExtractTitle(item);
        if (!string.IsNullOrEmpty(title))
        {
            metadata["title"] = title;
        }

        return Task.FromResult(metadata);
    }

    /// <summary>
    /// Extracts content from the item.
    /// </summary>
    protected virtual string ExtractContent(IAIIndexableItem item)
    {
        var parts = new List<string>();

        foreach (var fieldName in ContentFieldNames)
        {
            var value = item.GetFieldValue<string>(fieldName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                parts.Add(value);
            }
        }

        var combined = string.Join("\n\n", parts);
        return StripHtml(NormalizeWhitespace(combined));
    }

    /// <summary>
    /// Extracts title from the item.
    /// </summary>
    protected virtual string? ExtractTitle(IAIIndexableItem item)
    {
        foreach (var fieldName in TitleFieldNames)
        {
            var value = item.GetFieldValue<string>(fieldName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// Strips HTML tags from text.
    /// </summary>
    protected static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        // Simple HTML stripping - for production, consider HtmlAgilityPack
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        return text;
    }

    /// <summary>
    /// Normalizes whitespace.
    /// </summary>
    protected static string NormalizeWhitespace(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return System.Text.RegularExpressions.Regex.Replace(text.Trim(), @"\s+", " ");
    }
}
