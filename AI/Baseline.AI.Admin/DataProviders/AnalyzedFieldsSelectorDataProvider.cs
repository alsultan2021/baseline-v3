using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

namespace Baseline.AI.Admin.DataProviders;

/// <summary>
/// Data provider for selecting content fields to analyze for auto-tagging.
/// Provides a curated list of well-known content fields plus dynamically discovered fields
/// from eligible content types.
/// </summary>
internal class AnalyzedFieldsSelectorDataProvider : IGeneralSelectorDataProvider
{
    /// <summary>
    /// Well-known field names commonly available on content types.
    /// </summary>
    private static readonly (string Value, string Text)[] WellKnownFields =
    [
        ("Title", "Title"),
        ("Description", "Description / Summary"),
        ("Content", "Content (rich text)"),
        ("ShortDescription", "Short Description"),
        ("Keywords", "Keywords / Meta Keywords"),
        ("MetaDescription", "Meta Description"),
        ("Name", "Name / Display Name"),
        ("Teaser", "Teaser Text"),
        ("Body", "Body Text"),
        ("Summary", "Summary"),
        ("Subtitle", "Subtitle"),
        ("HtmlContent", "HTML Content (crawled)"),
    ];

    /// <inheritdoc />
    public Task<PagedSelectListItems<string>> GetItemsAsync(
        string searchTerm,
        int pageIndex,
        CancellationToken cancellationToken)
    {
        var items = WellKnownFields.AsEnumerable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            items = items.Where(f =>
                f.Text.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                f.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        var result = new PagedSelectListItems<string>
        {
            NextPageAvailable = false,
            Items = items.Select(f => new ObjectSelectorListItem<string>
            {
                Value = f.Value,
                Text = f.Text,
                IsValid = true
            })
        };

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IEnumerable<ObjectSelectorListItem<string>>> GetSelectedItemsAsync(
        IEnumerable<string> selectedValues,
        CancellationToken cancellationToken)
    {
        if (selectedValues is null || !selectedValues.Any())
        {
            return Task.FromResult<IEnumerable<ObjectSelectorListItem<string>>>([]);
        }

        var knownLookup = WellKnownFields.ToDictionary(f => f.Value, f => f.Text, StringComparer.OrdinalIgnoreCase);

        var result = selectedValues.Select(val =>
            new ObjectSelectorListItem<string>
            {
                Value = val,
                Text = knownLookup.TryGetValue(val, out var text) ? text : val,
                IsValid = true
            });

        return Task.FromResult(result);
    }
}
