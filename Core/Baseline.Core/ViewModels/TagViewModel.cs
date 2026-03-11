using Tag = CMS.ContentEngine.Tag;

namespace Baseline.Core.ViewModels;

/// <summary>
/// View model for displaying a taxonomy tag in hierarchical UI components.
/// Supports nested tag structures with level tracking for indentation.
/// </summary>
/// <param name="Name">The display name of the tag.</param>
/// <param name="Level">The nesting level (0 = root, 1 = first child, etc.).</param>
/// <param name="Value">The unique identifier (GUID) of the tag.</param>
/// <param name="IsChecked">Whether the tag is currently selected (for filter UI).</param>
public record TagViewModel(string Name, int Level, Guid Value, bool IsChecked = false)
{
    private const int ROOT_TAG_ID = 0;

    /// <summary>
    /// Creates a TagViewModel from a Kentico Tag.
    /// </summary>
    /// <param name="tag">The Kentico tag.</param>
    /// <param name="level">The nesting level.</param>
    /// <returns>A new TagViewModel instance.</returns>
    public static TagViewModel FromTag(Tag tag, int level = 0) => new(tag.Title, level, tag.Identifier);

    /// <summary>
    /// Creates a flat list of TagViewModels from a hierarchical tag collection,
    /// preserving parent-child relationships through level tracking.
    /// </summary>
    /// <param name="tags">The collection of tags to convert.</param>
    /// <returns>A flattened list of TagViewModels with level information.</returns>
    public static List<TagViewModel> FromTags(IEnumerable<Tag> tags)
    {
        var result = new List<TagViewModel>();
        var tagsByParentId = tags
            .GroupBy(tag => tag.ParentID)
            .ToDictionary(group => group.Key, group => group.ToList());

        if (tagsByParentId.TryGetValue(ROOT_TAG_ID, out var firstLevelTags))
        {
            BuildTagList(firstLevelTags, ROOT_TAG_ID);
        }

        return result;

        void BuildTagList(IEnumerable<Tag> currentLevelTags, int level)
        {
            foreach (var tag in currentLevelTags.OrderBy(tag => tag.Order))
            {
                result.Add(FromTag(tag, level));

                if (tagsByParentId.TryGetValue(tag.ID, out var childrenTags))
                {
                    BuildTagList(childrenTags, level + 1);
                }
            }
        }
    }
}
