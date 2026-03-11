# Baseline v3 for Xperience by Kentico

A streamlined foundation for Xperience by Kentico websites with built-in SEO, navigation, account management, automation, and e-commerce support.

## Version

**Baseline v3.0.0** - Compatible with Xperience by Kentico 31.x

## Modules

| Module | Description |
|--------|-------------|
| **Core** | Foundation services including SEO, caching, structured data, and content retrieval |
| **Account** | Authentication and user management with external providers support |
| **Navigation** | Dynamic menu and breadcrumb generation |
| **Localization** | Multi-language content and UI localization |
| **Search** | Full-text search with Lucene integration |
| **Ecommerce** | Shopping cart, checkout, and order management |
| **Automation** | Contact-based workflow automation engine |
| **AI** | AI-powered content and admin features |
| **Forms** | Form builder and submission handling |
| **SEO** | Search engine optimization tools |
| **Digital Marketing** | Marketing automation integration |
| **Data Protection** | GDPR compliance and consent management |
| **Tools** | CLI utilities for development and deployment |
| **MediaTools** | Image and media asset management |
| **TabbedPages** | Tabbed content page templates |
| **Experiments** | A/B testing and experimentation |
| **EmailMarketing** | Email campaign management |

## Getting Started

```csharp
// In Program.cs
builder.Services.AddBaselineCore();
builder.Services.AddBaselineAccount();
builder.Services.AddBaselineNavigation();
builder.Services.AddBaselineAutomation();
// ... add other modules as needed
```

## Documentation

For full documentation, visit the [GitHub repository](https://github.com/alsultan2021/baseline-v3).

## License

MIT License - See LICENSE file for details.

## Credits

Based on [XperienceCommunity.Baseline](https://github.com/KenticoDevTrev/XperienceCommunity.Baseline) by Trevor Fayas.
