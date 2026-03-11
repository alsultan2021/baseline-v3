using Baseline.Ecommerce.Interfaces;
using CMS.ContentEngine;
using Tag = CMS.ContentEngine.Tag;

namespace Baseline.Ecommerce.Services;

/// <summary>
/// Default implementation for retrieving tag titles from taxonomy.
/// </summary>
/// <param name="taxonomyRetriever">The taxonomy retriever service.</param>
public sealed class TagTitleRetriever(ITaxonomyRetriever taxonomyRetriever) : ITagTitleRetriever
{
    /// <inheritdoc />
    public async Task<string?> GetTagTitleAsync(Guid tagIdentifier, string languageName, CancellationToken cancellationToken = default)
    {
        var tags = await taxonomyRetriever.RetrieveTags([tagIdentifier], languageName, cancellationToken);
        return tags.FirstOrDefault()?.Title;
    }

    /// <inheritdoc />
    public async Task<CSharpFunctionalExtensions.Result<Tag>> GetTagByIdentifierAsync(string tagIdentifier, string languageCode, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(tagIdentifier, out Guid guid))
        {
            return CSharpFunctionalExtensions.Result.Failure<Tag>("Invalid tag identifier");
        }

        var tags = await taxonomyRetriever.RetrieveTags([guid], languageCode, cancellationToken);
        var tag = tags.FirstOrDefault();

        return tag != null
            ? CSharpFunctionalExtensions.Result.Success(tag)
            : CSharpFunctionalExtensions.Result.Failure<Tag>("Tag not found");
    }

    /// <inheritdoc />
    public async Task<CSharpFunctionalExtensions.Result<IReadOnlyList<Tag>>> GetTagsByIdentifiersAsync(IEnumerable<string> tagIdentifiers, string languageCode, CancellationToken cancellationToken = default)
    {
        var guids = tagIdentifiers
            .Select(id => Guid.TryParse(id, out Guid guid) ? guid : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList();

        if (guids.Count == 0)
        {
            return CSharpFunctionalExtensions.Result.Success<IReadOnlyList<Tag>>([]);
        }

        var tags = await taxonomyRetriever.RetrieveTags(guids, languageCode, cancellationToken);
        return CSharpFunctionalExtensions.Result.Success<IReadOnlyList<Tag>>(tags.ToList());
    }
}
