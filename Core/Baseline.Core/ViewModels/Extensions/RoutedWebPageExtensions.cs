using Kentico.Content.Web.Mvc;

namespace Baseline.Core.Extensions;

/// <summary>
/// Extensions for RoutedWebPage.
/// </summary>
public static class RoutedWebPageExtensions
{
    /// <summary>
    /// Converts a RoutedWebPage to a TreeCultureIdentity for content lookups.
    /// </summary>
    public static TreeCultureIdentity ToTreeCultureIdentity(this RoutedWebPage page)
    {
        return new TreeCultureIdentity(page.LanguageName)
        {
            PageID = page.WebPageItemID,
            PageGuid = page.WebPageItemGUID
        };
    }

    /// <summary>
    /// Converts a RoutedWebPage to a TreeIdentity.
    /// </summary>
    public static TreeIdentity ToTreeIdentity(this RoutedWebPage page)
    {
        return new TreeIdentity()
        {
            PageID = page.WebPageItemID,
            PageGuid = page.WebPageItemGUID
        };
    }
}
