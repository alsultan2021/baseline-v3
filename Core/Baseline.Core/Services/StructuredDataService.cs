using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace Baseline.Core;

/// <summary>
/// Default implementation of <see cref="IStructuredDataService"/>.
/// </summary>
internal sealed class StructuredDataService(IJsonLdGenerator jsonLdGenerator) : IStructuredDataService
{
    // Cache property lookups per (Type, propertyName) to avoid repeated reflection
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> PropertyCache = new();

    public Task<string> GenerateArticleJsonLdAsync<T>(T content) where T : class
    {
        // Use reflection to extract article properties from content
        var schema = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Article"
        };

        var type = typeof(T);

        // Try common article property names
        TryAddProperty(schema, content, type, "headline", "Title", "Headline", "Name");
        TryAddProperty(schema, content, type, "description", "Description", "Summary", "Excerpt", "Teaser");
        TryAddProperty(schema, content, type, "author", "Author", "AuthorName", "CreatedBy");
        TryAddProperty(schema, content, type, "image", "Image", "Thumbnail", "ThumbnailUrl", "FeaturedImage");
        TryAddProperty(schema, content, type, "url", "Url", "CanonicalUrl", "PageUrl");

        // Try to extract dates
        TryAddDateProperty(schema, content, type, "datePublished", "PublishedDate", "DatePublished", "Created", "CreatedDate");
        TryAddDateProperty(schema, content, type, "dateModified", "ModifiedDate", "DateModified", "LastModified", "Updated");

