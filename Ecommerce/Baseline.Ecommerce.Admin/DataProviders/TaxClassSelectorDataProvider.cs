using Baseline.Ecommerce.Models;
using CMS.DataEngine;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

namespace Baseline.Ecommerce.Admin.DataProviders;

/// <summary>
/// General selector data provider for selecting multiple tax classes.
/// Used for assigning tax classes to products in reusable content types.
/// Use with [GeneralSelectorComponent(dataProviderType: typeof(TaxClassSelectorDataProvider), ...)]
/// </summary>
public class TaxClassSelectorDataProvider : IGeneralSelectorDataProvider
{
    /// <summary>
    /// Gets paginated list of tax classes based on search term.
    /// </summary>
    public async Task<PagedSelectListItems<string>> GetItemsAsync(
        string searchTerm,
        int pageIndex,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check if the data class exists
            var dataClassExists = DataClassInfoProvider.GetDataClassInfo("Baseline.TaxClass") != null;

            if (!dataClassExists)
            {
                return new PagedSelectListItems<string>
                {
                    Items = [],
                    NextPageAvailable = false
                };
            }

            var query = TaxClassInfo.Provider.Get()
                .WhereTrue(nameof(TaxClassInfo.TaxClassEnabled));

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.WhereContains(nameof(TaxClassInfo.TaxClassDisplayName), searchTerm);
            }

            query = query
                .OrderByDescending(nameof(TaxClassInfo.TaxClassIsDefault))
                .OrderBy(nameof(TaxClassInfo.TaxClassOrder))
                .Page(pageIndex, 20);

            var taxClasses = await query.GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            var items = taxClasses.Select(taxClass => new ObjectSelectorListItem<string>
            {
                Value = taxClass.TaxClassGuid.ToString(),
                Text = $"{taxClass.TaxClassDisplayName} ({taxClass.TaxClassDefaultRate:F2}%)",
                IsValid = true
            });

            return new PagedSelectListItems<string>
            {
                Items = items,
                NextPageAvailable = query.NextPageAvailable
            };
        }
        catch
        {
            return new PagedSelectListItems<string>
            {
                Items = [],
                NextPageAvailable = false
            };
        }
    }

    /// <summary>
    /// Gets specific tax class items by their GUIDs.
    /// Used for displaying already selected tax classes when the form loads.
    /// </summary>
    public async Task<IEnumerable<ObjectSelectorListItem<string>>> GetSelectedItemsAsync(
        IEnumerable<string> selectedValues,
        CancellationToken cancellationToken)
    {
        if (selectedValues?.Any() != true)
        {
            return [];
        }

        try
        {
            var guids = selectedValues
                .Where(v => Guid.TryParse(v, out _))
                .Select(v => Guid.Parse(v))
                .ToArray();

            if (guids.Length == 0)
            {
                return [];
            }

            var taxClasses = await TaxClassInfo.Provider.Get()
                .WhereIn(nameof(TaxClassInfo.TaxClassGuid), guids)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            return taxClasses.Select(taxClass => new ObjectSelectorListItem<string>
            {
                Value = taxClass.TaxClassGuid.ToString(),
                Text = $"{taxClass.TaxClassDisplayName} ({taxClass.TaxClassDefaultRate:F2}%)",
                IsValid = true
            });
        }
        catch
        {
            return [];
        }
    }
}
