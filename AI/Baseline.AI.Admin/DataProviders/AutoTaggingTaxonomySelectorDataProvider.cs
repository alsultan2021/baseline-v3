using CMS.ContentEngine;
using CMS.DataEngine;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

using KenticoTaxonomyInfo = CMS.ContentEngine.TaxonomyInfo;

namespace Baseline.AI.Admin.DataProviders;

/// <summary>
/// Data provider for selecting taxonomies in auto-tagging settings.
/// Exposes all registered XbK taxonomies for multi-select.
/// </summary>
internal class AutoTaggingTaxonomySelectorDataProvider(
    IInfoProvider<KenticoTaxonomyInfo> taxonomyProvider)
    : IGeneralSelectorDataProvider
{
    private static ObjectSelectorListItem<string> InvalidItem => new()
    {
        IsValid = false,
        Text = "Invalid",
        Value = ""
    };

    /// <inheritdoc />
    public async Task<PagedSelectListItems<string>> GetItemsAsync(
        string searchTerm,
        int pageIndex,
        CancellationToken cancellationToken)
    {
        var query = taxonomyProvider.Get()
            .OrderBy(nameof(KenticoTaxonomyInfo.TaxonomyTitle))
            .Page(pageIndex, 50);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query.WhereContains(nameof(KenticoTaxonomyInfo.TaxonomyTitle), searchTerm);
        }

        var items = (await query.GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
            .Select(t => new ObjectSelectorListItem<string>
            {
                Value = t.TaxonomyName,
                Text = t.TaxonomyTitle,
                IsValid = true
            });

        return new PagedSelectListItems<string>
        {
            NextPageAvailable = query.NextPageAvailable,
            Items = items
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ObjectSelectorListItem<string>>> GetSelectedItemsAsync(
        IEnumerable<string> selectedValues,
        CancellationToken cancellationToken)
    {
        if (selectedValues is null || !selectedValues.Any())
        {
            return [];
        }

        var items = await taxonomyProvider.Get()
            .WhereIn(nameof(KenticoTaxonomyInfo.TaxonomyName), selectedValues.ToList())
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return selectedValues.Select(val =>
            items.FirstOrDefault(t =>
                string.Equals(t.TaxonomyName, val, StringComparison.OrdinalIgnoreCase))
                is { } found
                ? new ObjectSelectorListItem<string>
                {
                    Value = found.TaxonomyName,
                    Text = found.TaxonomyTitle,
                    IsValid = true
                }
                : InvalidItem);
    }
}
