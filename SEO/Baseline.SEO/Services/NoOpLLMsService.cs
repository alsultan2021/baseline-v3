using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.SEO;

/// <summary>
/// No-op default implementation of <see cref="ILLMsService"/>.
/// Returns minimal valid LLMs.txt content. Override via DI for full generation.
/// </summary>
public class NoOpLLMsService(
    IOptions<BaselineSEOOptions> options,
    ILogger<NoOpLLMsService> logger) : ILLMsService
{
    private readonly LLMsOptions _llmsOptions = options.Value.LLMs;
    private readonly List<ILLMsSectionProvider> _sectionProviders = [];

    /// <inheritdoc/>
    public async Task<string> GenerateLLMsTxtAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("NoOp LLMs: GenerateLLMsTxtAsync called — returning minimal content");

        var sections = new List<string>
        {
            "# LLMs.txt",
            "",
            "> This site uses Baseline SEO module. Register a custom ILLMsService for full content.",
        };

        // Include registered section providers
        foreach (var provider in _sectionProviders.OrderByDescending(p => p.Priority))
        {
            var content = await provider.GenerateSectionAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(content))
            {
                sections.Add("");
                sections.Add($"## {provider.SectionName}");
                sections.Add(content);
            }
        }

        return string.Join(Environment.NewLine, sections);
    }

    /// <inheritdoc/>
    public Task<string> GenerateLLMsFullTxtAsync(CancellationToken cancellationToken = default) =>
        GenerateLLMsTxtAsync(cancellationToken);

    /// <inheritdoc/>
    public Task<LLMsContentIndex> GetContentIndexAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("NoOp LLMs: GetContentIndexAsync called — returning empty index");
        return Task.FromResult(new LLMsContentIndex
        {
            Site = new SiteInfo
            {
                Name = _llmsOptions.FallbackCompanyName,
                Description = "No content index configured.",
                Url = _llmsOptions.BaseUrlOverride ?? string.Empty
            },
            TotalItems = 0
        });
    }

    /// <inheritdoc/>
    public VectorEndpoint? GetVectorEndpoint()
    {
        if (!_llmsOptions.EnableVectorIndex || string.IsNullOrEmpty(_llmsOptions.VectorSearchEndpoint))
            return null;

        return new VectorEndpoint { Url = _llmsOptions.VectorSearchEndpoint };
    }

    /// <inheritdoc/>
    public void RegisterSectionProvider(ILLMsSectionProvider provider) =>
        _sectionProviders.Add(provider);

    /// <inheritdoc/>
    public LLMsValidation ValidateLLMsTxt(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return new LLMsValidation
            {
                IsValid = false,
                Errors = [new LLMsValidationError { Message = "Content is empty." }]
            };

        return new LLMsValidation { IsValid = true };
    }
}
