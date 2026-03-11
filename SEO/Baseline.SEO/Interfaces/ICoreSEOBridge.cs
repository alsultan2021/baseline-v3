namespace Baseline.SEO;

/// <summary>
/// Optional bridge interface for projects using both Baseline.Core and Baseline.SEO.
/// <para>
/// Baseline.Core provides: <c>IStructuredDataService</c>, <c>IMetaDataService</c>, 
/// <c>ISeoMetadataService</c>, <c>ILlmsTxtService</c>, <c>IRobotsTxtService</c>, 
/// <c>IJsonLdGenerator</c>.
/// </para>
/// <para>
/// Baseline.SEO provides: <c>IGEOOptimizationService</c>, <c>IAnswerEngineService</c>,
/// <c>ISEOAuditService</c>, <c>ILLMsService</c>.
/// </para>
/// <para>
/// The modules are intentionally separate — Core handles standard SEO (meta tags,
/// structured data, robots/llms.txt endpoints) while SEO handles AI-powered optimization
/// (GEO, Answer Engine, auditing, advanced LLMs.txt). Implement this interface at 
/// the site level to bridge the two when needed.
/// </para>
/// </summary>
/// <example>
/// <code>
/// public class SiteSEOBridge(
///     IStructuredDataService coreStructuredData,
///     IAnswerEngineService answerEngine,
///     IMetaDataService coreMetaData) : ICoreSEOBridge
/// {
///     public async Task&lt;string&gt; GenerateCombinedStructuredDataAsync(string content, string url)
///     {
///         var faqData = await answerEngine.ExtractFAQsAsync(content);
///         if (faqData.Questions.Count > 0)
///             return faqData.ToJsonLd();
///         return coreStructuredData.GenerateCustomJsonLd(new() { ["@type"] = "WebPage" });
///     }
/// }
/// </code>
/// </example>
public interface ICoreSEOBridge
{
    /// <summary>
    /// Generates combined structured data from both Core and SEO modules.
    /// Core provides standard schemas (Article, FAQ, Breadcrumb); SEO adds 
    /// AI-extracted FAQs, HowTo, Speakable, and GEO-optimized content.
    /// </summary>
    /// <param name="content">Raw content text.</param>
    /// <param name="url">Page URL for context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Combined JSON-LD string.</returns>
    Task<string> GenerateCombinedStructuredDataAsync(
        string content, string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enriches Core metadata with SEO module analysis.
    /// Combines Core's <c>IMetaDataService</c> page metadata with
    /// SEO module's GEO analysis and Answer Engine insights.
    /// </summary>
    /// <param name="contentItemId">Kentico content item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Enriched metadata dictionary.</returns>
    Task<Dictionary<string, string>> GetEnrichedMetadataAsync(
        int contentItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bridges Core's <c>ILlmsTxtService.GenerateAsync()</c> with
    /// SEO module's richer <c>ILLMsService.GenerateLLMsTxtAsync()</c>.
    /// <para>
    /// Consuming projects typically implement Core's <c>ILlmsTxtService</c> 
    /// to delegate to SEO module's <c>ILLMsService</c> for the endpoint,
    /// while using <c>ILLMsService</c> directly for advanced features 
    /// (content index, vector endpoint, validation).
    /// </para>
    /// </summary>
    Task<string> GenerateLLMsTxtAsync(CancellationToken cancellationToken = default);
}
