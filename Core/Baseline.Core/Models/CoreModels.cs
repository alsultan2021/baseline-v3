// V3 Native Core Models
// These replace the v2 Core.Models types with cleaner v3 implementations

using CSharpFunctionalExtensions;

namespace Baseline.Core;

#region Interfaces

/// <summary>
/// Interface for objects that can provide a cache key.
/// </summary>
public interface ICacheKey
{
    string GetCacheKey();
}

/// <summary>
/// Interface for objects that can be identified.
/// </summary>
public interface IObjectIdentifiable
{
    Maybe<int> UserID { get; }
    Maybe<Guid> UserGUID { get; }
}

/// <summary>
/// Interface for media metadata.
/// </summary>
public interface IMediaMetadata { }

/// <summary>
/// Interface for user metadata.
/// </summary>
public interface IUserMetadata { }

/// <summary>
/// Interface for object identity.
/// </summary>
public interface IObjectIdentity
{
    Maybe<int> ObjectID { get; }
    Maybe<Guid> ObjectGuid { get; }
    Maybe<string> ObjectCodeName { get; }
}

#endregion

#region Enums

/// <summary>
/// Cache duration presets.
/// </summary>
public enum CacheMinuteTypes
{
    VeryShort = 5,
    Short = 15,
    Medium = 60,
    Long = 360,
    VeryLong = 1440
}

#endregion

#region Identity Models

/// <summary>
/// Represents a path and channel lookup.
/// </summary>
public record PathChannel(string Path, Maybe<int> ChannelId) : ICacheKey
{
    public string GetCacheKey() => $"{Path}|{ChannelId.GetValueOrDefault(0)}";
}

/// <summary>
/// Represents a path, culture, and channel lookup.
/// </summary>
public record PathCultureChannel(string Path, Maybe<string> Culture, Maybe<int> ChannelId) : ICacheKey
{
    public string GetCacheKey() => $"{Path}|{Culture.GetValueOrDefault(string.Empty)}|{ChannelId.GetValueOrDefault(0)}";
}

/// <summary>
/// Represents content and culture lookup.
/// </summary>
public record ContentCulture(int ContentId, Maybe<string> Culture) : ICacheKey
{
    public string GetCacheKey() => $"{ContentId}|{Culture.GetValueOrDefault(string.Empty)}";
}

/// <summary>
/// Tree identity for page tree lookups.
/// </summary>
public record TreeIdentity : ICacheKey
{
    public Maybe<int> PageID { get; init; }
    public Maybe<Guid> PageGuid { get; init; }
    public Maybe<string> PageName { get; init; }
    public Maybe<PathChannel> PathChannelLookup { get; init; }

    public string GetCacheKey()
    {
        var pathKey = PathChannelLookup.TryGetValue(out var pc)
            ? $"{pc.Path}{pc.ChannelId.GetValueOrDefault(0)}"
            : string.Empty;
        return $"{PageID.GetValueOrDefault(0)}{pathKey}{PageGuid.GetValueOrDefault(Guid.Empty)}";
    }

    public override int GetHashCode() => GetCacheKey().GetHashCode();
}

/// <summary>
/// Tree identity with culture.
/// </summary>
public record TreeCultureIdentity : TreeIdentity
{
    public TreeCultureIdentity(string culture) => Culture = culture;
    public string Culture { get; init; }

    public new string GetCacheKey()
    {
        var pathKey = PathChannelLookup.TryGetValue(out var pc)
            ? $"{pc.Path}{pc.ChannelId.GetValueOrDefault(0)}"
            : string.Empty;
        return $"{PageID.GetValueOrDefault(0)}{pathKey}{PageGuid.GetValueOrDefault(Guid.Empty)}{Culture}";
    }

    public override int GetHashCode() => GetCacheKey().GetHashCode();
}

/// <summary>
/// Content identity for content item lookups.
/// </summary>
public record ContentIdentity : ICacheKey
{
    public Maybe<int> ContentID { get; init; }
    public Maybe<Guid> ContentGuid { get; init; }
    public Maybe<string> ContentName { get; init; }

    public string GetCacheKey() =>
        $"{ContentID.GetValueOrDefault(0)}|{ContentGuid.GetValueOrDefault(Guid.Empty)}|{ContentName.GetValueOrDefault(string.Empty)}";

    public override int GetHashCode() => GetCacheKey().GetHashCode();
}

/// <summary>
/// Content culture identity for localized content lookups.
/// </summary>
public record ContentCultureIdentity : ICacheKey
{
    public Maybe<int> ContentCultureID { get; init; }
    public Maybe<Guid> ContentCultureGuid { get; init; }
    public Maybe<ContentCulture> ContentCultureLookup { get; init; }

    public string GetCacheKey()
    {
        var lookupKey = ContentCultureLookup.TryGetValue(out var cc)
            ? $"{cc.ContentId}|{cc.Culture.GetValueOrDefault(string.Empty)}"
            : string.Empty;
        return $"{ContentCultureID.GetValueOrDefault(0)}|{ContentCultureGuid.GetValueOrDefault(Guid.Empty)}|{lookupKey}";
    }

    public override int GetHashCode() => GetCacheKey().GetHashCode();
}

/// <summary>
/// Object identity for general object lookups.
/// </summary>
public record ObjectIdentity : IObjectIdentity, ICacheKey
{
    public Maybe<int> ObjectID { get; init; }
    public Maybe<Guid> ObjectGuid { get; init; }
    public Maybe<string> ObjectCodeName { get; init; }

