using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Options;

using ModelContextProtocol.Server;

namespace Baseline.Core.MCP.Tools;

/// <summary>
/// SEO verification and guidance tools — audits HTML for proper SEO practices,
/// generates meta tag templates, validates heading hierarchy, checks accessibility
/// overlap, and provides structured data snippets for Schema.org markup.
/// </summary>
[McpServerToolType]
public static partial class SeoVerificationTool
{
    // ───────────────────────── audit rules ─────────────────────────

    #region Audit Rules

    private static readonly AuditRule[] AuditRules =
    [
        // ── Meta & Head ──
        new("meta-title", "Meta Title", "Critical", "Head",
            @"<title\b[^>]*>.+?</title>",
            "Page must have a <title> tag inside <head>. Keep 50-60 characters. Include primary keyword near the start.",
            """<title>Primary Keyword - Secondary Keyword | Brand Name</title>"""),

        new("meta-description", "Meta Description", "Critical", "Head",
            @"<meta\s+name=[""']description[""']\s+content=[""'][^""']+[""']",
            "Page must have a meta description. Keep 150-160 characters. Include primary keyword and a call to action.",
            """<meta name="description" content="Concise page description with primary keyword. Include a call to action. Keep under 160 characters." />"""),

        new("meta-viewport", "Viewport Meta", "Critical", "Head",
            @"<meta\s+name=[""']viewport[""']",
            "Page must include viewport meta for mobile responsiveness.",
            """<meta name="viewport" content="width=device-width, initial-scale=1.0" />"""),

        new("meta-charset", "Charset Declaration", "Critical", "Head",
            @"<meta\s+charset=[""']UTF-8[""']",
            "Page must declare UTF-8 charset as first meta tag in <head>.",
            """<meta charset="UTF-8" />"""),

        new("canonical-url", "Canonical URL", "High", "Head",
            @"<link\s+rel=[""']canonical[""']\s+href=[""'][^""']+[""']",
            "Page should have a canonical URL to prevent duplicate content issues. Must be absolute URL.",
            """<link rel="canonical" href="https://example.com/page-url" />"""),

        new("meta-robots", "Robots Meta", "Medium", "Head",
            @"<meta\s+name=[""']robots[""']\s+content=[""'][^""']+[""']",
            "Add robots meta to control indexing. Use 'index, follow' for public pages, 'noindex, nofollow' for private pages.",
            """<meta name="robots" content="index, follow" />"""),

        new("lang-attr", "HTML Language", "High", "Head",
            @"<html[^>]+lang=[""'][a-z]{2}",
            "The <html> element must have a lang attribute for accessibility and SEO.",
            """<html lang="en" dir="ltr">"""),

        new("favicon", "Favicon", "Medium", "Head",
            @"<link\s+rel=[""'](icon|shortcut icon)[""']",
            "Page should include a favicon for branding in browser tabs and bookmarks.",
            """<link rel="icon" type="image/png" href="/images/favicon-96x96.png" sizes="96x96" />"""),

        // ── Open Graph ──
        new("og-title", "Open Graph Title", "High", "OpenGraph",
            @"<meta\s+property=[""']og:title[""']\s+content=[""'][^""']+[""']",
            "Add og:title for social media sharing. Should match or closely reflect the page title.",
            """<meta property="og:title" content="Page Title Here" />"""),

        new("og-description", "Open Graph Description", "High", "OpenGraph",
            @"<meta\s+property=[""']og:description[""']\s+content=[""'][^""']+[""']",
            "Add og:description for social media sharing previews.",
            """<meta property="og:description" content="Brief description for social sharing." />"""),

        new("og-image", "Open Graph Image", "High", "OpenGraph",
            @"<meta\s+property=[""']og:image[""']\s+content=[""'][^""']+[""']",
            "Add og:image (1200x630px recommended) for social media sharing. Must be absolute URL.",
            """<meta property="og:image" content="https://example.com/images/og-image.jpg" />"""),

        new("og-url", "Open Graph URL", "Medium", "OpenGraph",
            @"<meta\s+property=[""']og:url[""']\s+content=[""'][^""']+[""']",
            "Add og:url pointing to the canonical URL of the page.",
            """<meta property="og:url" content="https://example.com/page-url" />"""),

        new("og-type", "Open Graph Type", "Medium", "OpenGraph",
            @"<meta\s+property=[""']og:type[""']\s+content=[""'][^""']+[""']",
            "Add og:type (website, article, product, etc.) for correct social media rendering.",
            """<meta property="og:type" content="website" />"""),

        new("og-sitename", "Open Graph Site Name", "Low", "OpenGraph",
            @"<meta\s+property=[""']og:site_name[""']\s+content=[""'][^""']+[""']",
            "Add og:site_name for brand identification in social shares.",
            """<meta property="og:site_name" content="Your Brand Name" />"""),

        // ── Twitter Card ──
        new("twitter-card", "Twitter Card", "Medium", "Twitter",
            @"<meta\s+(name|property)=[""']twitter:card[""']\s+content=[""'][^""']+[""']",
            "Add twitter:card meta (summary_large_image recommended) for Twitter sharing.",
            """<meta name="twitter:card" content="summary_large_image" />"""),

        new("twitter-title", "Twitter Title", "Medium", "Twitter",
            @"<meta\s+(name|property)=[""']twitter:title[""']\s+content=[""'][^""']+[""']",
            "Add twitter:title for Twitter card previews. Max 70 characters.",
            """<meta name="twitter:title" content="Page Title for Twitter" />"""),

        new("twitter-description", "Twitter Description", "Medium", "Twitter",
            @"<meta\s+(name|property)=[""']twitter:description[""']\s+content=[""'][^""']+[""']",
            "Add twitter:description for Twitter card previews. Max 200 characters.",
            """<meta name="twitter:description" content="Page description for Twitter card." />"""),

        new("twitter-image", "Twitter Image", "Medium", "Twitter",
            @"<meta\s+(name|property)=[""']twitter:image[""']\s+content=[""'][^""']+[""']",
            "Add twitter:image for Twitter card image. Min 300x157px, max 4096x4096px.",
            """<meta name="twitter:image" content="https://example.com/images/twitter-card.jpg" />"""),

        // ── Heading Hierarchy ──
        new("single-h1", "Single H1 Tag", "Critical", "Headings",
            @"<h1[\s>]",
            "Page must have exactly ONE <h1> tag. It should contain the primary keyword and describe the page content.",
            """<h1 class="fw-600 text-dark-gray ls-minus-1px">Primary Keyword in Main Heading</h1>"""),

        new("heading-hierarchy", "Heading Hierarchy", "High", "Headings",
            @"<h[1-6][\s>]",
            "Headings must follow sequential order (h1 > h2 > h3). Never skip levels (e.g., h1 then h3). Each section should use h2, subsections h3.",
            "h1 (one) > h2 (sections) > h3 (subsections) > h4 (details)"),

        // ── Images ──
        new("img-alt", "Image Alt Text", "Critical", "Images",
            @"<img\b[^>]+alt=[""'][^""']+[""']",
            "ALL <img> tags must have descriptive alt text. Use keywords naturally. Decorative images: alt=\"\".",
            """<img src="/images/photo.jpg" alt="Descriptive text with keywords" class="w-100" />"""),

        new("img-lazy", "Lazy Loading", "Medium", "Images",
            @"loading=[""']lazy[""']",
            "Add loading=\"lazy\" to below-the-fold images for better page speed.",
            """<img src="/images/photo.jpg" alt="Description" loading="lazy" width="600" height="400" />"""),

        new("img-dimensions", "Image Dimensions", "Medium", "Images",
            @"<img\b[^>]+(width|height)=[""']\d+[""']",
            "Include width and height attributes on images to prevent Cumulative Layout Shift (CLS).",
            """<img src="/images/photo.jpg" alt="Description" width="600" height="400" />"""),

        new("img-modern-format", "Modern Image Formats", "Low", "Images",
            @"<(picture|source)\b|\.webp|\.avif",
            "Use <picture> with WebP/AVIF sources for modern format support with fallback.",
            """
<picture>
  <source srcset="/images/photo.avif" type="image/avif" />
  <source srcset="/images/photo.webp" type="image/webp" />
  <img src="/images/photo.jpg" alt="Description" width="600" height="400" loading="lazy" />
</picture>
"""),

        // ── Links ──
        new("link-descriptive", "Descriptive Link Text", "High", "Links",
            @"<a\b[^>]+>[^<]{3,}",
            "Links should have descriptive anchor text. Avoid 'click here' or 'read more' alone. Screen readers and crawlers need meaningful text.",
            """<a href="/services" class="btn btn-medium btn-dark-gray">View our design services</a>"""),

        new("external-noopener", "External Link Security", "Medium", "Links",
            @"target=[""']_blank[""'][^>]*rel=[""'][^""']*noopener",
            "External links with target=\"_blank\" must include rel=\"noopener noreferrer\" for security.",
            """<a href="https://external.com" target="_blank" rel="noopener noreferrer">External Site</a>"""),

        // ── Structured Data ──
        new("schema-org", "Schema.org Structured Data", "High", "StructuredData",
            @"(application/ld\+json|itemscope|itemtype=[""']https?://schema\.org)",
            "Add Schema.org structured data (JSON-LD preferred) for rich search results.",
            """<script type="application/ld+json">{"@context":"https://schema.org","@type":"WebPage","name":"Page Title"}</script>"""),

        // ── Accessibility (SEO overlap) ──
        new("aria-landmarks", "ARIA Landmarks", "Medium", "Accessibility",
            @"role=[""'](banner|navigation|main|contentinfo)[""']|<(header|nav|main|footer)[\s>]",
            "Use semantic HTML5 elements (header, nav, main, footer) or ARIA roles for landmark regions.",
            """
<header><!-- site header --></header>
<nav aria-label="Main navigation"><!-- nav --></nav>
<main><!-- page content --></main>
<footer><!-- site footer --></footer>
"""),

        new("skip-navigation", "Skip Navigation Link", "Medium", "Accessibility",
            @"skip.?(to|nav|content|main)",
            "Include a skip navigation link for keyboard users and screen readers.",
            """<a href="#main-content" class="skip-to-main-content-link">Skip to main content</a>"""),

        new("form-labels", "Form Input Labels", "High", "Accessibility",
            @"<label\b[^>]+for=[""'][^""']+[""']|aria-label=[""'][^""']+[""']",
            "All form inputs must have associated <label> elements (via for/id) or aria-label attributes.",
            """
<label for="email">Email Address</label>
<input type="email" id="email" name="email" class="form-control" aria-required="true" />
"""),

        // ── Performance ──
        new("preload-critical", "Preload Critical Resources", "Medium", "Performance",
            @"<link\s+rel=[""']preload[""']",
            "Preload critical CSS/fonts/hero images with <link rel=\"preload\"> for faster rendering.",
            """<link rel="preload" href="/css/combined.min.css" as="style" />"""),

        new("async-defer-scripts", "Async/Defer Scripts", "Medium", "Performance",
            @"<script\b[^>]+(async|defer)",
            "Non-critical scripts should use async or defer attributes to avoid blocking rendering.",
            """<script src="/js/analytics.js" async></script>"""),

        new("preconnect", "Preconnect External Origins", "Low", "Performance",
            @"<link\s+rel=[""']preconnect[""']",
            "Use <link rel=\"preconnect\"> for external origins (fonts, CDNs, analytics) to reduce DNS/TLS time.",
            """<link rel="preconnect" href="https://fonts.googleapis.com" />""")
    ];

