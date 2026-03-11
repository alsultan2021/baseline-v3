using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.AI.Services;

/// <summary>
/// XbK implementation of IContentProvider.
/// Provides content information from Xperience by Kentico for auto-tagging.
/// 
/// Note: This implementation provides basic content provider capabilities.
/// For comprehensive content extraction and tagging, use the direct
/// SuggestTagsAsync(ContentToTag content, ...) method by constructing
/// ContentToTag from your content-type specific code.
/// </summary>
/// <remarks>
/// XbK content retrieval requires knowing the content type at compile time.
/// For dynamic content type handling, the recommended approach is:
/// 1. Use SuggestTagsAsync with a pre-built ContentToTag object
/// 2. Extract content fields in your page template/controller
/// 3. Pass the extracted content to the auto-tagging service
/// </remarks>
public sealed class XbKContentProvider(
    IOptions<BaselineAIOptions> options,
    ILogger<XbKContentProvider> logger) : IContentProvider
{
    private readonly BaselineAIOptions _options = options.Value;
    private readonly ILogger<XbKContentProvider> _logger = logger;

    /// <inheritdoc />
    public Task<ContentToTag?> GetContentForTaggingAsync(
        Guid contentItemGuid,
        CancellationToken cancellationToken = default)
    {
        // XbK's IContentRetriever/IContentQueryExecutor require knowing the content type
        // at compile time. For generic auto-tagging across all content types,
        // callers should construct ContentToTag directly and use SuggestTagsAsync.
        _logger.LogDebug(
            "GetContentForTaggingAsync called for {ContentItemGuid}. " +
            "For dynamic content types, use SuggestTagsAsync with ContentToTag directly.",
            contentItemGuid);

        return Task.FromResult<ContentToTag?>(null);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Guid>> GetExistingTagsAsync(
        Guid contentItemGuid,
        CancellationToken cancellationToken = default)
    {
        // Getting existing tags requires knowing which taxonomy fields exist
        // on the specific content type. This is content-type specific.
        _logger.LogDebug(
            "GetExistingTagsAsync called for {ContentItemGuid}. " +
            "Requires content-type specific implementation.",
            contentItemGuid);

        return Task.FromResult<IEnumerable<Guid>>([]);
    }

    /// <inheritdoc />
    public Task ApplyTagsAsync(
        Guid contentItemGuid,
        IEnumerable<Guid> tagGuids,
        CancellationToken cancellationToken = default)
    {
        // Applying tags requires IContentItemManagerFactory and knowing
        // which taxonomy field to update. Log for now.
        _logger.LogInformation(
            "ApplyTagsAsync called for content item {ContentItemGuid} with {TagCount} tags. " +
            "Tag application requires content-type specific implementation.",
            contentItemGuid, tagGuids.Count());

        return Task.CompletedTask;
    }
}
