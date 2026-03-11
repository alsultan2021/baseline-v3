namespace Baseline.Ecommerce.Interfaces;

/// <summary>
/// Service for retrieving tag titles from taxonomy.
/// </summary>
public interface ITagTitleRetriever
{
    /// <summary>
    /// Get title of a tag based on the tag identifier.
    /// </summary>
    /// <param name="tagIdentifier">The tag GUID identifier.</param>
    /// <param name="languageName">The language name for localization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tag title or null if not found.</returns>
    Task<string?> GetTagTitleAsync(Guid tagIdentifier, string languageName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a tag by its identifier.
    /// </summary>
    /// <param name="tagIdentifier">The tag identifier as string (GUID format).</param>
    /// <param name="languageCode">The language code for localization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the tag or failure message.</returns>
    Task<CSharpFunctionalExtensions.Result<CMS.ContentEngine.Tag>> GetTagByIdentifierAsync(string tagIdentifier, string languageCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get multiple tags by their identifiers.
    /// </summary>
    /// <param name="tagIdentifiers">Collection of tag identifiers as strings (GUID format).</param>
    /// <param name="languageCode">The language code for localization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the list of tags or failure message.</returns>
    Task<CSharpFunctionalExtensions.Result<IReadOnlyList<CMS.ContentEngine.Tag>>> GetTagsByIdentifiersAsync(IEnumerable<string> tagIdentifiers, string languageCode, CancellationToken cancellationToken = default);
}