    #endregion

    #region Structured Data Templates

    private static readonly Dictionary<string, StructuredDataTemplate> SchemaTemplates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["webpage"] = new("WebPage", "Generic web page", """
{
  "@context": "https://schema.org",
  "@type": "WebPage",
  "name": "{{title}}",
  "description": "{{description}}",
  "url": "{{url}}",
  "inLanguage": "{{language}}",
  "isPartOf": {
    "@type": "WebSite",
    "name": "{{siteName}}",
    "url": "{{siteUrl}}"
  }
}
"""),
        ["organization"] = new("Organization", "Business/organization with logo and contact", """
{
  "@context": "https://schema.org",
  "@type": "Organization",
  "name": "{{name}}",
  "url": "{{url}}",
  "logo": "{{logoUrl}}",
  "description": "{{description}}",
  "address": {
    "@type": "PostalAddress",
    "streetAddress": "{{street}}",
    "addressLocality": "{{city}}",
    "addressRegion": "{{region}}",
    "postalCode": "{{postalCode}}",
    "addressCountry": "{{country}}"
  },
  "contactPoint": {
    "@type": "ContactPoint",
    "telephone": "{{phone}}",
    "contactType": "customer service",
    "email": "{{email}}"
  },
  "sameAs": [
    "{{facebookUrl}}",
    "{{twitterUrl}}",
    "{{linkedinUrl}}"
  ]
}
"""),
        ["localbusiness"] = new("LocalBusiness", "Local business with hours and location", """
{
  "@context": "https://schema.org",
  "@type": "LocalBusiness",
  "name": "{{name}}",
  "url": "{{url}}",
  "image": "{{imageUrl}}",
  "telephone": "{{phone}}",
  "email": "{{email}}",
  "priceRange": "{{priceRange}}",
  "address": {
    "@type": "PostalAddress",
    "streetAddress": "{{street}}",
    "addressLocality": "{{city}}",
    "addressRegion": "{{region}}",
    "postalCode": "{{postalCode}}",
    "addressCountry": "{{country}}"
  },
  "geo": {
    "@type": "GeoCoordinates",
    "latitude": "{{latitude}}",
    "longitude": "{{longitude}}"
  },
  "openingHoursSpecification": [
    {
      "@type": "OpeningHoursSpecification",
      "dayOfWeek": ["Monday","Tuesday","Wednesday","Thursday","Friday"],
      "opens": "09:00",
      "closes": "17:00"
    }
  ]
}
"""),
        ["article"] = new("Article", "Blog post or news article", """
{
  "@context": "https://schema.org",
  "@type": "Article",
  "headline": "{{title}}",
  "description": "{{description}}",
  "image": "{{imageUrl}}",
  "datePublished": "{{publishDate}}",
  "dateModified": "{{modifiedDate}}",
  "author": {
    "@type": "Person",
    "name": "{{authorName}}",
    "url": "{{authorUrl}}"
  },
  "publisher": {
    "@type": "Organization",
    "name": "{{publisherName}}",
    "logo": {
      "@type": "ImageObject",
      "url": "{{publisherLogoUrl}}"
    }
  },
  "mainEntityOfPage": {
    "@type": "WebPage",
    "@id": "{{url}}"
  }
}
"""),
        ["product"] = new("Product", "E-commerce product with price and availability", """
{
  "@context": "https://schema.org",
  "@type": "Product",
  "name": "{{name}}",
  "description": "{{description}}",
  "image": "{{imageUrl}}",
  "sku": "{{sku}}",
  "brand": {
    "@type": "Brand",
    "name": "{{brandName}}"
  },
  "offers": {
    "@type": "Offer",
    "priceCurrency": "{{currency}}",
    "price": "{{price}}",
    "availability": "https://schema.org/InStock",
    "url": "{{url}}"
  },
  "aggregateRating": {
    "@type": "AggregateRating",
    "ratingValue": "{{rating}}",
    "reviewCount": "{{reviewCount}}"
  }
}
"""),
        ["faqpage"] = new("FAQPage", "FAQ page with question/answer pairs", """
{
  "@context": "https://schema.org",
  "@type": "FAQPage",
  "mainEntity": [
    {
      "@type": "Question",
      "name": "{{question1}}",
      "acceptedAnswer": {
        "@type": "Answer",
        "text": "{{answer1}}"
      }
    },
    {
      "@type": "Question",
      "name": "{{question2}}",
      "acceptedAnswer": {
        "@type": "Answer",
        "text": "{{answer2}}"
      }
    }
  ]
}
"""),
        ["breadcrumb"] = new("BreadcrumbList", "Breadcrumb navigation for search results", """
{
  "@context": "https://schema.org",
  "@type": "BreadcrumbList",
  "itemListElement": [
    {
      "@type": "ListItem",
      "position": 1,
      "name": "Home",
      "item": "{{siteUrl}}"
    },
    {
      "@type": "ListItem",
      "position": 2,
      "name": "{{parentName}}",
      "item": "{{parentUrl}}"
    },
    {
      "@type": "ListItem",
      "position": 3,
      "name": "{{currentName}}"
    }
  ]
}
"""),
        ["event"] = new("Event", "Event with date, location and ticket info", """
{
  "@context": "https://schema.org",
  "@type": "Event",
  "name": "{{name}}",
  "description": "{{description}}",
  "startDate": "{{startDate}}",
  "endDate": "{{endDate}}",
  "eventStatus": "https://schema.org/EventScheduled",
  "eventAttendanceMode": "https://schema.org/OfflineEventAttendanceMode",
  "location": {
    "@type": "Place",
    "name": "{{venueName}}",
    "address": {
      "@type": "PostalAddress",
      "streetAddress": "{{street}}",
      "addressLocality": "{{city}}",
      "addressCountry": "{{country}}"
    }
  },
  "offers": {
    "@type": "Offer",
    "price": "{{price}}",
    "priceCurrency": "{{currency}}",
    "url": "{{ticketUrl}}",
    "availability": "https://schema.org/InStock"
  },
  "organizer": {
    "@type": "Organization",
    "name": "{{organizerName}}",
    "url": "{{organizerUrl}}"
  }
}
"""),
        ["hotel"] = new("Hotel", "Hotel/resort with amenities and rating", """
{
  "@context": "https://schema.org",
  "@type": "Hotel",
  "name": "{{name}}",
  "description": "{{description}}",
  "url": "{{url}}",
  "image": "{{imageUrl}}",
  "telephone": "{{phone}}",
  "priceRange": "{{priceRange}}",
  "starRating": {
    "@type": "Rating",
    "ratingValue": "{{stars}}"
  },
  "address": {
    "@type": "PostalAddress",
    "streetAddress": "{{street}}",
    "addressLocality": "{{city}}",
    "addressCountry": "{{country}}"
  },
  "amenityFeature": [
    { "@type": "LocationFeatureSpecification", "name": "Free WiFi" },
    { "@type": "LocationFeatureSpecification", "name": "Swimming Pool" },
    { "@type": "LocationFeatureSpecification", "name": "Spa" }
  ],
  "checkinTime": "15:00",
  "checkoutTime": "11:00"
}
"""),
        ["restaurant"] = new("Restaurant", "Restaurant with cuisine, hours and menu", """
{
  "@context": "https://schema.org",
  "@type": "Restaurant",
  "name": "{{name}}",
  "description": "{{description}}",
  "url": "{{url}}",
  "image": "{{imageUrl}}",
  "telephone": "{{phone}}",
  "servesCuisine": "{{cuisine}}",
  "priceRange": "{{priceRange}}",
  "menu": "{{menuUrl}}",
  "address": {
    "@type": "PostalAddress",
    "streetAddress": "{{street}}",
    "addressLocality": "{{city}}",
    "addressCountry": "{{country}}"
  },
  "openingHoursSpecification": [
    {
      "@type": "OpeningHoursSpecification",
      "dayOfWeek": ["Monday","Tuesday","Wednesday","Thursday","Friday","Saturday"],
      "opens": "11:00",
      "closes": "22:00"
    }
  ],
  "aggregateRating": {
    "@type": "AggregateRating",
    "ratingValue": "{{rating}}",
    "reviewCount": "{{reviewCount}}"
  }
}
""")
    };

    #endregion

    #region Meta Tag Templates by Page Type

    private static readonly Dictionary<string, string[]> MetaTemplatesByPage = new(StringComparer.OrdinalIgnoreCase)
    {
        ["homepage"] =
        [
            """<title>Brand Name - Primary Value Proposition | City/Service</title>""",
            """<meta name="description" content="Brand Name offers [primary service]. Serving [area/audience] with [key differentiator]. [Call to action]." />""",
            """<link rel="canonical" href="https://example.com/" />""",
            """<meta name="robots" content="index, follow" />""",
            """<meta property="og:title" content="Brand Name - Primary Value Proposition" />""",
            """<meta property="og:description" content="Short social-friendly description of the brand." />""",
            """<meta property="og:image" content="https://example.com/images/og-homepage.jpg" />""",
            """<meta property="og:url" content="https://example.com/" />""",
            """<meta property="og:type" content="website" />""",
            """<meta property="og:site_name" content="Brand Name" />""",
            """<meta name="twitter:card" content="summary_large_image" />""",
            """<meta name="twitter:title" content="Brand Name - Primary Value Proposition" />""",
            """<meta name="twitter:description" content="Short Twitter-friendly description." />""",
            """<meta name="twitter:image" content="https://example.com/images/twitter-homepage.jpg" />"""
        ],
        ["article"] =
        [
            """<title>Article Title - Category | Brand Name</title>""",
            """<meta name="description" content="Summary of the article in 150-160 chars. Include primary keyword and what reader will learn." />""",
            """<link rel="canonical" href="https://example.com/blog/article-slug" />""",
            """<meta name="robots" content="index, follow" />""",
            """<meta property="og:title" content="Article Title" />""",
            """<meta property="og:description" content="Short social-friendly article summary." />""",
            """<meta property="og:image" content="https://example.com/images/article-featured.jpg" />""",
            """<meta property="og:url" content="https://example.com/blog/article-slug" />""",
            """<meta property="og:type" content="article" />""",
            """<meta property="article:published_time" content="2026-01-15T09:00:00Z" />""",
            """<meta property="article:modified_time" content="2026-01-20T14:30:00Z" />""",
            """<meta property="article:author" content="Author Name" />""",
            """<meta name="twitter:card" content="summary_large_image" />"""
        ],
        ["product"] =
        [
            """<title>Product Name - Category | Brand Name</title>""",
            """<meta name="description" content="Buy Product Name. [Key features]. [Price if public]. Free shipping on orders over $X. Shop now." />""",
            """<link rel="canonical" href="https://example.com/shop/product-slug" />""",
            """<meta name="robots" content="index, follow" />""",
            """<meta property="og:title" content="Product Name - $Price" />""",
            """<meta property="og:description" content="Product description for social sharing." />""",
            """<meta property="og:image" content="https://example.com/images/product-photo.jpg" />""",
            """<meta property="og:type" content="product" />""",
            """<meta property="product:price:amount" content="99.99" />""",
            """<meta property="product:price:currency" content="USD" />""",
            """<meta name="twitter:card" content="summary_large_image" />"""
        ],
        ["service"] =
        [
            """<title>Service Name - Service Type | Brand Name</title>""",
            """<meta name="description" content="Professional [service type] by Brand Name. [Key benefit]. [Experience/credentials]. Get a free consultation." />""",
            """<link rel="canonical" href="https://example.com/services/service-slug" />""",
            """<meta name="robots" content="index, follow" />""",
            """<meta property="og:title" content="Service Name | Brand Name" />""",
            """<meta property="og:description" content="Brief service description for social sharing." />""",
            """<meta property="og:image" content="https://example.com/images/service-og.jpg" />""",
            """<meta property="og:type" content="website" />""",
            """<meta name="twitter:card" content="summary_large_image" />"""
        ],
        ["contact"] =
        [
            """<title>Contact Us | Brand Name - Get in Touch</title>""",
            """<meta name="description" content="Contact Brand Name at [phone] or [email]. Visit us at [address]. We're available [hours]. Get a free quote today." />""",
            """<link rel="canonical" href="https://example.com/contact" />""",
            """<meta name="robots" content="index, follow" />""",
            """<meta property="og:title" content="Contact Us | Brand Name" />""",
            """<meta property="og:type" content="website" />"""
        ],
        ["landing"] =
        [
            """<title>Compelling Offer Headline | Brand Name</title>""",
            """<meta name="description" content="[Offer description]. [Urgency/scarcity]. [Call to action - Sign up/Download/Register today]." />""",
            """<link rel="canonical" href="https://example.com/campaign/landing-slug" />""",
            """<meta name="robots" content="noindex, follow" />""",
            """<meta property="og:title" content="Compelling Offer Headline" />""",
            """<meta property="og:description" content="Social sharing description for the offer." />""",
            """<meta property="og:image" content="https://example.com/images/landing-og.jpg" />""",
            """<meta property="og:type" content="website" />""",
            """<meta name="twitter:card" content="summary_large_image" />"""
        ],
        ["event"] =
        [
            """<title>Event Name - Date | Venue | Brand Name</title>""",
            """<meta name="description" content="Join us for [event] on [date] at [venue]. [Speakers/highlights]. Register now — limited seats available." />""",
            """<link rel="canonical" href="https://example.com/events/event-slug" />""",
            """<meta name="robots" content="index, follow" />""",
            """<meta property="og:title" content="Event Name - Date" />""",
            """<meta property="og:description" content="Event description for social sharing." />""",
            """<meta property="og:image" content="https://example.com/images/event-og.jpg" />""",
            """<meta property="og:type" content="website" />""",
            """<meta name="twitter:card" content="summary_large_image" />"""
        ]
    };

    #endregion

    #region Crafto-Specific SEO Patterns

    private static readonly Dictionary<string, string> CraftoSeoPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["section-heading-seo"] = """
<!-- SEO-friendly section heading pattern -->
<section class="bg-very-light-gray">
  <div class="container">
    <div class="row justify-content-center mb-4">
      <div class="col-xl-7 col-lg-8 text-center">
        <span class="fs-13 text-uppercase fw-700 text-base-color d-inline-block mb-10px">Section Category</span>
        <h2 class="alt-font text-dark-gray fw-600 ls-minus-1px">
          Section Title with <strong>Primary Keyword</strong>
        </h2>
        <p class="w-65 mx-auto md-w-90">
          Description text that includes relevant keywords naturally.
        </p>
      </div>
    </div>
  </div>
</section>
""",
        ["breadcrumb-with-schema"] = """
<!-- Crafto breadcrumb with Schema.org structured data -->
<section class="half-section page-title-center-alignment cover-background bg-very-light-gray top-space-margin">
  <div class="container">
    <div class="row">
      <div class="col-12 text-center position-relative page-title-large">
        <h1 class="d-inline-block fw-600 ls-minus-1px text-dark-gray mb-15px">Page Title with Keyword</h1>
      </div>
      <div class="col-12 breadcrumb breadcrumb-style-01 d-flex justify-content-center">
        <ol itemscope itemtype="https://schema.org/BreadcrumbList">
          <li itemprop="itemListElement" itemscope itemtype="https://schema.org/ListItem">
            <a itemprop="item" href="/"><span itemprop="name">Home</span></a>
            <meta itemprop="position" content="1" />
          </li>
          <li itemprop="itemListElement" itemscope itemtype="https://schema.org/ListItem">
            <a itemprop="item" href="/services"><span itemprop="name">Services</span></a>
            <meta itemprop="position" content="2" />
          </li>
          <li itemprop="itemListElement" itemscope itemtype="https://schema.org/ListItem">
            <span itemprop="name">Current Page</span>
            <meta itemprop="position" content="3" />
          </li>
        </ol>
      </div>
    </div>
  </div>
</section>
""",
        ["seo-image"] = """
<!-- SEO-optimized image with alt, dimensions, lazy loading -->
<figure class="mb-0">
  <picture>
    <source srcset="/images/photo.webp" type="image/webp" />
    <img src="/images/photo.jpg"
         alt="Descriptive alt text with keyword"
         width="600" height="400"
         loading="lazy"
         class="border-radius-6px w-100" />
  </picture>
  <figcaption class="fs-14 text-medium-gray mt-10px">
    Caption: additional context for search engines
  </figcaption>
</figure>
""",
        ["seo-link-cta"] = """
<!-- SEO-friendly CTA with descriptive anchor text -->
<a href="/services/web-design"
   class="btn btn-medium btn-switch-text btn-rounded btn-base-color btn-box-shadow"
   title="Learn more about our web design services">
  <span>
    <span class="btn-double-text" data-text="View web design services">
      View web design services
    </span>
  </span>
</a>
""",
        ["seo-internal-linking"] = """
<!-- SEO internal linking within content -->
<div class="col-lg-8 offset-lg-2">
  <p>
    Our <a href="/services/web-design" class="text-base-color fw-600">professional web design services</a>
    help businesses create compelling online experiences. Combined with our
    <a href="/services/seo" class="text-base-color fw-600">SEO optimization expertise</a>,
    we deliver measurable results.
  </p>
</div>
""",
        ["semantic-footer"] = """
<!-- SEO-friendly semantic footer -->
<footer class="footer-dark bg-extra-dark-slate-blue" role="contentinfo">
  <div class="container">
    <div class="row">
      <div class="col-lg-3 col-md-6">
        <a href="/" aria-label="Return to homepage">
          <img src="/images/logo-white.svg" alt="Brand Name logo" width="160" height="40" />
        </a>
        <p class="mt-20px">Brief brand description with keywords.</p>
      </div>
      <nav class="col-lg-3 col-md-6" aria-label="Footer navigation">
        <h3 class="text-white fw-600 fs-18 mb-20px">Quick Links</h3>
        <ul class="list-unstyled">
          <li><a href="/about" class="text-light-opacity">About Us</a></li>
          <li><a href="/services" class="text-light-opacity">Our Services</a></li>
          <li><a href="/contact" class="text-light-opacity">Contact Us</a></li>
        </ul>
      </nav>
      <div class="col-lg-3 col-md-6" itemscope itemtype="https://schema.org/Organization">
        <h3 class="text-white fw-600 fs-18 mb-20px">Contact</h3>
        <p class="text-light-opacity" itemprop="telephone">+1 (555) 123-4567</p>
        <p class="text-light-opacity" itemprop="email">info@example.com</p>
        <div itemprop="address" itemscope itemtype="https://schema.org/PostalAddress">
          <span itemprop="streetAddress" class="text-light-opacity">123 Main Street</span>
          <span itemprop="addressLocality" class="text-light-opacity">City</span>
        </div>
      </div>
    </div>
  </div>
</footer>
"""
    };

    #endregion

    // ───────────────────────── MCP Tool Methods ─────────────────────────

    /// <summary>
    /// Audits an HTML snippet against SEO best practices and returns pass/fail results.
    /// </summary>
    [McpServerTool(
        Name = nameof(AuditHtmlSeo),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Audit HTML for SEO"),
    Description("Audits an HTML snippet or full page HTML against SEO best practices. Returns pass/fail for each rule, with fix suggestions and code examples. Filter by category: 'Head', 'OpenGraph', 'Twitter', 'Headings', 'Images', 'Links', 'StructuredData', 'Accessibility', 'Performance'. Leave empty to run all checks.")]
    public static string AuditHtmlSeo(
        IOptions<BaselineMCPConfiguration> options,
        [Description("HTML content to audit (full page or section snippet)")] string html,
        [Description("Filter by audit category: 'Head', 'OpenGraph', 'Twitter', 'Headings', 'Images', 'Links', 'StructuredData', 'Accessibility', 'Performance'. Leave empty for all.")] string? category = null)
    {
        if (string.IsNullOrWhiteSpace(html))
            throw new ArgumentException("HTML content is required.", nameof(html));

        var rules = string.IsNullOrWhiteSpace(category)
            ? AuditRules
            : AuditRules.Where(r => r.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToArray();

        var results = new List<object>();
        int passed = 0;
        int failed = 0;
        int warnings = 0;

        foreach (var rule in rules)
        {
            bool found = Regex.IsMatch(html, rule.Pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Special case: single-h1 checks for exactly one h1
            if (rule.Id == "single-h1" && found)
            {
                int h1Count = Regex.Matches(html, @"<h1[\s>]", RegexOptions.IgnoreCase).Count;
                if (h1Count > 1)
                {
                    results.Add(new
                    {
                        rule.Id,
                        rule.Name,
                        Status = "FAIL",
                        rule.Severity,
                        rule.Category,
                        Issue = $"Found {h1Count} <h1> tags — page must have exactly ONE.",
                        rule.Guidance,
                        rule.FixSnippet
                    });
                    failed++;
                    continue;
                }
            }

            // Special case: heading hierarchy validation
            if (rule.Id == "heading-hierarchy" && found)
            {
                var headings = HeadingRegex().Matches(html);
                bool hierarchyValid = true;
                int prevLevel = 0;
                var hierarchyIssues = new List<string>();

                foreach (Match m in headings)
                {
                    int level = int.Parse(m.Groups[1].Value);
                    if (prevLevel > 0 && level > prevLevel + 1)
                    {
                        hierarchyIssues.Add($"h{prevLevel} jumps to h{level} (skipped h{prevLevel + 1})");
                        hierarchyValid = false;
                    }
                    prevLevel = level;
                }

                if (!hierarchyValid)
                {
                    results.Add(new
                    {
                        rule.Id,
                        rule.Name,
                        Status = "FAIL",
                        rule.Severity,
                        rule.Category,
                        Issue = $"Heading hierarchy broken: {string.Join("; ", hierarchyIssues)}",
                        rule.Guidance,
                        rule.FixSnippet
                    });
                    failed++;
                    continue;
                }
            }

            // Special case: check for imgs missing alt
            if (rule.Id == "img-alt")
            {
                var allImgs = Regex.Matches(html, @"<img\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                int missingAlt = 0;
                foreach (Match img in allImgs)
                {
                    if (!Regex.IsMatch(img.Value, @"alt=[""']", RegexOptions.IgnoreCase))
                        missingAlt++;
                }

                if (missingAlt > 0)
                {
                    results.Add(new
                    {
                        rule.Id,
                        rule.Name,
                        Status = "FAIL",
                        rule.Severity,
                        rule.Category,
                        Issue = $"{missingAlt} <img> tag(s) missing alt attribute.",
                        rule.Guidance,
                        rule.FixSnippet
                    });
                    failed++;
                    continue;
                }
                else if (allImgs.Count > 0)
                {
                    results.Add(new
                    {
                        rule.Id,
                        rule.Name,
                        Status = "PASS",
                        rule.Severity,
                        rule.Category,
                        Detail = $"All {allImgs.Count} images have alt attributes."
                    });
                    passed++;
                    continue;
                }
            }

            if (found)
            {
                results.Add(new
                {
                    rule.Id,
                    rule.Name,
                    Status = "PASS",
                    rule.Severity,
                    rule.Category
                });
                passed++;
            }
            else
            {
                string status = rule.Severity is "Critical" or "High" ? "FAIL" : "WARN";
                if (status == "FAIL") failed++;
                else warnings++;

                results.Add(new
                {
                    rule.Id,
                    rule.Name,
                    Status = status,
                    rule.Severity,
                    rule.Category,
                    rule.Guidance,
                    rule.FixSnippet
                });
            }
        }

        string grade = (passed, failed) switch
        {
            _ when failed == 0 && warnings == 0 => "A+",
            _ when failed == 0 => "A",
            _ when failed <= 2 => "B",
            _ when failed <= 5 => "C",
            _ when failed <= 8 => "D",
            _ => "F"
        };

        var result = new
        {
            Grade = grade,
            Summary = new { Passed = passed, Failed = failed, Warnings = warnings, Total = rules.Length },
            AvailableCategories = AuditRules.Select(r => r.Category).Distinct().OrderBy(c => c).ToArray(),
            Results = results
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Returns a complete set of meta tags for a specific page type.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetSeoMetaTagTemplate),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get SEO Meta Tag Template"),
    Description("Returns a complete meta tag template for a specific page type ('homepage', 'article', 'product', 'service', 'contact', 'landing', 'event'). Includes title, description, canonical, Open Graph, and Twitter Card tags with placeholder guidance.")]
    public static string GetSeoMetaTagTemplate(
        IOptions<BaselineMCPConfiguration> options,
        [Description("Page type: 'homepage', 'article', 'product', 'service', 'contact', 'landing', 'event'")] string pageType)
    {
        if (string.IsNullOrWhiteSpace(pageType))
            throw new ArgumentException("Page type is required.", nameof(pageType));

        if (!MetaTemplatesByPage.TryGetValue(pageType.Trim(), out string[]? tags))
        {
            return JsonSerializer.Serialize(new
            {
                Error = $"Unknown page type '{pageType}'.",
                AvailableTypes = MetaTemplatesByPage.Keys.OrderBy(k => k).ToArray()
            }, options.Value.SerializerOptions);
        }

        var result = new
        {
            PageType = pageType,
            MetaTags = tags,
            CombinedHtml = string.Join("\n", tags),
            TitleGuidelines = new
            {
                MaxLength = 60,
                Format = "Primary Keyword - Secondary | Brand Name",
                Tips = new[]
                {
                    "Place primary keyword at the start",
                    "Keep under 60 characters to avoid truncation in SERPs",
                    "Make each page title unique",
                    "Include brand name at the end after pipe or dash"
                }
            },
            DescriptionGuidelines = new
            {
                MaxLength = 160,
                Tips = new[]
                {
                    "Include primary keyword naturally in first 120 characters",
                    "Write a compelling summary that encourages clicks",
                    "End with a call to action when appropriate",
                    "Make each page description unique"
                }
            },
            XperienceIntegration = new
            {
                Note = "Xperience by Kentico pages should use the IWebPageMetaFields reusable schema",
                Fields = new[]
                {
                    "WebPageMetaTitle → <title> and og:title",
                    "WebPageMetaShortDescription → meta description and og:description",
                    "WebPageCanonicalURL → canonical link",
                    "WebPageMetaRobots → robots meta",
                    "WebPageMetaExcludeFromSitemap → controls sitemap inclusion"
                }
            }
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Returns Schema.org structured data templates in JSON-LD format.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetSchemaOrgTemplate),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Schema.org Structured Data Template"),
    Description("Returns JSON-LD structured data templates for Schema.org types: 'webpage', 'organization', 'localbusiness', 'article', 'product', 'faqpage', 'breadcrumb', 'event', 'hotel', 'restaurant'. Replace {{placeholder}} values with actual data.")]
    public static string GetSchemaOrgTemplate(
        IOptions<BaselineMCPConfiguration> options,
        [Description("Schema type: 'webpage', 'organization', 'localbusiness', 'article', 'product', 'faqpage', 'breadcrumb', 'event', 'hotel', 'restaurant'. Leave empty to list all.")] string? schemaType = null)
    {
        if (string.IsNullOrWhiteSpace(schemaType))
        {
            var catalog = SchemaTemplates.Select(t => new
            {
                Type = t.Key,
                SchemaName = t.Value.SchemaType,
                t.Value.Description
            });

            return JsonSerializer.Serialize(new
            {
                AvailableTemplates = catalog,
                Usage = "Wrap the JSON-LD in: <script type=\"application/ld+json\">...</script>",
                Tip = "Place structured data in <head> or at the end of <body>. One script block per entity."
            }, options.Value.SerializerOptions);
        }

        if (!SchemaTemplates.TryGetValue(schemaType.Trim(), out var template))
        {
            return JsonSerializer.Serialize(new
            {
                Error = $"Unknown schema type '{schemaType}'.",
                AvailableTypes = SchemaTemplates.Keys.OrderBy(k => k).ToArray()
            }, options.Value.SerializerOptions);
        }

        // Extract placeholders
        var placeholders = PlaceholderRegex()
            .Matches(template.JsonLdTemplate)
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToArray();

        var result = new
        {
            SchemaType = template.SchemaType,
            template.Description,
            JsonLdTemplate = template.JsonLdTemplate.Trim(),
            Placeholders = placeholders,
            HtmlWrapper = $"""
<script type="application/ld+json">
{template.JsonLdTemplate.Trim()}
</script>
""",
            ValidationUrl = "https://search.google.com/test/rich-results",
            Tips = new[]
            {
                "Replace all {{placeholder}} values with actual content",
                "Remove any properties you don't have data for",
                "Validate with Google Rich Results Test before deploying",
                "Use absolute URLs for all url/image properties",
                "Keep structured data consistent with visible page content"
            }
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Returns SEO-optimized Crafto HTML patterns for common page sections.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetCraftoSeoPatterns),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Crafto SEO-Optimized Patterns"),
    Description("Returns SEO-optimized HTML patterns specific to Crafto components. Includes proper heading hierarchy, Schema.org breadcrumbs, semantic images, descriptive CTAs, internal linking patterns, and semantic footer markup. Pattern keys: 'section-heading-seo', 'breadcrumb-with-schema', 'seo-image', 'seo-link-cta', 'seo-internal-linking', 'semantic-footer'.")]
    public static string GetCraftoSeoPatterns(
        IOptions<BaselineMCPConfiguration> options,
        [Description("Specific pattern key (e.g., 'breadcrumb-with-schema', 'seo-image'). Leave empty for all.")] string? patternKey = null)
    {
        if (!string.IsNullOrWhiteSpace(patternKey))
        {
            if (!CraftoSeoPatterns.TryGetValue(patternKey.Trim(), out string? pattern))
            {
                return JsonSerializer.Serialize(new
                {
                    Error = $"Unknown pattern '{patternKey}'.",
                    AvailablePatterns = CraftoSeoPatterns.Keys.OrderBy(k => k).ToArray()
                }, options.Value.SerializerOptions);
            }

            return JsonSerializer.Serialize(new
            {
                Pattern = patternKey,
                Html = pattern.Trim()
            }, options.Value.SerializerOptions);
        }

        var result = new
        {
            TotalPatterns = CraftoSeoPatterns.Count,
            Patterns = CraftoSeoPatterns.Select(p => new
            {
                Key = p.Key,
                Html = p.Value.Trim()
            }).ToArray(),
            GeneralSeoTips = new[]
            {
                "Every page needs exactly 1 <h1> with the primary keyword",
                "Use h2 for main sections, h3 for subsections — never skip levels",
                "All images: descriptive alt text + width/height + loading='lazy' for below-fold",
                "Links: descriptive anchor text, never 'click here'",
                "External links: target='_blank' rel='noopener noreferrer'",
                "Use <picture> with WebP/AVIF for modern image formats",
                "Add Schema.org JSON-LD for the primary entity on each page",
                "Breadcrumbs: use Schema.org BreadcrumbList markup",
                "Use semantic HTML5 elements: header, nav, main, article, section, footer",
                "Include skip-navigation link for accessibility",
                "Crafto's alt-font class should only be used on headings for SEO weight",
                "Keep text-to-HTML ratio high — avoid excessive wrapper divs where semantic elements work"
            }
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Returns a complete SEO checklist for a specific page type combining all verification areas.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetSeoChecklist),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get SEO Checklist"),
    Description("Returns a comprehensive SEO checklist for a specific page type. Covers technical SEO, on-page SEO, content, structured data, social sharing, performance, and accessibility. Use this before publishing any page.")]
    public static string GetSeoChecklist(
        IOptions<BaselineMCPConfiguration> options,
        [Description("Page type: 'homepage', 'article', 'product', 'service', 'contact', 'landing', 'event', 'general'")] string pageType = "general")
    {
        var commonChecks = new ChecklistSection[]
        {
            new("Technical SEO", [
                new("Unique <title> tag (50-60 chars) with primary keyword", "Critical"),
                new("Unique meta description (150-160 chars) with keyword + CTA", "Critical"),
                new("Canonical URL set (absolute URL)", "High"),
                new("Meta viewport for responsive design", "Critical"),
                new("UTF-8 charset declared", "Critical"),
                new("HTML lang attribute set", "High"),
                new("Robots meta matches indexing intent", "Medium"),
                new("No broken internal or external links", "High"),
                new("URL is clean, lowercase, hyphen-separated, keyword-rich", "High"),
                new("HTTPS enforced (no mixed content)", "Critical"),
                new("XML sitemap includes page (unless excluded)", "Medium"),
                new("Page loads under 3 seconds", "High")
            ]),
            new("On-Page SEO", [
                new("Exactly one H1 tag with primary keyword", "Critical"),
                new("Heading hierarchy: H1 > H2 > H3 (no skipped levels)", "High"),
                new("Primary keyword in first 100 words of body content", "High"),
                new("Keywords used naturally (no stuffing)", "Medium"),
                new("Internal links to related content (2-5 per page)", "High"),
                new("External links open in new tab with noopener", "Medium"),
                new("Descriptive anchor text on all links", "High"),
                new("Content is minimum 300-500 words for indexable pages", "Medium")
            ]),
            new("Images", [
                new("All <img> have descriptive alt text", "Critical"),
                new("Images have width and height attributes (CLS prevention)", "Medium"),
                new("Below-fold images have loading='lazy'", "Medium"),
                new("Modern formats used (WebP/AVIF with fallback)", "Low"),
                new("Images compressed and appropriately sized", "High"),
                new("Decorative images have alt='' (empty string)", "Medium")
            ]),
            new("Structured Data", [
                new("JSON-LD Schema.org block present for primary entity", "High"),
                new("Breadcrumb markup (Schema.org BreadcrumbList)", "Medium"),
                new("Validated with Google Rich Results Test", "High"),
                new("Structured data matches visible page content", "High")
            ]),
            new("Social Sharing", [
                new("Open Graph title, description, image, url, type set", "High"),
                new("OG image is 1200x630px (min) with absolute URL", "Medium"),
                new("Twitter Card tags set (summary_large_image)", "Medium"),
                new("Social preview tested with Facebook/Twitter debuggers", "Low")
            ]),
            new("Performance", [
                new("Critical CSS inlined or preloaded", "Medium"),
                new("Non-critical scripts use async/defer", "Medium"),
                new("External origins use preconnect/dns-prefetch", "Low"),
                new("Fonts use display=swap for FOIT prevention", "Medium"),
                new("No render-blocking resources in <head>", "High")
            ]),
            new("Accessibility (SEO Impact)", [
                new("Semantic HTML5 landmarks (header, nav, main, footer)", "Medium"),
                new("Skip-to-content link present", "Medium"),
                new("Form inputs have labels or aria-label", "High"),
                new("Color contrast meets WCAG AA (4.5:1 for text)", "Medium"),
                new("Focus indicators visible on interactive elements", "Medium"),
                new("ARIA roles used correctly where semantic HTML insufficient", "Low")
            ])
        };

        // Add page-type specific checks
        var pageSpecificChecks = pageType.ToLowerInvariant() switch
        {
            "article" => new ChecklistSection("Article-Specific", [
                new("Article structured data (headline, author, dates, publisher)", "High"),
                new("article:published_time and article:modified_time OG tags", "Medium"),
                new("Author bio with link (E-E-A-T signal)", "Medium"),
                new("Related posts section for internal linking", "Medium"),
                new("Category/tag taxonomy pages linked", "Low"),
                new("Table of contents for long articles (2000+ words)", "Low")
            ]),
            "product" => new ChecklistSection("Product-Specific", [
                new("Product structured data (name, price, availability, rating)", "Critical"),
                new("product:price OG tags", "Medium"),
                new("Multiple product images with unique alt text", "High"),
                new("Customer reviews with AggregateRating schema", "Medium"),
                new("Breadcrumb: Home > Category > Product", "Medium"),
                new("Price and availability visible above the fold", "High")
            ]),
            "homepage" => new ChecklistSection("Homepage-Specific", [
                new("Organization structured data with logo, contact, social", "High"),
                new("Clear value proposition in H1", "Critical"),
                new("Key service/product links in first viewport", "High"),
                new("Trust signals (logos, ratings, certifications) present", "Medium"),
                new("Site-wide navigation is crawlable (no JS-only nav)", "High")
            ]),
            "service" => new ChecklistSection("Service-Specific", [
                new("Service structured data or LocalBusiness schema", "High"),
                new("Clear service description with keyword in first paragraph", "High"),
                new("Pricing information visible (if applicable)", "Medium"),
                new("Call-to-action with descriptive text", "High"),
                new("Testimonials/reviews with structured data", "Medium")
            ]),
            "event" => new ChecklistSection("Event-Specific", [
                new("Event structured data (name, date, location, offers)", "Critical"),
                new("Event status (scheduled/cancelled/postponed)", "High"),
                new("Ticket/registration link prominently placed", "High"),
                new("Speaker/performer information with Person schema", "Medium"),
                new("Countdown timer for urgency (Crafto countdown component)", "Low")
            ]),
            "contact" => new ChecklistSection("Contact-Specific", [
                new("LocalBusiness or Organization structured data", "High"),
                new("NAP (Name, Address, Phone) consistent with Google Business", "Critical"),
                new("Google Maps embed or link to directions", "Medium"),
                new("Contact form has proper labels and aria attributes", "High"),
                new("Phone number is clickable (tel: link)", "Medium")
            ]),
            "landing" => new ChecklistSection("Landing-Specific", [
                new("Consider noindex if gated/campaign-specific content", "Medium"),
                new("Single clear CTA above the fold", "Critical"),
                new("Minimal navigation to reduce bounce", "Medium"),
                new("Social proof (testimonials, logos, stats) present", "High"),
                new("Form fields minimized for conversion", "Medium")
            ]),
            _ => null
        };

        var allSections = pageSpecificChecks is not null
            ? [.. commonChecks, pageSpecificChecks]
            : commonChecks;

        int totalChecks = allSections.Sum(s => s.Items.Length);

        var result = new
        {
            PageType = pageType,
            TotalChecks = totalChecks,
            Sections = allSections.Select(s => new
            {
                s.Name,
                ItemCount = s.Items.Length,
                Items = s.Items.Select(i => new { i.Check, i.Severity })
            }),
            AvailablePageTypes = new[] { "homepage", "article", "product", "service", "contact", "landing", "event", "general" }
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    // ───────────────────────── regex helpers ─────────────────────────

    [GeneratedRegex(@"<h([1-6])[\s>]", RegexOptions.IgnoreCase)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex PlaceholderRegex();

    // ───────────────────────── types ─────────────────────────

    private sealed record AuditRule(
        string Id,
        string Name,
        string Severity,
        string Category,
        string Pattern,
        string Guidance,
        string FixSnippet);

    private sealed record StructuredDataTemplate(
        string SchemaType,
        string Description,
        string JsonLdTemplate);

    private sealed record ChecklistSection(
        string Name,
        ChecklistItem[] Items);

    private sealed record ChecklistItem(
        string Check,
        string Severity);
}
