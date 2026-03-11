namespace Baseline.Search;

/// <summary>
/// Configuration options for Baseline v3 Search module.
/// </summary>
public class BaselineSearchOptions
{
    /// <summary>
    /// The search provider to use.
    /// Default: "Lucene"
    /// </summary>
    public string Provider { get; set; } = "Lucene";

    /// <summary>
    /// Enable search suggestions/autocomplete.
    /// Default: true
    /// </summary>
    public bool EnableSuggestions { get; set; } = true;

    /// <summary>
    /// Enable faceted search.
    /// Default: true
    /// </summary>
    public bool EnableFacets { get; set; } = true;

    /// <summary>
    /// Enable search analytics tracking.
    /// Default: true
    /// </summary>
    public bool EnableAnalytics { get; set; } = true;

    /// <summary>
    /// File path for persisting search analytics data.
    /// Relative paths are resolved against the application content root.
    /// Default: "App_Data/SearchAnalytics"
    /// </summary>
    public string AnalyticsStoragePath { get; set; } = "App_Data/SearchAnalytics";

    /// <summary>
    /// Interval in minutes between analytics flush-to-disk operations.
    /// Default: 5
    /// </summary>
    public int AnalyticsFlushIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Enable AI-powered semantic search.
    /// Default: false
    /// </summary>
    public bool EnableSemanticSearch { get; set; } = false;

    /// <summary>
    /// Default page size for search results.
    /// Default: 10
    /// </summary>
    public int DefaultPageSize { get; set; } = 10;

    /// <summary>
    /// Maximum page size for search results.
    /// Default: 100
    /// </summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>
    /// Minimum query length.
    /// Default: 2
    /// </summary>
    public int MinQueryLength { get; set; } = 2;

    /// <summary>
    /// Lucene search configuration.
    /// </summary>
    public LuceneSearchOptions Lucene { get; set; } = new();

    /// <summary>
    /// Azure Cognitive Search configuration.
    /// </summary>
    public AzureSearchOptions Azure { get; set; } = new();

    /// <summary>
    /// Algolia search configuration.
    /// </summary>
    public AlgoliaSearchOptions Algolia { get; set; } = new();

    /// <summary>
    /// Semantic search configuration.
    /// </summary>
    public SemanticSearchOptions Semantic { get; set; } = new();
}

/// <summary>
/// Lucene search configuration.
/// </summary>
public class LuceneSearchOptions
{
    /// <summary>
    /// Index storage path.
    /// </summary>
    public string IndexPath { get; set; } = "App_Data/LuceneIndex";

    /// <summary>
    /// Analyzer type.
    /// Default: "Standard"
    /// </summary>
    public string Analyzer { get; set; } = "Standard";

    /// <summary>
    /// Enable highlighting in results.
    /// Default: true
    /// </summary>
    public bool EnableHighlighting { get; set; } = true;

    /// <summary>
    /// Number of highlight fragments.
    /// Default: 3
    /// </summary>
    public int HighlightFragments { get; set; } = 3;

    /// <summary>
    /// Fragment size in characters.
    /// Default: 100
    /// </summary>
    public int FragmentSize { get; set; } = 100;

    /// <summary>
    /// Index generation retention policy.
    /// Controls how many rebuild snapshots are kept.
    /// </summary>
    public IndexRetentionPolicy Retention { get; set; } = new();
}

/// <summary>
/// Azure Cognitive Search configuration.
/// </summary>
public class AzureSearchOptions
{
    /// <summary>
    /// Azure Search service endpoint.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Azure Search API key.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Index name.
    /// </summary>
    public string? IndexName { get; set; }

    /// <summary>
    /// Enable semantic ranking.
    /// Default: false
    /// </summary>
    public bool EnableSemanticRanking { get; set; } = false;

    /// <summary>
    /// Semantic configuration name.
    /// </summary>
    public string? SemanticConfigurationName { get; set; }
}

/// <summary>
/// Algolia search configuration.
/// </summary>
public class AlgoliaSearchOptions
{
    /// <summary>
    /// Algolia Application ID.
    /// </summary>
    public string? ApplicationId { get; set; }

    /// <summary>
    /// Algolia API key (search-only).
    /// </summary>
    public string? SearchApiKey { get; set; }

    /// <summary>
    /// Algolia Admin API key (for indexing).
    /// </summary>
    public string? AdminApiKey { get; set; }

    /// <summary>
    /// Index name.
    /// </summary>
    public string? IndexName { get; set; }
}

/// <summary>
/// Semantic search configuration.
/// </summary>
public class SemanticSearchOptions
{
    /// <summary>
    /// Embedding model to use.
    /// Default: "text-embedding-3-small"
    /// </summary>
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    /// <summary>
    /// Vector database provider.
    /// </summary>
    public string VectorProvider { get; set; } = "InMemory";

    /// <summary>
    /// Similarity threshold (0.0 - 1.0).
    /// Default: 0.7
    /// </summary>
    public double SimilarityThreshold { get; set; } = 0.7;

    /// <summary>
    /// Minimum similarity threshold for results.
    /// Default: 0.5
    /// </summary>
    public double MinSimilarityThreshold { get; set; } = 0.5;

    /// <summary>
    /// Number of results to retrieve from vector search.
    /// Default: 20
    /// </summary>
    public int TopK { get; set; } = 20;

    /// <summary>
    /// Embedding dimensions.
    /// Default: 1536 (for OpenAI text-embedding-3-small)
    /// </summary>
    public int EmbeddingDimensions { get; set; } = 1536;

    /// <summary>
    /// Enable hybrid search (combine semantic + keyword).
    /// Default: true
    /// </summary>
    public bool EnableHybridSearch { get; set; } = true;

    /// <summary>
    /// Weight for semantic search in hybrid mode (0.0 - 1.0).
    /// Higher values favor semantic results.
    /// Default: 0.6
    /// </summary>
    public double SemanticWeight { get; set; } = 0.6;

    /// <summary>
    /// Embedding provider type: "AzureOpenAI", "OpenAI", or "Pseudo" (demo).
    /// Default: "Pseudo"
    /// </summary>
    public string EmbeddingProvider { get; set; } = "Pseudo";

    /// <summary>
    /// Azure OpenAI endpoint (e.g. https://myinstance.openai.azure.com/).
    /// Required when EmbeddingProvider == "AzureOpenAI".
    /// </summary>
    public string? AzureOpenAIEndpoint { get; set; }

    /// <summary>
    /// Azure OpenAI API key.
    /// Required when EmbeddingProvider == "AzureOpenAI".
    /// </summary>
    public string? AzureOpenAIApiKey { get; set; }

    /// <summary>
    /// Azure OpenAI deployment name for the embedding model.
    /// Required when EmbeddingProvider == "AzureOpenAI".
    /// </summary>
    public string? AzureOpenAIDeployment { get; set; }

    /// <summary>
    /// OpenAI API key (for direct OpenAI, not Azure).
    /// Required when EmbeddingProvider == "OpenAI".
    /// </summary>
    public string? OpenAIApiKey { get; set; }

    /// <summary>
    /// Path for persisting vector embeddings to disk.
    /// Default: "App_Data/SearchVectors"
    /// </summary>
    public string VectorStoragePath { get; set; } = "App_Data/SearchVectors";
}
