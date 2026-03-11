using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Core;

/// <summary>
/// Enhanced implementation of <see cref="ILlmsTxtService"/> with GEO optimization support.
/// </summary>
internal sealed class LlmsTxtService(
    IOptions<LlmsTxtOptions> options,
    ILogger<LlmsTxtService> logger,
    ILlmsTxtContentProvider? contentProvider = null) : ILlmsTxtService
{
    private readonly LlmsTxtOptions _options = options.Value;

    public async Task<string> GenerateAsync()
    {
        var sb = new StringBuilder();

        // Site header with enhanced metadata
        GenerateSiteHeader(sb);

        // Generate capabilities section for AI understanding
        if (_options.EnableCapabilitiesSection)
        {
            GenerateCapabilitiesSection(sb);
        }

        // Contact section
        GenerateContactSection(sb);

        // Auto-discovered content from provider
        if (_options.AutoDiscoverPages && contentProvider != null)
        {
            await GenerateDiscoveredContentAsync(sb);
        }

        // Custom sections
        GenerateCustomSections(sb);

        // API documentation section
        if (_options.ApiEndpoints.Count > 0)
        {
            GenerateApiSection(sb);
        }

        // Vector index information for RAG
        if (_options.EnableVectorIndex && !string.IsNullOrEmpty(_options.VectorIndexUrl))
        {
            GenerateVectorIndexSection(sb);
        }

        // Licensing and usage terms
        if (!string.IsNullOrEmpty(_options.LicenseInfo))
        {
            GenerateLicenseSection(sb);
        }

        logger.LogDebug("Generated llms.txt with {Sections} custom sections", _options.Sections.Count);

        return sb.ToString().TrimEnd();
    }

    private void GenerateSiteHeader(StringBuilder sb)
    {
        if (!string.IsNullOrEmpty(_options.SiteName))
        {
            sb.AppendLine($"# {_options.SiteName}");
        }

        // Version info for AI model understanding
        if (!string.IsNullOrEmpty(_options.Version))
        {
            sb.AppendLine($"> Version: {_options.Version}");
        }

        if (!string.IsNullOrEmpty(_options.LastUpdated))
        {
            sb.AppendLine($"> Last Updated: {_options.LastUpdated}");
        }

        sb.AppendLine();

        if (!string.IsNullOrEmpty(_options.SiteDescription))
        {
            sb.AppendLine(_options.SiteDescription);
            sb.AppendLine();
        }

        // Primary topics for better AI categorization
        if (_options.PrimaryTopics.Count > 0)
        {
            sb.AppendLine("**Primary Topics**: " + string.Join(", ", _options.PrimaryTopics));
            sb.AppendLine();
        }

        // Target audience
        if (!string.IsNullOrEmpty(_options.TargetAudience))
        {
            sb.AppendLine($"**Target Audience**: {_options.TargetAudience}");
            sb.AppendLine();
        }
    }

    private void GenerateCapabilitiesSection(StringBuilder sb)
    {
        sb.AppendLine("## Capabilities");
        sb.AppendLine();

        if (_options.SupportedLanguages.Count > 0)
        {
            sb.AppendLine($"- **Languages**: {string.Join(", ", _options.SupportedLanguages)}");
        }

        if (_options.ContentFormats.Count > 0)
        {
            sb.AppendLine($"- **Content Formats**: {string.Join(", ", _options.ContentFormats)}");
        }

        if (_options.Features.Count > 0)
        {
            sb.AppendLine("- **Features**:");
            foreach (var feature in _options.Features)
            {
                sb.AppendLine($"  - {feature}");
            }
        }

        sb.AppendLine();
    }

    private void GenerateContactSection(StringBuilder sb)
    {
        if (string.IsNullOrEmpty(_options.ContactEmail) && string.IsNullOrEmpty(_options.ContactUrl))
        {
            return;
        }

        sb.AppendLine("## Contact");
        if (!string.IsNullOrEmpty(_options.ContactEmail))
        {
            sb.AppendLine($"- Email: {_options.ContactEmail}");
        }
        if (!string.IsNullOrEmpty(_options.ContactUrl))
        {
            sb.AppendLine($"- URL: {_options.ContactUrl}");
        }
        if (!string.IsNullOrEmpty(_options.SupportUrl))
        {
            sb.AppendLine($"- Support: {_options.SupportUrl}");
        }
        sb.AppendLine();
    }

    private async Task GenerateDiscoveredContentAsync(StringBuilder sb)
    {
        if (contentProvider == null)
        {
            return;
        }

        var pages = await contentProvider.GetContentPagesAsync(_options.MaxPages);
        if (!pages.Any())
        {
            return;
        }

        sb.AppendLine("## Content Index");
        sb.AppendLine();

        // Group by content type
        var groupedPages = pages
            .GroupBy(p => p.ContentType)
            .OrderBy(g => g.Key);

        foreach (var group in groupedPages)
        {
            sb.AppendLine($"### {group.Key}");
            sb.AppendLine();

            foreach (var page in group.Take(20)) // Limit per type
            {
                sb.AppendLine($"- [{page.Title}]({page.Url})");
                if (!string.IsNullOrEmpty(page.Summary))
                {
                    sb.AppendLine($"  {page.Summary}");
                }
            }
            sb.AppendLine();
        }
    }

    private void GenerateCustomSections(StringBuilder sb)
    {
        foreach (var section in _options.Sections)
        {
            sb.AppendLine($"## {section.Title}");
            if (!string.IsNullOrEmpty(section.Content))
            {
                sb.AppendLine(section.Content);
            }
            foreach (var link in section.Links)
            {
                sb.AppendLine($"- {link}");
            }
            sb.AppendLine();
        }
    }

    private void GenerateApiSection(StringBuilder sb)
    {
        sb.AppendLine("## API Endpoints");
        sb.AppendLine();

        foreach (var endpoint in _options.ApiEndpoints)
        {
            sb.AppendLine($"### {endpoint.Name}");
            sb.AppendLine($"- **URL**: `{endpoint.Url}`");
            sb.AppendLine($"- **Method**: {endpoint.Method}");
            if (!string.IsNullOrEmpty(endpoint.Description))
            {
                sb.AppendLine($"- **Description**: {endpoint.Description}");
            }
            if (!string.IsNullOrEmpty(endpoint.Authentication))
            {
                sb.AppendLine($"- **Authentication**: {endpoint.Authentication}");
            }
            sb.AppendLine();
        }
    }

    private void GenerateVectorIndexSection(StringBuilder sb)
    {
        sb.AppendLine("## Vector Index (RAG Support)");
        sb.AppendLine();
        sb.AppendLine($"For AI assistants that support RAG (Retrieval-Augmented Generation), a vector index is available:");
        sb.AppendLine();
        sb.AppendLine($"- **Index URL**: {_options.VectorIndexUrl}");
        if (!string.IsNullOrEmpty(_options.VectorIndexFormat))
        {
            sb.AppendLine($"- **Format**: {_options.VectorIndexFormat}");
        }
        if (_options.VectorDimensions > 0)
        {
            sb.AppendLine($"- **Dimensions**: {_options.VectorDimensions}");
        }
        if (!string.IsNullOrEmpty(_options.EmbeddingModel))
        {
            sb.AppendLine($"- **Embedding Model**: {_options.EmbeddingModel}");
        }
        sb.AppendLine();
    }

    private void GenerateLicenseSection(StringBuilder sb)
    {
        sb.AppendLine("## Terms of Use");
        sb.AppendLine();
        sb.AppendLine(_options.LicenseInfo);
        sb.AppendLine();

        if (_options.AllowedUseCases.Count > 0)
        {
            sb.AppendLine("**Allowed Use Cases**:");
            foreach (var useCase in _options.AllowedUseCases)
            {
                sb.AppendLine($"- {useCase}");
            }
            sb.AppendLine();
        }

        if (_options.RestrictedUseCases.Count > 0)
        {
            sb.AppendLine("**Restricted Use Cases**:");
            foreach (var useCase in _options.RestrictedUseCases)
            {
                sb.AppendLine($"- {useCase}");
            }
            sb.AppendLine();
        }
    }
}

