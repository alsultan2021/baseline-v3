namespace Baseline.Core;

/// <summary>
/// Service for generating structured data (JSON-LD) for SEO.
/// </summary>
public interface IStructuredDataService
{
    /// <summary>
    /// Generates Article schema JSON-LD.
    /// </summary>
    Task<string> GenerateArticleJsonLdAsync<T>(T content) where T : class;

    /// <summary>
    /// Generates BreadcrumbList schema JSON-LD.
    /// </summary>
    Task<string> GenerateBreadcrumbJsonLdAsync(IEnumerable<BreadcrumbItem> items);

    /// <summary>
    /// Generates FAQPage schema JSON-LD.
    /// </summary>
    Task<string> GenerateFaqJsonLdAsync(IEnumerable<FaqItem> items);

    /// <summary>
    /// Generates Organization schema JSON-LD.
    /// </summary>
    Task<string> GenerateOrganizationJsonLdAsync(OrganizationData data);

    /// <summary>
    /// Generates WebSite schema JSON-LD with search action.
    /// </summary>
    Task<string> GenerateWebSiteJsonLdAsync(WebSiteData data);

    /// <summary>
    /// Generates custom JSON-LD from a dictionary.
    /// </summary>
    string GenerateCustomJsonLd(Dictionary<string, object> data);

    /// <summary>
    /// Generates Event schema JSON-LD.
    /// See: https://schema.org/Event
    /// </summary>
    Task<string> GenerateEventJsonLdAsync(EventSchemaData data);

    /// <summary>
    /// Generates Product schema JSON-LD.
    /// See: https://schema.org/Product
    /// </summary>
    Task<string> GenerateProductJsonLdAsync(ProductSchemaData data);

    /// <summary>
    /// Generates LocalBusiness schema JSON-LD.
    /// See: https://schema.org/LocalBusiness
    /// </summary>
    Task<string> GenerateLocalBusinessJsonLdAsync(LocalBusinessSchemaData data);
}

/// <summary>
/// Represents a breadcrumb navigation item.
/// </summary>
public record BreadcrumbItem(string Name, string Url, int Position);

/// <summary>
/// Represents a FAQ item for structured data.
/// </summary>
public record FaqItem(string Question, string Answer);

/// <summary>
/// Organization data for structured data.
/// </summary>
public record OrganizationData(
    string Name,
    string Url,
    string? Logo = null,
    string? Description = null,
    IEnumerable<string>? SocialProfiles = null,
    ContactPointData? ContactPoint = null);

/// <summary>
/// Contact point data for organization.
/// </summary>
public record ContactPointData(
    string Telephone,
    string ContactType,
    string? Email = null,
    IEnumerable<string>? AvailableLanguages = null);

/// <summary>
/// Website data for structured data.
/// </summary>
public record WebSiteData(
    string Name,
    string Url,
    string? SearchActionUrl = null,
    string? Description = null);

/// <summary>
/// Event data for structured data.
/// See: https://schema.org/Event
/// </summary>
public record EventSchemaData(
    string Name,
    DateTimeOffset StartDate,
    string? Description = null,
    DateTimeOffset? EndDate = null,
    string? Location = null,
    string? LocationAddress = null,
    string? Url = null,
    string? Image = null,
    string? OrganizerName = null,
    string? OrganizerUrl = null,
    EventAttendanceMode AttendanceMode = EventAttendanceMode.Offline,
    EventStatus Status = EventStatus.Scheduled);

/// <summary>
/// Event attendance mode.
/// </summary>
public enum EventAttendanceMode
{
    Offline,
    Online,
    Mixed
}

/// <summary>
/// Event status.
/// </summary>
public enum EventStatus
{
    Scheduled,
    Cancelled,
    Postponed,
    MovedOnline,
    Rescheduled
}

/// <summary>
/// Product data for structured data.
/// See: https://schema.org/Product
/// </summary>
public record ProductSchemaData(
    string Name,
    string? Description = null,
    string? Image = null,
    string? Sku = null,
    string? Brand = null,
    string? Url = null,
    decimal? Price = null,
    string? PriceCurrency = null,
    string? Availability = null,
    double? RatingValue = null,
    int? ReviewCount = null);

/// <summary>
/// LocalBusiness data for structured data.
/// See: https://schema.org/LocalBusiness
/// </summary>
public record LocalBusinessSchemaData(
    string Name,
    string? Description = null,
    string? Url = null,
    string? Image = null,
    string? Telephone = null,
    string? Email = null,
    string? StreetAddress = null,
    string? AddressLocality = null,
    string? AddressRegion = null,
    string? PostalCode = null,
    string? AddressCountry = null,
    double? Latitude = null,
    double? Longitude = null,
    string? OpeningHours = null,
    string? PriceRange = null);
