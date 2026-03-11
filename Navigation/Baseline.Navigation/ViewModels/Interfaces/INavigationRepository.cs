namespace Baseline.Navigation;

/// <summary>
/// INavigationRepository provides GetNavItemsAsync for v2-style navigation queries
/// Maps to v3 IMenuService.GetChildNavigationAsync
/// </summary>
public interface INavigationRepository : IMenuService
{
    /// <summary>
    /// Gets navigation items from a parent path ( method).
    /// </summary>
    Task<IEnumerable<NavigationItem>> GetNavItemsAsync(string parentPath);
}
