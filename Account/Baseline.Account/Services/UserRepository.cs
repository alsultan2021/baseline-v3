using CMS.Helpers;
using CMS.Membership;
using Kentico.Membership;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Baseline.Account;

/// <summary>
/// v3 implementation of IUserRepository using Xperience by Kentico member APIs.
/// Implements caching for MemberInfo lookups per Kentico best practices.
/// </summary>
/// <typeparam name="TUser">The application user type that extends ApplicationUser.</typeparam>
public sealed class UserRepository<TUser>(
    UserManager<TUser> userManager,
    IProgressiveCache progressiveCache,
    ILogger<UserRepository<TUser>> logger) : IUserRepository
    where TUser : ApplicationUser, new()
{
    private const int CacheMinutes = 10;
    /// <inheritdoc/>
    public async Task<IUser?> GetByIdAsync(int id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        return user is null ? null : MapToUser(user);
    }

    /// <inheritdoc/>
    public async Task<IUser?> GetByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is null ? null : MapToUser(user);
    }

    /// <inheritdoc/>
    public async Task<IUser?> GetByUsernameAsync(string username)
    {
        var user = await userManager.FindByNameAsync(username);
        return user is null ? null : MapToUser(user);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Warning: This method retrieves all members and should not be used with large user bases.
    /// Consider using MemberInfo.Provider directly with pagination for production scenarios.
    /// Limited to first 1000 members to prevent memory issues.
    /// </remarks>
    public async Task<IEnumerable<IUser>> GetAllAsync()
    {
        const int MaxResults = 1000;
        logger.LogWarning("GetAllAsync called - limited to {MaxResults} results. Use MemberInfo.Provider with pagination for large user bases.", MaxResults);

        return await progressiveCache.LoadAsync(async cs =>
        {
            var members = MemberInfo.Provider.Get()
                .TopN(MaxResults)
                .OrderBy("MemberName")
                .TypedResult;

            if (cs.Cached)
            {
                cs.CacheDependency = CacheHelper.GetCacheDependency($"{MemberInfo.OBJECT_TYPE}|all");
            }

            var users = new List<IUser>();
            foreach (var member in members)
            {
                var user = await userManager.FindByIdAsync(member.MemberID.ToString());
                if (user is not null)
                {
                    users.Add(MapToUser(user, member.MemberGuid));
                }
            }

            return (IEnumerable<IUser>)users;
        }, new CacheSettings(CacheMinutes, "baseline|allusers"));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This method invalidates the member cache but redirects actual updates to UserManager.
    /// Use UserManager.UpdateAsync for actual user updates.
    /// </remarks>
    public Task UpdateAsync(IUser user)
    {
        logger.LogInformation("UpdateAsync called for user {UserId} - invalidating cache", user.UserId);

        // Invalidate cached data for this user
        InvalidateUserCache(user.UserId);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Invalidates cached data for a specific user.
    /// Call this after UserManager.UpdateAsync to ensure cache consistency.
    /// </summary>
    /// <param name="memberId">The member ID to invalidate.</param>
    public void InvalidateUserCache(int memberId)
    {
        // Touch the dummy keys to invalidate any cached data
        CacheHelper.TouchKey($"{MemberInfo.OBJECT_TYPE}|byid|{memberId}");
        CacheHelper.TouchKey("baseline|allusers");
        logger.LogDebug("Invalidated cache for member {MemberId}", memberId);
    }

    /// <inheritdoc/>
    public async Task<CSharpFunctionalExtensions.Result<IUser>> GetUserAsync(string username)
    {
        var user = await userManager.FindByNameAsync(username);
        return user is null
            ? CSharpFunctionalExtensions.Result.Failure<IUser>($"User not found: {username}")
            : CSharpFunctionalExtensions.Result.Success(MapToUser(user));
    }

    /// <inheritdoc/>
    public async Task<CSharpFunctionalExtensions.Result<IUser>> GetUserAsync(Guid userGuid)
    {
        // Use cached lookup for member GUID to ID mapping
        var memberId = await GetMemberIdByGuidCachedAsync(userGuid);
        if (memberId is null)
        {
            return CSharpFunctionalExtensions.Result.Failure<IUser>($"User not found: {userGuid}");
        }

        var user = await userManager.FindByIdAsync(memberId.Value.ToString());
        return user is null
            ? CSharpFunctionalExtensions.Result.Failure<IUser>($"User not found: {userGuid}")
            : CSharpFunctionalExtensions.Result.Success(MapToUser(user, userGuid));
    }

    /// <summary>
    /// Gets member ID by GUID with caching.
    /// </summary>
    private async Task<int?> GetMemberIdByGuidCachedAsync(Guid memberGuid)
    {
        return await progressiveCache.LoadAsync(async cs =>
            {
                var memberInfo = MemberInfo.Provider.Get()
                    .WhereEquals("MemberGuid", memberGuid)
                    .TopN(1)
                    .TypedResult;

                var member = memberInfo.FirstOrDefault();
                if (member is null)
                {
                    // Don't cache misses for long
                    cs.CacheMinutes = 1;
                    return (int?)null;
                }

                if (cs.Cached)
                {
                    cs.CacheDependency = CacheHelper.GetCacheDependency($"{MemberInfo.OBJECT_TYPE}|byid|{member.MemberID}");
                }

                return member.MemberID;
            },
            new CacheSettings(CacheMinutes, "baseline|memberid|byguid", memberGuid));
    }

    /// <summary>
    /// Gets member GUID by ID with caching.
    /// </summary>
    private async Task<Guid> GetMemberGuidCachedAsync(int memberId)
    {
        return await progressiveCache.LoadAsync(async cs =>
            {
                var member = MemberInfo.Provider.Get(memberId);
                if (member is null)
                {
                    cs.CacheMinutes = 1;
                    return Guid.Empty;
                }

                if (cs.Cached)
                {
                    cs.CacheDependency = CacheHelper.GetCacheDependency($"{MemberInfo.OBJECT_TYPE}|byid|{memberId}");
                }

                return member.MemberGuid;
            },
            new CacheSettings(CacheMinutes, "baseline|memberguid|byid", memberId));
    }

    /// <inheritdoc/>
    public async Task<CSharpFunctionalExtensions.Result<IUser>> GetUserByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is null
            ? CSharpFunctionalExtensions.Result.Failure<IUser>($"User not found by email: {email}")
            : CSharpFunctionalExtensions.Result.Success(MapToUser(user));
    }

    private static IUser MapToUser(TUser user, Guid? precomputedGuid = null)
    {
        return new UserAdapter(user, precomputedGuid);
    }

    /// <summary>
    /// Creates a user adapter with cached GUID lookup.
    /// </summary>
    private async Task<IUser> MapToUserWithGuidAsync(TUser user)
    {
        var guid = user.Id > 0 ? await GetMemberGuidCachedAsync(user.Id) : Guid.Empty;
        return new UserAdapter(user, guid);
    }

    /// <summary>
    /// Adapter to expose ApplicationUser as IUser.
    /// Note: UserGuid is computed lazily or pre-populated to avoid sync DB calls.
    /// </summary>
    private sealed class UserAdapter : IUser
    {
        private readonly TUser _user;
        private readonly Guid? _precomputedGuid;

        public UserAdapter(TUser user, Guid? precomputedGuid = null)
        {
            _user = user;
            _precomputedGuid = precomputedGuid;
        }

        public int UserId => _user.Id;

        /// <summary>
        /// Returns the precomputed GUID if available, otherwise returns Empty.
        /// Use MapToUserWithGuidAsync for guaranteed GUID resolution.
        /// </summary>
        public Guid UserGuid => _precomputedGuid ?? Guid.Empty;

        public string UserName => _user.UserName ?? string.Empty;
        public string Email => _user.Email ?? string.Empty;
        public string FirstName => string.Empty;
        public string LastName => string.Empty;
        public bool Enabled => _user.Enabled;
        public bool IsExternal => _user.IsExternal;
    }
}
