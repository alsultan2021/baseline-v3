namespace Baseline.Search.Lucene;

/// <summary>
/// Base indexing strategy for content with metadata.
/// Provides common indexing logic for web page items.
/// </summary>
public abstract class BaseMetadataIndexingStrategy : ILuceneIndexingStrategy
{
    private readonly LuceneWebCrawlerService _webCrawler;
    private readonly LuceneWebScraperSanitizer _sanitizer;

    /// <summary>
    /// Content field name for scraped content.
    /// </summary>
    public const string ContentFieldName = "Content";

    protected BaseMetadataIndexingStrategy(
        LuceneWebCrawlerService webCrawler,
        LuceneWebScraperSanitizer sanitizer)
    {
        _webCrawler = webCrawler;
        _sanitizer = sanitizer;
    }

    /// <inheritdoc />
    public abstract string StrategyName { get; }

    /// <inheritdoc />
    public abstract IReadOnlyList<string> GetContentTypes();

    /// <inheritdoc />
    public virtual bool ShouldIndex(string contentTypeName) =>
        GetContentTypes().Contains(contentTypeName, StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public virtual IReadOnlyList<LuceneFieldDefinition> GetFieldDefinitions() =>
    [
        new() { Name = "Id", FieldType = LuceneFieldType.String, Store = true },
        new() { Name = "ContentItemId", FieldType = LuceneFieldType.Numeric, Store = true },
        new() { Name = "Title", FieldType = LuceneFieldType.Text, Store = true, Boost = 2.0f },
        new() { Name = "Description", FieldType = LuceneFieldType.Text, Store = true, Boost = 1.5f },
        new() { Name = "Keywords", FieldType = LuceneFieldType.Text, Store = true, Boost = 1.8f },
        new() { Name = ContentFieldName, FieldType = LuceneFieldType.Text, Store = true },
        new() { Name = "HtmlContent", FieldType = LuceneFieldType.Text, Store = false },
        new() { Name = "Url", FieldType = LuceneFieldType.String, Store = true },
        new() { Name = "Thumbnail", FieldType = LuceneFieldType.String, Store = true },
        new() { Name = "ContentType", FieldType = LuceneFieldType.String, Store = true },
        new() { Name = "Created", FieldType = LuceneFieldType.Date, Store = true },
        new() { Name = "NoIndex", FieldType = LuceneFieldType.String, Store = true },
        new() { Name = "MemberPermissionOverride", FieldType = LuceneFieldType.String, Store = true },
        new() { Name = "MemberPermissionIsSecure", FieldType = LuceneFieldType.String, Store = true },
        new() { Name = "MemberPermissionRoleTags", FieldType = LuceneFieldType.String, Store = true }
    ];

    /// <inheritdoc />
    public virtual async Task<LuceneIndexDocument?> MapToDocumentAsync(
        IIndexableItem item,
        CancellationToken cancellationToken = default)
    {
        if (!item.IsWebPageItem)
        {
            // Override for reusable content items if needed
            return null;
        }

        // Get metadata from the item
        var metadata = await GetMetadataAsync(item, cancellationToken);
        if (metadata is null)
        {
            return null;
        }

        // Check if noindex
        if (metadata.NoIndex)
        {
            return null;
        }

        var document = new LuceneIndexDocument
        {
            Id = item.ContentItemId.ToString()
        };

        // Add basic fields
        document
            .AddStoredField("ContentItemId", item.ContentItemId)
            .AddTextField("Title", metadata.Title)
            .AddTextField("Description", metadata.Description)
            .AddTextField("Keywords", metadata.Keywords)
            .AddStringField("Url", metadata.CanonicalUrl)
            .AddStringField("Thumbnail", metadata.ThumbnailUrl)
            .AddStringField("ContentType", item.ContentTypeName)
            .AddStringField("NoIndex", metadata.NoIndex.ToString().ToLowerInvariant());

        // Add created date
        if (metadata.Created.HasValue)
        {
            document.AddDateField("Created", metadata.Created.Value);
        }

        // Add permission fields
        document
            .AddStringField("MemberPermissionOverride", metadata.MemberPermissionOverride.ToString())
            .AddStringField("MemberPermissionIsSecure", metadata.MemberPermissionIsSecure.ToString());

        if (metadata.MemberPermissionRoleTags?.Count > 0)
        {
            document.AddStringField("MemberPermissionRoleTags",
                string.Join(";", metadata.MemberPermissionRoleTags));
        }

        // Scrape page content if URL is available
        if (!string.IsNullOrWhiteSpace(metadata.CanonicalUrl))
        {
            var html = await _webCrawler.CrawlPageAsync(metadata.CanonicalUrl, cancellationToken);
            if (!string.IsNullOrWhiteSpace(html))
            {
                var sanitizedContent = await _sanitizer.SanitizeHtmlDocumentAsync(html);
                document.AddTextField("HtmlContent", sanitizedContent, store: false);
            }
        }

        // Set content field (description or scraped content)
        if (!string.IsNullOrWhiteSpace(metadata.Description))
        {
            document.AddTextField(ContentFieldName, metadata.Description);
        }
        else if (document.Fields.TryGetValue("HtmlContent", out var htmlField) &&
                 htmlField.Value is string htmlContent)
        {
            document.AddTextField(ContentFieldName, htmlContent);
        }

        // Apply custom field mappings
        await ApplyCustomFieldsAsync(document, item, metadata, cancellationToken);

        return document;
    }

    /// <summary>
    /// Gets metadata from the indexable item.
    /// </summary>
    protected abstract Task<PageMetadataForIndexing?> GetMetadataAsync(
        IIndexableItem item,
        CancellationToken cancellationToken);

    /// <summary>
    /// Override to add custom fields to the document.
    /// </summary>
    protected virtual Task ApplyCustomFieldsAsync(
        LuceneIndexDocument document,
        IIndexableItem item,
        PageMetadataForIndexing metadata,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

/// <summary>
/// Metadata extracted for indexing.
/// </summary>
public sealed class PageMetadataForIndexing
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Keywords { get; set; }
    public string? CanonicalUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool NoIndex { get; set; }
    public DateTimeOffset? Created { get; set; }
    public bool MemberPermissionOverride { get; set; }
    public bool MemberPermissionIsSecure { get; set; }
    public List<string>? MemberPermissionRoleTags { get; set; }
}
