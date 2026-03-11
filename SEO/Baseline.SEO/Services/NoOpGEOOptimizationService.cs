using Microsoft.Extensions.Logging;

namespace Baseline.SEO;

/// <summary>
/// No-op default implementation of <see cref="IGEOOptimizationService"/>.
/// Returns neutral/empty results. Override via DI to provide AI-backed analysis.
/// </summary>
public class NoOpGEOOptimizationService(ILogger<NoOpGEOOptimizationService> logger)
    : IGEOOptimizationService
{
    /// <inheritdoc/>
    public Task<GEOAnalysis> AnalyzeContentAsync(string content, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("NoOp GEO: AnalyzeContentAsync called — returning empty analysis");
        return Task.FromResult(new GEOAnalysis { Score = 0 });
    }

    /// <inheritdoc/>
    public Task<GEOAnalysis> AnalyzePageAsync(string url, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("NoOp GEO: AnalyzePageAsync called for {Url}", url);
        return Task.FromResult(new GEOAnalysis { Score = 0 });
    }

    /// <inheritdoc/>
    public Task<IEnumerable<GEOSuggestion>> GetSuggestionsAsync(string content, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("NoOp GEO: GetSuggestionsAsync called — returning empty");
        return Task.FromResult(Enumerable.Empty<GEOSuggestion>());
    }

    /// <inheritdoc/>
    public Task<string> GenerateAISummaryAsync(string content, int maxTokens = 200, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("NoOp GEO: GenerateAISummaryAsync called — returning empty");
        return Task.FromResult(string.Empty);
    }

    /// <inheritdoc/>
    public Task<CitabilityResult> CheckCitabilityAsync(string content, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("NoOp GEO: CheckCitabilityAsync called — returning uncitable");
        return Task.FromResult(new CitabilityResult { IsCitable = false, Score = 0 });
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ConceptVariation>> GenerateConceptVariationsAsync(string content, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("NoOp GEO: GenerateConceptVariationsAsync called — returning empty");
        return Task.FromResult(Enumerable.Empty<ConceptVariation>());
    }
}
