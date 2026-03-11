using Microsoft.Extensions.Logging;

namespace Baseline.SEO;

/// <summary>
/// No-op default implementation of <see cref="ISEOAuditService"/>.
/// Returns neutral/empty results. Override via DI to provide real audit capabilities.
/// </summary>
public class NoOpSEOAuditService(ILogger<NoOpSEOAuditService> logger) : ISEOAuditService
{
    /// <inheritdoc/>
    public Task<SEOAuditResult> AuditPageAsync(string url, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("NoOp SEOAudit: AuditPageAsync called for {Url}", url);
        return Task.FromResult(new SEOAuditResult { Url = url, Score = 0 });
    }

    /// <inheritdoc/>
    public Task<SEOAuditResult> AuditHtmlAsync(string html, string url, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("NoOp SEOAudit: AuditHtmlAsync called for {Url}", url);
        return Task.FromResult(new SEOAuditResult { Url = url, Score = 0 });
    }

    /// <inheritdoc/>
    public Task<SiteAuditResult> AuditSiteAsync(IEnumerable<string>? urls = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("NoOp SEOAudit: AuditSiteAsync called — returning empty");
        return Task.FromResult(new SiteAuditResult());
    }

    /// <inheritdoc/>
    public Task<SEOReport> GenerateReportAsync(DateTime? since = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("NoOp SEOAudit: GenerateReportAsync called — returning empty");
        return Task.FromResult(new SEOReport { Summary = "No audit data available. Register a real ISEOAuditService implementation." });
    }

    /// <inheritdoc/>
    public Task<IEnumerable<SEOScoreHistory>> GetScoreHistoryAsync(
        string url, int count = 30, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("NoOp SEOAudit: GetScoreHistoryAsync called — returning empty");
        return Task.FromResult(Enumerable.Empty<SEOScoreHistory>());
    }

    /// <inheritdoc/>
    public Task<StructuredDataValidation> ValidateStructuredDataAsync(
        string url, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("NoOp SEOAudit: ValidateStructuredDataAsync called for {Url}", url);
        return Task.FromResult(new StructuredDataValidation { Url = url, IsValid = true });
    }

    /// <inheritdoc/>
    public Task<CoreWebVitals> CheckCoreWebVitalsAsync(string url, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("NoOp SEOAudit: CheckCoreWebVitalsAsync called for {Url}", url);
        return Task.FromResult(new CoreWebVitals());
    }
}
