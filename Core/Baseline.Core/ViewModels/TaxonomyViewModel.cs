using CMS.ContentEngine;
using Tag = CMS.ContentEngine.Tag;

namespace Baseline.Core.ViewModels;

/// <summary>
/// View model for displaying a taxonomy with its tags in UI components.
/// Commonly used for category filters, tag selectors, and navigation menus.
/// </summary>
/// <param name="Name">The display name of the taxonomy.</param>
/// <param name="CodeName">The code name of the taxonomy.</param>
/// <param name="Tags">The list of tag view models within this taxonomy.</param>
public record TaxonomyViewModel(string Name, string CodeName, List<TagViewModel> Tags)
{
    /// <summary>
    /// Creates a TaxonomyViewModel from Kentico TaxonomyData.
    /// </summary>
    /// <param name="taxonomy">The taxonomy data from Kentico.</param>
    /// <returns>A new TaxonomyViewModel instance.</returns>
    public static TaxonomyViewModel FromTaxonomyData(TaxonomyData taxonomy) =>
        new(taxonomy.Taxonomy.Title, taxonomy.Taxonomy.Name, TagViewModel.FromTags(taxonomy.Tags));

    /// <summary>
    /// Gets the identifiers of all selected (checked) tags.
    /// </summary>
    /// <returns>Collection of selected tag GUIDs.</returns>
    public IEnumerable<Guid> GetSelectedTagIdentifiers() =>
        Tags?.Where(tag => tag.IsChecked).Select(tag => tag.Value) ?? [];

    /// <summary>
    /// Creates a TagCollection from the selected tags.
    /// Returns null if no tags are selected or if creation fails.
    /// </summary>
    /// <returns>A TagCollection of selected tags, or null.</returns>
    public async Task<TagCollection?> GetSelectedTagsAsync()
    {
        if (Tags == null || Tags.Count == 0)
        {
            return null;
        }

        var tagValues = GetSelectedTagIdentifiers();

        if (!tagValues.Any())
        {
            return null;
        }

        try
        {
            return await TagCollection.Create(tagValues);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
