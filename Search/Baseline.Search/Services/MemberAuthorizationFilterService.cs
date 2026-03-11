using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Baseline.Search;

/// <summary>
/// Filters search results based on the current member's authentication state
/// and role-tag permissions. Secured pages are excluded for anonymous users,
/// and role-restricted pages are excluded when the member lacks matching role tags.
/// </summary>
public class MemberAuthorizationFilterService(
    IHttpContextAccessor httpContextAccessor,
    ILogger<MemberAuthorizationFilterService> logger) : IMemberAuthorizationFilter
{
    private const string MemberRoleClaim = "MemberRole";
    private const string IsSecuredField = "IsSecured";
    private const string RoleTagsField = "MemberPermissionRoleTags";

    /// <inheritdoc />
    public Task<IEnumerable<SearchResult>> FilterResultsAsync(IEnumerable<SearchResult> results)
    {
        var user = httpContextAccessor.HttpContext?.User;
        bool isAuthenticated = user?.Identity?.IsAuthenticated ?? false;

        // Collect member role claims
        var memberRoles = isAuthenticated
            ? user!.FindAll(MemberRoleClaim)
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrEmpty(v))
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var filtered = results.Where(result =>
        {
            // Check if the page is secured
            bool isSecured = result.Metadata.TryGetValue(IsSecuredField, out var securedObj)
                && securedObj is true or "true" or "True";

            if (isSecured && !isAuthenticated)
            {
                return false; // anonymous users cannot see secured pages
            }

            // Check role-tag restriction
            if (result.Metadata.TryGetValue(RoleTagsField, out var roleTagsObj)
                && roleTagsObj is string roleTagsCsv
                && !string.IsNullOrWhiteSpace(roleTagsCsv))
            {
                // If the result specifies required role tags, the user must have at least one
                var requiredRoles = roleTagsCsv
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (requiredRoles.Length > 0 && !requiredRoles.Any(memberRoles.Contains))
                {
                    return false;
                }
            }

            return true;
        });

        logger.LogDebug("MemberAuthFilter: Authenticated={Auth}, Roles={Roles}",
            isAuthenticated, string.Join(",", memberRoles));

        return Task.FromResult(filtered);
    }

    /// <inheritdoc />
    public Task<bool> CanAccessAsync(string documentId)
    {
        // Default: allow — filtering happens at the result-set level
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> GetAuthorizedContentTypesAsync()
    {
        // No content-type-level restrictions — all types visible
        return Task.FromResult<IEnumerable<string>>([]);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Guid>> GetAuthorizedTaxonomyTagsAsync()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return Task.FromResult<IEnumerable<Guid>>([]);
        }

        // Extract taxonomy GUIDs from MemberRole claims that are valid GUIDs
        var tagGuids = user.FindAll(MemberRoleClaim)
            .Select(c => c.Value)
            .Where(v => Guid.TryParse(v, out _))
            .Select(Guid.Parse);

        return Task.FromResult(tagGuids);
    }
}