        return Task.FromResult(jsonLdGenerator.Generate(schema));
    }

    private static void TryAddProperty<T>(Dictionary<string, object> schema, T content, Type type, string schemaKey, params string[] propertyNames) where T : class
    {
        foreach (var propName in propertyNames)
        {
            var prop = PropertyCache.GetOrAdd((type, propName), static key =>
                key.Item1.GetProperty(key.Item2, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));

            if (prop is not null)
            {
                var value = prop.GetValue(content);
                if (value is not null && !string.IsNullOrEmpty(value.ToString()))
                {
                    schema[schemaKey] = value.ToString()!;
                    return;
                }
            }
        }
    }

    private static void TryAddDateProperty<T>(Dictionary<string, object> schema, T content, Type type, string schemaKey, params string[] propertyNames) where T : class
    {
        foreach (var propName in propertyNames)
        {
            var prop = PropertyCache.GetOrAdd((type, propName), static key =>
                key.Item1.GetProperty(key.Item2, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));

            if (prop is not null)
            {
                var value = prop.GetValue(content);
                if (value is DateTime dateValue && dateValue != default)
                {
                    schema[schemaKey] = dateValue.ToString("yyyy-MM-dd");
                    return;
                }
                if (value is DateTimeOffset dtoValue && dtoValue != default)
                {
                    schema[schemaKey] = dtoValue.ToString("yyyy-MM-dd");
                    return;
                }
            }
        }
    }

    public Task<string> GenerateBreadcrumbJsonLdAsync(IEnumerable<BreadcrumbItem> items)
    {
        var itemList = items.Select(item => new Dictionary<string, object>
        {
            ["@type"] = "ListItem",
            ["position"] = item.Position,
            ["name"] = item.Name,
            ["item"] = item.Url
        }).ToList();

        var schema = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BreadcrumbList",
            ["itemListElement"] = itemList
        };

        return Task.FromResult(jsonLdGenerator.Generate(schema));
    }

    public Task<string> GenerateFaqJsonLdAsync(IEnumerable<FaqItem> items)
    {
        var mainEntity = items.Select(item => new Dictionary<string, object>
        {
            ["@type"] = "Question",
            ["name"] = item.Question,
            ["acceptedAnswer"] = new Dictionary<string, object>
            {
                ["@type"] = "Answer",
                ["text"] = item.Answer
            }
        }).ToList();

        var schema = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "FAQPage",
            ["mainEntity"] = mainEntity
        };

        return Task.FromResult(jsonLdGenerator.Generate(schema));
    }

    public Task<string> GenerateOrganizationJsonLdAsync(OrganizationData data)
    {
        var schema = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Organization",
            ["name"] = data.Name,
            ["url"] = data.Url
        };

        if (!string.IsNullOrEmpty(data.Logo))
        {
            schema["logo"] = data.Logo;
        }

        if (!string.IsNullOrEmpty(data.Description))
        {
            schema["description"] = data.Description;
        }

        if (data.SocialProfiles?.Any() == true)
        {
            schema["sameAs"] = data.SocialProfiles.ToList();
        }

        if (data.ContactPoint is not null)
        {
            schema["contactPoint"] = new Dictionary<string, object>
            {
                ["@type"] = "ContactPoint",
                ["telephone"] = data.ContactPoint.Telephone,
                ["contactType"] = data.ContactPoint.ContactType,
                ["email"] = data.ContactPoint.Email ?? string.Empty,
                ["availableLanguage"] = data.ContactPoint.AvailableLanguages?.ToList() ?? ["en"]
            };
        }

        return Task.FromResult(jsonLdGenerator.Generate(schema));
    }

    public Task<string> GenerateWebSiteJsonLdAsync(WebSiteData data)
    {
        var schema = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "WebSite",
            ["name"] = data.Name,
            ["url"] = data.Url
        };

        if (!string.IsNullOrEmpty(data.Description))
        {
            schema["description"] = data.Description;
        }

        if (!string.IsNullOrEmpty(data.SearchActionUrl))
        {
            schema["potentialAction"] = new Dictionary<string, object>
            {
                ["@type"] = "SearchAction",
                ["target"] = new Dictionary<string, object>
                {
                    ["@type"] = "EntryPoint",
                    ["urlTemplate"] = data.SearchActionUrl
                },
                ["query-input"] = "required name=search_term_string"
            };
        }

        return Task.FromResult(jsonLdGenerator.Generate(schema));
    }

    public string GenerateCustomJsonLd(Dictionary<string, object> data)
    {
        if (!data.ContainsKey("@context"))
        {
            data["@context"] = "https://schema.org";
        }

        return jsonLdGenerator.Generate(data);
    }

    public Task<string> GenerateEventJsonLdAsync(EventSchemaData data)
    {
        var schema = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Event",
            ["name"] = data.Name,
            ["startDate"] = data.StartDate.ToString("yyyy-MM-ddTHH:mm:sszzz")
        };

        if (!string.IsNullOrEmpty(data.Description))
            schema["description"] = data.Description;

        if (data.EndDate.HasValue)
            schema["endDate"] = data.EndDate.Value.ToString("yyyy-MM-ddTHH:mm:sszzz");

        if (!string.IsNullOrEmpty(data.Url))
            schema["url"] = data.Url;

        if (!string.IsNullOrEmpty(data.Image))
            schema["image"] = data.Image;

        // Attendance mode
        schema["eventAttendanceMode"] = data.AttendanceMode switch
        {
            EventAttendanceMode.Online => "https://schema.org/OnlineEventAttendanceMode",
            EventAttendanceMode.Mixed => "https://schema.org/MixedEventAttendanceMode",
            _ => "https://schema.org/OfflineEventAttendanceMode"
        };

        // Event status
        schema["eventStatus"] = data.Status switch
        {
            EventStatus.Cancelled => "https://schema.org/EventCancelled",
            EventStatus.Postponed => "https://schema.org/EventPostponed",
            EventStatus.MovedOnline => "https://schema.org/EventMovedOnline",
            EventStatus.Rescheduled => "https://schema.org/EventRescheduled",
            _ => "https://schema.org/EventScheduled"
        };

        // Location
        if (!string.IsNullOrEmpty(data.Location))
        {
            var location = new Dictionary<string, object>
            {
                ["@type"] = "Place",
                ["name"] = data.Location
            };

            if (!string.IsNullOrEmpty(data.LocationAddress))
            {
                location["address"] = data.LocationAddress;
            }

            schema["location"] = location;
        }

        // Organizer
        if (!string.IsNullOrEmpty(data.OrganizerName))
        {
            var organizer = new Dictionary<string, object>
            {
                ["@type"] = "Organization",
                ["name"] = data.OrganizerName
            };

            if (!string.IsNullOrEmpty(data.OrganizerUrl))
                organizer["url"] = data.OrganizerUrl;

            schema["organizer"] = organizer;
        }

        return Task.FromResult(jsonLdGenerator.Generate(schema));
    }

    public Task<string> GenerateProductJsonLdAsync(ProductSchemaData data)
    {
        var schema = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Product",
            ["name"] = data.Name
        };

        if (!string.IsNullOrEmpty(data.Description))
            schema["description"] = data.Description;

        if (!string.IsNullOrEmpty(data.Image))
            schema["image"] = data.Image;

        if (!string.IsNullOrEmpty(data.Sku))
            schema["sku"] = data.Sku;

        if (!string.IsNullOrEmpty(data.Url))
            schema["url"] = data.Url;

        if (!string.IsNullOrEmpty(data.Brand))
        {
            schema["brand"] = new Dictionary<string, object>
            {
                ["@type"] = "Brand",
                ["name"] = data.Brand
            };
        }

        // Offer (price)
        if (data.Price.HasValue)
        {
            var offer = new Dictionary<string, object>
            {
                ["@type"] = "Offer",
                ["price"] = data.Price.Value,
                ["priceCurrency"] = data.PriceCurrency ?? "USD"
            };

            if (!string.IsNullOrEmpty(data.Availability))
                offer["availability"] = $"https://schema.org/{data.Availability}";

            schema["offers"] = offer;
        }

        // Aggregate rating
        if (data.RatingValue.HasValue)
        {
            var rating = new Dictionary<string, object>
            {
                ["@type"] = "AggregateRating",
                ["ratingValue"] = data.RatingValue.Value,
                ["bestRating"] = 5
            };

            if (data.ReviewCount.HasValue)
                rating["reviewCount"] = data.ReviewCount.Value;

            schema["aggregateRating"] = rating;
        }

        return Task.FromResult(jsonLdGenerator.Generate(schema));
    }

    public Task<string> GenerateLocalBusinessJsonLdAsync(LocalBusinessSchemaData data)
    {
        var schema = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "LocalBusiness",
            ["name"] = data.Name
        };

        if (!string.IsNullOrEmpty(data.Description))
            schema["description"] = data.Description;

        if (!string.IsNullOrEmpty(data.Url))
            schema["url"] = data.Url;

        if (!string.IsNullOrEmpty(data.Image))
            schema["image"] = data.Image;

        if (!string.IsNullOrEmpty(data.Telephone))
            schema["telephone"] = data.Telephone;

        if (!string.IsNullOrEmpty(data.Email))
            schema["email"] = data.Email;

        if (!string.IsNullOrEmpty(data.PriceRange))
            schema["priceRange"] = data.PriceRange;

        if (!string.IsNullOrEmpty(data.OpeningHours))
            schema["openingHours"] = data.OpeningHours;

        // Address
        if (!string.IsNullOrEmpty(data.StreetAddress) || !string.IsNullOrEmpty(data.AddressLocality))
        {
            var address = new Dictionary<string, object> { ["@type"] = "PostalAddress" };

            if (!string.IsNullOrEmpty(data.StreetAddress))
                address["streetAddress"] = data.StreetAddress;
            if (!string.IsNullOrEmpty(data.AddressLocality))
                address["addressLocality"] = data.AddressLocality;
            if (!string.IsNullOrEmpty(data.AddressRegion))
                address["addressRegion"] = data.AddressRegion;
            if (!string.IsNullOrEmpty(data.PostalCode))
                address["postalCode"] = data.PostalCode;
            if (!string.IsNullOrEmpty(data.AddressCountry))
                address["addressCountry"] = data.AddressCountry;

            schema["address"] = address;
        }

        // Geo coordinates
        if (data.Latitude.HasValue && data.Longitude.HasValue)
        {
            schema["geo"] = new Dictionary<string, object>
            {
                ["@type"] = "GeoCoordinates",
                ["latitude"] = data.Latitude.Value,
                ["longitude"] = data.Longitude.Value
            };
        }

        return Task.FromResult(jsonLdGenerator.Generate(schema));
    }
}