    public string GetCacheKey() =>
        $"{ObjectID.GetValueOrDefault(0)}|{ObjectGuid.GetValueOrDefault(Guid.Empty)}|{ObjectCodeName.GetValueOrDefault(string.Empty)}";

    public override int GetHashCode() => GetCacheKey().GetHashCode();
}

#endregion

#region Page Models

/// <summary>
/// Represents a page in the content tree.
/// </summary>
public record PageIdentity : ICacheKey
{
    public PageIdentity() { }

    public PageIdentity(
        string name, string alias, int pageID, Guid pageGuid,
        int contentID, string contentName, Guid contentGuid,
        int contentCultureID, Guid contentCultureGuid,
        string path, string culture, string relativeUrl, string absoluteUrl,
        int level, int channelID, string pageType)
    {
        Name = name;
        Alias = alias;
        PageID = pageID;
        PageGuid = pageGuid;
        ContentID = contentID;
        ContentName = contentName;
        ContentGuid = contentGuid;
        ContentCultureID = contentCultureID;
        ContentCultureGuid = contentCultureGuid;
        Path = path;
        Culture = culture;
        RelativeUrl = relativeUrl;
        AbsoluteUrl = absoluteUrl;
        PageLevel = level;
        ChannelID = channelID;
        PageType = pageType;
    }

    public string Name { get; init; } = string.Empty;
    public string Alias { get; init; } = string.Empty;
    public int PageID { get; init; }
    public Guid PageGuid { get; init; }
    public int ContentID { get; init; }
    public string ContentName { get; init; } = string.Empty;
    public Guid ContentGuid { get; init; }
    public int ContentCultureID { get; init; }
    public Guid ContentCultureGuid { get; init; }
    public string Path { get; init; } = string.Empty;
    public string Culture { get; init; } = string.Empty;
    public string RelativeUrl { get; init; } = string.Empty;
    public string AbsoluteUrl { get; init; } = string.Empty;
    public int PageLevel { get; init; }
    public int ChannelID { get; init; }
    public string PageType { get; init; } = string.Empty;

    public string GetCacheKey() => $"{PageID}|{ContentCultureID}|{Culture}";
    public override int GetHashCode() => GetCacheKey().GetHashCode();

    /// <summary>
    /// Empty page identity for null cases.
    /// </summary>
    public static PageIdentity Empty => new();
}

/// <summary>
/// Page metadata for SEO.
/// </summary>
public record PageMetaData
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Keywords { get; init; }
    public string? CanonicalUrl { get; init; }
    public string? OgTitle { get; init; }
    public string? OgDescription { get; init; }
    public string? OgImage { get; init; }
    public string? OgType { get; init; }
    public string? TwitterCard { get; init; }
    public string? TwitterSite { get; init; }
    public bool NoIndex { get; init; }
    public bool NoFollow { get; init; }
    public IEnumerable<AlternateUrl> AlternateUrls { get; init; } = [];
}

/// <summary>
/// Alternate URL for hreflang.
/// </summary>
public record AlternateUrl(string Hreflang, string Url);

#endregion

#region User Models

/// <summary>
/// Represents an authenticated or public user.
/// </summary>
public record User : IObjectIdentifiable
{
    public User()
    {
        UserName = "Public";
        Email = "public@localhost";
        Enabled = true;
        IsExternal = false;
        IsPublic = true;
    }

    public User(string userName, string email, bool enabled, bool isExternal, bool isPublic)
    {
        UserName = userName;
        Email = email;
        Enabled = enabled;
        IsExternal = isExternal;
        IsPublic = isPublic;
    }

    public User(int userID, string userName, Guid userGUID, string email, bool enabled, bool isExternal, bool isPublic = false)
    {
        UserID = userID;
        UserName = userName;
        UserGUID = userGUID;
        Email = email;
        Enabled = enabled;
        IsExternal = isExternal;
        IsPublic = isPublic;
    }

    public Maybe<int> UserID { get; init; }
    public string UserName { get; init; } = string.Empty;
    public Maybe<Guid> UserGUID { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool Enabled { get; init; }
    public bool IsExternal { get; init; }
    public bool IsPublic { get; init; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Public/anonymous user.
    /// </summary>
    public static User Public => new();
}

#endregion

#region Media Models

/// <summary>
/// Represents a media item (image, document, etc.).
/// </summary>
public record MediaItem
{
    public int MediaID { get; init; }
    public Guid MediaGuid { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string FileExtension { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? AltText { get; init; }
    public long FileSize { get; init; }
    public string MimeType { get; init; } = string.Empty;
    public int? Width { get; init; }
    public int? Height { get; init; }
    public DateTimeOffset Created { get; init; }
    public DateTimeOffset Modified { get; init; }
}

/// <summary>
/// Image metadata for responsive images.
/// </summary>
public record ImageProfile
{
    public string Url { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
    public string? AltText { get; init; }
    public string? Title { get; init; }
    public bool IsLazyLoad { get; init; } = true;
    public string? Srcset { get; init; }
    public string? Sizes { get; init; }
}

#endregion

#region Category Models

/// <summary>
/// Represents a taxonomy category.
/// </summary>
public record CategoryItem
{
    public int CategoryID { get; init; }
    public Guid CategoryGuid { get; init; }
    public string CodeName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int? ParentCategoryID { get; init; }
    public int Order { get; init; }
}

#endregion
