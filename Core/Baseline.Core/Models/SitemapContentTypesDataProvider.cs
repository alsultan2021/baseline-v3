using CMS.DataEngine;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

namespace Baseline.Core.Models;

/// <summary>
/// Data provider for page-based content types selector.
/// Provides all available website page content types for sitemap configuration.
/// </summary>
public class SitemapContentTypesDataProvider : IGeneralSelectorDataProvider
{
    /// <summary>
    /// Gets paginated list of page content types based on search term.
    /// </summary>
    /// <param name="searchTerm">Optional search term to filter content types by display name</param>
    /// <param name="pageIndex">Page index for pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of page content type options</returns>
    public async Task<PagedSelectListItems<string>> GetItemsAsync(
        string searchTerm,
        int pageIndex,
        CancellationToken cancellationToken)
    {
        // Query for Website page content types (pages that can appear in sitemap)
        var itemQuery = DataClassInfoProvider.ProviderObject
            .Get()
            .WhereEquals(nameof(DataClassInfo.ClassContentTypeType), "Website");

        // Filter by search term if provided
        if (!string.IsNullOrEmpty(searchTerm))
        {
            itemQuery.WhereStartsWith(nameof(DataClassInfo.ClassDisplayName), searchTerm);
        }

        // Order by display name for consistent UI
        itemQuery.OrderBy(nameof(DataClassInfo.ClassDisplayName));

        // Apply pagination
        itemQuery.Page(pageIndex, 20);

        // Convert to selector items
        var items = (await itemQuery.GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
            .Select(x => new ObjectSelectorListItem<string>
            {
                Value = x.ClassName,
                Text = x.ClassDisplayName,
                IsValid = true
            });

        return new PagedSelectListItems<string>
        {
            NextPageAvailable = itemQuery.NextPageAvailable,
            Items = items
        };
    }

    /// <summary>
    /// Returns selector items for currently selected content types.
    /// </summary>
    /// <param name="selectedValues">Currently selected content type class names</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of selected item options</returns>
    public async Task<IEnumerable<ObjectSelectorListItem<string>>> GetSelectedItemsAsync(
        IEnumerable<string> selectedValues,
        CancellationToken cancellationToken)
    {
        if (selectedValues is null || !selectedValues.Any())
        {
            return [];
        }

        var itemQuery = DataClassInfoProvider.ProviderObject
            .Get()
            .WhereIn(nameof(DataClassInfo.ClassName), selectedValues.ToList());

        var allItems = await itemQuery.GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return allItems
            .Where(x => selectedValues.Contains(x.ClassName))
            .Select(x => new ObjectSelectorListItem<string>
            {
                Value = x.ClassName,
                Text = x.ClassDisplayName,
                IsValid = true
            })
            .ToList();
    }
}
