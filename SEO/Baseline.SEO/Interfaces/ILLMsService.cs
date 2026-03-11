namespace Baseline.SEO;

/// <summary>
/// Service for generating and managing LLMs.txt files.
/// LLMs.txt helps AI crawlers and language models understand your site content.
/// Similar to robots.txt but optimized for AI consumption.
/// </summary>
/// <remarks>
/// <b>Core overlap:</b> Baseline.Core's <c>ILlmsTxtService</c> provides a simple
/// <c>GenerateAsync() → string</c> contract for the /llms.txt endpoint. This interface
/// is a <i>superset</i> adding content indexing, vector endpoints, section providers,
/// LLMs-full.txt, and validation. Consuming projects typically implement Core's
/// <c>ILlmsTxtService</c> by delegating to this richer interface.
/// See <see cref="ICoreSEOBridge"/> for bridging patterns.
/// </remarks>
public interface ILLMsService
{
    /// <summary>
    /// Generates the LLMs.txt file content.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>LLMs.txt content.</returns>
    Task<string> GenerateLLMsTxtAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates LLMs-full.txt with comprehensive content.
    /// Extended version with more detail for AI training.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>LLMs-full.txt content.</returns>
    Task<string> GenerateLLMsFullTxtAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the content index for AI consumption.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Content index.</returns>
    Task<LLMsContentIndex> GetContentIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the vector search endpoint information.
    /// </summary>
    /// <returns>Vector endpoint details.</returns>
    VectorEndpoint? GetVectorEndpoint();

    /// <summary>
    /// Registers a custom section provider.
    /// </summary>
    /// <param name="provider">The section provider.</param>
    void RegisterSectionProvider(ILLMsSectionProvider provider);

    /// <summary>
    /// Validates LLMs.txt content.
    /// </summary>
    /// <param name="content">The content to validate.</param>
    /// <returns>Validation result.</returns>
    LLMsValidation ValidateLLMsTxt(string content);
}

/// <summary>
/// Content index for AI consumption.
/// </summary>
public record LLMsContentIndex
{
    /// <summary>
    /// Site information.
    /// </summary>
    public required SiteInfo Site { get; init; }

    /// <summary>
    /// Content categories.
    /// </summary>
    public IReadOnlyList<ContentCategory> Categories { get; init; } = [];

    /// <summary>
    /// Content items.
    /// </summary>
    public IReadOnlyList<LLMsContentItem> Items { get; init; } = [];

    /// <summary>
    /// API endpoints for AI access.
    /// </summary>
    public IReadOnlyList<APIEndpoint> Endpoints { get; init; } = [];

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Total items in index.
    /// </summary>
    public int TotalItems { get; init; }
}

/// <summary>
/// Site information for LLMs.txt.
/// </summary>
public record SiteInfo
{
    /// <summary>
    /// Site name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Site description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Base URL.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Primary language.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Available languages.
    /// </summary>
    public IReadOnlyList<string> AvailableLanguages { get; init; } = [];

    /// <summary>
    /// Contact email.
    /// </summary>
    public string? ContactEmail { get; init; }

    /// <summary>
    /// Organization name.
    /// </summary>
    public string? Organization { get; init; }

    /// <summary>
    /// Main topics/categories.
    /// </summary>
    public IReadOnlyList<string> Topics { get; init; } = [];
}

/// <summary>
/// A content category.
/// </summary>
public record ContentCategory
{
    /// <summary>
    /// Category name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Category description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// URL pattern for this category.
    /// </summary>
    public string? UrlPattern { get; init; }

    /// <summary>
    /// Item count in this category.
    /// </summary>
    public int ItemCount { get; init; }

    /// <summary>
    /// Content types in this category.
    /// </summary>
    public IReadOnlyList<string> ContentTypes { get; init; } = [];
}

/// <summary>
/// A content item for LLMs.txt.
/// </summary>
public record LLMsContentItem
{
    /// <summary>
    /// Item title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Item URL.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Brief description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Content type.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Category.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Last modified date.
    /// </summary>
    public DateTime? LastModified { get; init; }

    /// <summary>
    /// Tags/keywords.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Priority (0-1).
    /// </summary>
    public double Priority { get; init; } = 0.5;
}

/// <summary>
/// API endpoint for AI access.
/// </summary>
public record APIEndpoint
{
    /// <summary>
    /// Endpoint name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Endpoint URL.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// HTTP method.
    /// </summary>
    public string Method { get; init; } = "GET";

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Whether authentication is required.
    /// </summary>
    public bool RequiresAuth { get; init; }

    /// <summary>
    /// Endpoint type (search, content, embedding, etc.).
    /// </summary>
    public string? Type { get; init; }
}

/// <summary>
/// Vector search endpoint details.
/// </summary>
public record VectorEndpoint
{
    /// <summary>
    /// Endpoint URL.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Embedding model used.
    /// </summary>
    public string? EmbeddingModel { get; init; }

    /// <summary>
    /// Vector dimensions.
    /// </summary>
    public int? Dimensions { get; init; }

    /// <summary>
    /// Supported query parameters.
    /// </summary>
    public IReadOnlyList<string> QueryParameters { get; init; } = [];

    /// <summary>
    /// Example usage.
    /// </summary>
    public string? Example { get; init; }
}

/// <summary>
/// LLMs.txt validation result.
/// </summary>
public record LLMsValidation
{
    /// <summary>
    /// Whether the content is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Validation errors.
    /// </summary>
    public IReadOnlyList<LLMsValidationError> Errors { get; init; } = [];

    /// <summary>
    /// Validation warnings.
    /// </summary>
    public IReadOnlyList<LLMsValidationWarning> Warnings { get; init; } = [];

    /// <summary>
    /// Parsed sections.
    /// </summary>
    public IReadOnlyList<LLMsSection> Sections { get; init; } = [];
}

/// <summary>
/// A validation error.
/// </summary>
public record LLMsValidationError
{
    public required string Message { get; init; }
    public int? Line { get; init; }
}

/// <summary>
/// A validation warning.
/// </summary>
public record LLMsValidationWarning
{
    public required string Message { get; init; }
    public string? Recommendation { get; init; }
}

/// <summary>
/// A parsed section from LLMs.txt.
/// </summary>
public record LLMsSection
{
    public required string Name { get; init; }
    public required string Content { get; init; }
    public int StartLine { get; init; }
    public int EndLine { get; init; }
}
