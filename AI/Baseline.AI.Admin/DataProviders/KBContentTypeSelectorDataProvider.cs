using CMS.DataEngine;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

namespace Baseline.AI.Admin.DataProviders;

/// <summary>
/// Data provider for selecting content types in KB path configuration.
/// Includes Website content types (the primary type indexed by KB paths).
/// </summary>
internal class KBContentTypeSelectorDataProvider : IGeneralSelectorDataProvider
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
        var query = DataClassInfoProvider.ProviderObject
            .Get()
            .WhereIn(
                nameof(DataClassInfo.ClassContentTypeType),
                [ClassContentTypeType.WEBSITE, ClassContentTypeType.REUSABLE])
            .OrderBy(nameof(DataClassInfo.ClassDisplayName))
            .Page(pageIndex, 50);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query.WhereContains(nameof(DataClassInfo.ClassDisplayName), searchTerm);
        }

        var items = (await query.GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
            .Select(x => new ObjectSelectorListItem<string>
            {
                Value = x.ClassName,
                Text = $"{x.ClassDisplayName} ({x.ClassContentTypeType})",
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

        var items = (await DataClassInfoProvider.ProviderObject
            .Get()
            .WhereIn(nameof(DataClassInfo.ClassName), selectedValues.ToArray())
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
            .Select(x => new ObjectSelectorListItem<string>
            {
                Value = x.ClassName,
                Text = $"{x.ClassDisplayName} ({x.ClassContentTypeType})",
                IsValid = true
            });

        // Include invalid items for values not found
        var found = items.Select(i => i.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return items.Concat(
            selectedValues
                .Where(v => !found.Contains(v))
                .Select(_ => InvalidItem));
    }
}