/// <summary>
/// Provider for dynamically discovering content pages for llms.txt.
/// </summary>
public interface ILlmsTxtContentProvider
{
    /// <summary>
    /// Gets content pages to include in llms.txt.
    /// </summary>
    /// <param name="maxPages">Maximum number of pages to return.</param>
    /// <returns>Collection of content page information.</returns>
    Task<IEnumerable<LlmsTxtContentPage>> GetContentPagesAsync(int maxPages);
}

/// <summary>
/// Content page information for llms.txt.
/// </summary>
public sealed record LlmsTxtContentPage
{
    /// <summary>Page title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Page URL.</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>Content type (e.g., "Blog", "Product", "Documentation").</summary>
    public string ContentType { get; init; } = string.Empty;

    /// <summary>Brief summary of the page content.</summary>
    public string? Summary { get; init; }

    /// <summary>Last modified date.</summary>
    public DateTimeOffset? LastModified { get; init; }

    /// <summary>Primary topics/keywords.</summary>
    public IReadOnlyList<string> Topics { get; init; } = [];
}

/// <summary>
/// API endpoint information for llms.txt.
/// </summary>
public sealed record LlmsTxtApiEndpoint
{
    /// <summary>Endpoint name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Endpoint URL.</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>HTTP method (GET, POST, etc.).</summary>
    public string Method { get; init; } = "GET";

    /// <summary>Endpoint description.</summary>
    public string? Description { get; init; }

    /// <summary>Authentication type required.</summary>
    public string? Authentication { get; init; }
}
