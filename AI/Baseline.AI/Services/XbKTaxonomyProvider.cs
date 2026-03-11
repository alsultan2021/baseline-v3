using CMS.ContentEngine;
using CMS.DataEngine;
using Microsoft.Extensions.Logging;
using KenticoTag = CMS.ContentEngine.Tag;
using KenticoTaxonomyInfo = CMS.ContentEngine.TaxonomyInfo;
using BaselineTaxonomyInfo = Baseline.AI.TaxonomyInfo;

namespace Baseline.AI.Services;

/// <summary>
/// XbK implementation of ITaxonomyProvider.
/// Retrieves taxonomy information from Xperience by Kentico using ITaxonomyRetriever.
/// </summary>
public sealed class XbKTaxonomyProvider(
    ITaxonomyRetriever taxonomyRetriever,
    IInfoProvider<KenticoTaxonomyInfo> taxonomyInfoProvider,
    ILogger<XbKTaxonomyProvider> logger) : ITaxonomyProvider
{
    private readonly ITaxonomyRetriever _taxonomyRetriever = taxonomyRetriever;
    private readonly IInfoProvider<KenticoTaxonomyInfo> _taxonomyInfoProvider = taxonomyInfoProvider;
    private readonly ILogger<XbKTaxonomyProvider> _logger = logger;

    // Default language for taxonomy operations (can be configured)
    private const string DEFAULT_LANGUAGE = "en";

    /// <inheritdoc />
    public async Task<IReadOnlyList<BaselineTaxonomyInfo>> GetTaxonomiesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var taxonomies = await _taxonomyInfoProvider.Get()
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            var result = new List<BaselineTaxonomyInfo>();
            foreach (var taxonomy in taxonomies)
            {
                result.Add(new BaselineTaxonomyInfo
                {
                    Name = taxonomy.TaxonomyName,
                    DisplayName = taxonomy.TaxonomyTitle
                });
            }

            _logger.LogDebug("Retrieved {Count} taxonomies from XbK", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve taxonomies from XbK");
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TagInfo>> GetTagsAsync(
        string taxonomyName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var taxonomy = await _taxonomyRetriever.RetrieveTaxonomy(
                taxonomyName,
                DEFAULT_LANGUAGE,
                cancellationToken);

            if (taxonomy == null)
            {
                _logger.LogWarning("Taxonomy not found: {TaxonomyName}", taxonomyName);
                return [];
            }

            var tags = GetTagsFromTaxonomy(taxonomy.Tags);
            _logger.LogDebug("Retrieved {Count} tags from taxonomy: {TaxonomyName}", tags.Count, taxonomyName);
            return tags;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve tags for taxonomy: {TaxonomyName}", taxonomyName);
            return [];
        }
    }

    /// <summary>
    /// Extracts tags from taxonomy data.
    /// XbK returns a flat list of tags with ParentID for hierarchy.
    /// </summary>
    private static List<TagInfo> GetTagsFromTaxonomy(IEnumerable<KenticoTag> tags)
    {
        var result = new List<TagInfo>();

        foreach (var tag in tags)
        {
            result.Add(new TagInfo
            {
                TagGuid = tag.Identifier,
                Name = tag.Title,
                Description = null // XbK Tag doesn't expose description in this API
            });
        }

        return result;
    }
}
