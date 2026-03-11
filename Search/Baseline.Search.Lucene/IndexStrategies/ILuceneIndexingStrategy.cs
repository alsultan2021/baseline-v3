namespace Baseline.Search.Lucene;

/// <summary>
/// Interface for Lucene indexing strategies.
/// </summary>
public interface ILuceneIndexingStrategy
{
    /// <summary>
    /// Strategy name for identification.
    /// </summary>
    string StrategyName { get; }

    /// <summary>
    /// Maps a content item to a Lucene document.
    /// </summary>
    /// <param name="item">The content item to index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The indexed document or null to skip indexing.</returns>
    Task<LuceneIndexDocument?> MapToDocumentAsync(
        IIndexableItem item,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the fields to index for this strategy.
    /// </summary>
    IReadOnlyList<LuceneFieldDefinition> GetFieldDefinitions();

    /// <summary>
    /// Gets content type names that this strategy handles.
    /// </summary>
    IReadOnlyList<string> GetContentTypes();

    /// <summary>
    /// Determines if this strategy should index the given content type.
    /// </summary>
    bool ShouldIndex(string contentTypeName);
}

/// <summary>
/// Interface for items that can be indexed.
/// </summary>
public interface IIndexableItem
{
    /// <summary>
    /// Content item ID.
    /// </summary>
    int ContentItemId { get; }

    /// <summary>
    /// Content item GUID.
    /// </summary>
    Guid ContentItemGuid { get; }

    /// <summary>
    /// Content type name.
    /// </summary>
    string ContentTypeName { get; }

    /// <summary>
    /// Language code.
    /// </summary>
    string LanguageCode { get; }

    /// <summary>
    /// Channel name (for web page items).
    /// </summary>
    string? ChannelName { get; }

    /// <summary>
    /// Whether this is a web page item.
    /// </summary>
    bool IsWebPageItem { get; }

    /// <summary>
    /// Gets a field value by name.
    /// </summary>
    object? GetFieldValue(string fieldName);

    /// <summary>
    /// Gets a strongly-typed field value.
    /// </summary>
    T? GetFieldValue<T>(string fieldName);
}

/// <summary>
/// Lucene document for indexing.
/// </summary>
public sealed class LuceneIndexDocument
{
    /// <summary>
    /// Unique document ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Fields in the document.
    /// </summary>
    public Dictionary<string, LuceneFieldValue> Fields { get; set; } = [];

    /// <summary>
    /// Whether this document should be deleted instead of indexed.
    /// </summary>
    public bool ShouldDelete { get; set; }

    /// <summary>
    /// Adds a text field (analyzed for full-text search).
    /// </summary>
    public LuceneIndexDocument AddTextField(string name, string? value, bool store = true)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            Fields[name] = new LuceneFieldValue
            {
                Value = value,
                FieldType = LuceneFieldType.Text,
                Store = store
            };
        }
        return this;
    }

    /// <summary>
    /// Adds a string field (not analyzed, exact match only).
    /// </summary>
    public LuceneIndexDocument AddStringField(string name, string? value, bool store = true)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            Fields[name] = new LuceneFieldValue
            {
                Value = value,
                FieldType = LuceneFieldType.String,
                Store = store
            };
        }
        return this;
    }

    /// <summary>
    /// Adds a stored-only field (not indexed).
    /// </summary>
    public LuceneIndexDocument AddStoredField(string name, object? value)
    {
        if (value is not null)
        {
            Fields[name] = new LuceneFieldValue
            {
                Value = value,
                FieldType = LuceneFieldType.Stored,
                Store = true
            };
        }
        return this;
    }

    /// <summary>
    /// Adds a numeric field.
    /// </summary>
    public LuceneIndexDocument AddNumericField(string name, double value, bool store = true)
    {
        Fields[name] = new LuceneFieldValue
        {
            Value = value,
            FieldType = LuceneFieldType.Numeric,
            Store = store
        };
        return this;
    }

    /// <summary>
    /// Adds a date field.
    /// </summary>
    public LuceneIndexDocument AddDateField(string name, DateTimeOffset value, bool store = true)
    {
        Fields[name] = new LuceneFieldValue
        {
            Value = value,
            FieldType = LuceneFieldType.Date,
            Store = store
        };
        return this;
    }
}

/// <summary>
/// Lucene field value.
/// </summary>
public sealed class LuceneFieldValue
{
    /// <summary>
    /// The field value.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// The field type.
    /// </summary>
    public LuceneFieldType FieldType { get; set; }

    /// <summary>
    /// Whether to store the field value.
    /// </summary>
    public bool Store { get; set; }

    /// <summary>
    /// Optional boost value for relevance.
    /// </summary>
    public float Boost { get; set; } = 1.0f;
}

/// <summary>
/// Lucene field types.
/// </summary>
public enum LuceneFieldType
{
    /// <summary>
    /// Text field (analyzed).
    /// </summary>
    Text,

    /// <summary>
    /// String field (not analyzed).
    /// </summary>
    String,

    /// <summary>
    /// Stored only (not indexed).
    /// </summary>
    Stored,

    /// <summary>
    /// Numeric field.
    /// </summary>
    Numeric,

    /// <summary>
    /// Date field.
    /// </summary>
    Date
}

/// <summary>
/// Field definition for an index.
/// </summary>
public sealed class LuceneFieldDefinition
{
    /// <summary>
    /// Field name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Field type.
    /// </summary>
    public LuceneFieldType FieldType { get; set; }

    /// <summary>
    /// Whether to store the field value.
    /// </summary>
    public bool Store { get; set; } = true;

    /// <summary>
    /// Boost value for relevance.
    /// </summary>
    public float Boost { get; set; } = 1.0f;

    /// <summary>
    /// Analyzer name.
    /// </summary>
    public string? Analyzer { get; set; }
}
