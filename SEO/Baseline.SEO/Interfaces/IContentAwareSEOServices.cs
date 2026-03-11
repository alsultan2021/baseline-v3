namespace Baseline.SEO;

/// <summary>
/// Kentico content-aware extension of <see cref="IGEOOptimizationService"/>.
/// Adds overloads that accept Kentico content item IDs and web page GUIDs,
/// enabling automatic content extraction, cache dependency integration,
/// and content-type-aware analysis.
/// <para>
/// Implement this interface at the site level where Kentico packages are available.
/// The Baseline.SEO module itself has no Kentico dependency by design.
/// </para>
/// </summary>
/// <example>
/// <code>
/// public class KenticoGEOService(
///     IGEOOptimizationService baseService,
///     IContentRetriever contentRetriever) : IContentAwareGEOService
/// {
///     // Delegate base methods to the wrapped service
///     public Task&lt;GEOAnalysis&gt; AnalyzeContentAsync(string content, CancellationToken ct)
///         =&gt; baseService.AnalyzeContentAsync(content, ct);
///
///     // Kentico-aware overload
///     public async Task&lt;GEOAnalysis&gt; AnalyzeContentItemAsync(int contentItemId, CancellationToken ct)
///     {
///         var page = await contentRetriever.RetrievePages&lt;IWebPageFieldsSource&gt;(...);
///         var html = ExtractContent(page);
///         return await baseService.AnalyzeContentAsync(html, ct);
///     }
/// }
/// </code>
/// </example>
public interface IContentAwareGEOService : IGEOOptimizationService
{
    /// <summary>
    /// Analyzes a Kentico content item for GEO optimization.
    /// Extracts content from the content item automatically.
    /// </summary>
    /// <param name="contentItemId">Kentico content item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<GEOAnalysis> AnalyzeContentItemAsync(
        int contentItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes a web page by its GUID.
    /// Handles content retrieval, URL resolution, and cache dependencies.
    /// </summary>
    /// <param name="webPageGuid">Web page item GUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<GEOAnalysis> AnalyzeWebPageAsync(
        Guid webPageGuid, CancellationToken cancellationToken = default);
}

/// <summary>
/// Kentico content-aware extension of <see cref="IAnswerEngineService"/>.
/// Adds overloads that extract FAQs, HowTo, and structured data from 
/// Kentico content items via <c>IContentRetriever</c>.
/// </summary>
public interface IContentAwareAnswerEngineService : IAnswerEngineService
{
    /// <summary>
    /// Extracts FAQ content from a Kentico content item.
    /// </summary>
    /// <param name="contentItemId">Kentico content item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<FAQPage> ExtractFAQsFromContentItemAsync(
        int contentItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates all applicable structured data for a Kentico content item.
    /// Combines content-type metadata with AI extraction.
    /// </summary>
    /// <param name="contentItemId">Kentico content item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<StructuredDataCollection> GenerateStructuredDataForContentItemAsync(
        int contentItemId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Kentico content-aware extension of <see cref="ISEOAuditService"/>.
/// Adds overloads that audit Kentico web pages using content item data
/// and metadata schemas (e.g., <c>IBaseMetadata</c>).
/// </summary>
public interface IContentAwareSEOAuditService : ISEOAuditService
{
    /// <summary>
    /// Audits a Kentico web page using its content item ID.
    /// Automatically retrieves page metadata, content fields, and URL.
    /// </summary>
    /// <param name="contentItemId">Kentico content item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<SEOAuditResult> AuditContentItemAsync(
        int contentItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Audits all web pages of a given content type.
    /// </summary>
    /// <param name="contentTypeName">Kentico content type code name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<SiteAuditResult> AuditContentTypeAsync(
        string contentTypeName, CancellationToken cancellationToken = default);
}
