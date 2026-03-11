---
applyTo: "**"
---

# Project Generation Conventions

Shared knowledge base for Xperience by Kentico project generation. These
conventions apply to all generated code, content types, and project structures.

## Three-Project Pattern

Complex features use Core + Admin + RCL:

| Project | Purpose                                 | SDK                        | Target Framework |
| ------- | --------------------------------------- | -------------------------- | ---------------- |
| Core    | Business logic, services, models, DI    | `Microsoft.NET.Sdk`        | net10.0          |
| Admin   | Admin UI, React client, form components | `Microsoft.NET.Sdk`        | net10.0          |
| RCL     | Razor views, ViewComponents, widgets    | `Microsoft.NET.Sdk.Razor`  | net10.0          |

## Project Naming

```
{RootNamespace}.{Feature}/
├── {RootNamespace}.{Feature}.Core/
├── {RootNamespace}.{Feature}.Admin/
└── {RootNamespace}.{Feature}.RCL/
```

## Naming Conventions

| Item                   | Convention                    | Example                       |
| ---------------------- | ----------------------------- | ----------------------------- |
| Content type code name | `{Namespace}.{TypeName}`      | `DancingGoat.CafePage`        |
| Field code name        | `{TypeShortName}{FieldName}`  | `CafePageTitle`               |
| Reusable schema name   | `{Namespace}.{SchemaName}`    | `DancingGoat.SEOFields`       |
| Module class           | `{Feature}Module`             | `GalleryModule`               |
| DI extension method    | `Add{Feature}()`              | `AddGallery(configuration)`   |
| DI extension class     | `{Feature}ServiceCollectionExtensions` | `GalleryServiceCollectionExtensions` |
| Service interface      | `I{Feature}Service`           | `IGalleryService`             |
| Service implementation | `{Feature}Service`            | `GalleryService`              |
| Admin module           | `{Feature}AdminModule`        | `GalleryAdminModule`          |
| DTO record             | `{TypeShortName}DTO`          | `CafePageDTO`                 |
| View model             | `{TypeShortName}ViewModel`    | `CafePageViewModel`           |
| Mapper extension       | `{TypeShortName}Mapper`       | `CafePageMapper`              |
| Query service          | `I{TypeShortName}QueryService`| `ICafePageQueryService`       |

## .csproj Templates

### Core Project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Kentico.Xperience.Core" />
  </ItemGroup>
</Project>
```

### RCL Project

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Kentico.Xperience.WebApp" />
  </ItemGroup>
</Project>
```

### Admin Project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Kentico.Xperience.Admin" />
  </ItemGroup>
</Project>
```

## DI Registration Pattern

Each project exposes a single extension method:

```csharp
public static class {Feature}ServiceCollectionExtensions
{
    public static IServiceCollection Add{Feature}(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<I{Feature}Service, {Feature}Service>();
        return services;
    }
}
```

## Module Registration Pattern

Features needing database tables or custom objects use an Xperience Module:

```csharp
[assembly: RegisterModule(typeof({Feature}Module))]

public class {Feature}Module : Module
{
    public {Feature}Module() : base(nameof({Feature}Module)) { }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);
        // Register event handlers, custom providers
    }
}
```

## Content Type Integration

For each content type, generate:

1. **DTO** — Record type mapping Kentico fields to C# properties
2. **Mapper** — Extension method converting generated class to DTO
3. **Query Service** — `IContentRetriever`-based retrieval service

### DTO Pattern

```csharp
public record {TypeShortName}DTO(
    string Title,
    string Description,
    // ... mapped fields
);
```

### Mapper Pattern

```csharp
public static class {TypeShortName}Mapper
{
    public static {TypeShortName}DTO ToDTO(this {TypeName} source)
    {
        return new {TypeShortName}DTO(
            Title: source.{TypeShortName}Title,
            Description: source.{TypeShortName}Description
        );
    }
}
```

### Query Service Pattern

```csharp
public interface I{TypeShortName}QueryService
{
    Task<IEnumerable<{TypeShortName}DTO>> GetAllAsync(
        CancellationToken cancellationToken = default);
}

public class {TypeShortName}QueryService(
    IContentRetriever contentRetriever) : I{TypeShortName}QueryService
{
    public async Task<IEnumerable<{TypeShortName}DTO>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var builder = new ContentItemQueryBuilder()
            .ForContentType(
                {TypeName}.CONTENT_TYPE_NAME,
                q => q.OrderBy(nameof({TypeName}.{TypeShortName}Title)));

        var items = await contentRetriever.GetContentAsync<{TypeName}>(
            builder, cancellationToken: cancellationToken);

        return items.Select(x => x.ToDTO());
    }
}
```

## Admin Client Pattern

React/TypeScript admin clients use:

- Webpack for bundling (Kentico's webpack config)
- TypeScript strict mode
- Kentico admin SDK (`@kentico/xperience-admin-base`)

### Client Directory Structure

```
Client/
├── src/
│   ├── index.tsx
│   └── components/
├── package.json
├── webpack.config.js
└── tsconfig.json
```

## Content Type Classification

| Type         | Use When                                    |
| ------------ | ------------------------------------------- |
| **Reusable** | Shared content referenced by multiple pages |
| **Website**  | Content with URLs, page builder, routing    |
| **Email**    | Email campaign content                      |
| **Headless** | API-only content, no page builder           |

## Creation Order (Dependencies First)

1. Reusable field schemas
2. Reusable content types (referenced by others)
3. Website content types
4. Core project (business logic)
5. Admin project (admin UI)
6. RCL project (frontend)
7. Module registration
8. Solution file references

## C# Code Standards

- Use `IContentRetriever` for queries — **never** `IInfoProvider`
- Use `ILogger<T>` for logging — **never** `IEventLogService`
- Use async methods when available
- Use primary constructor syntax for services
- Use record types for DTOs
- Use init-only properties for view models
- Use file-scoped namespaces
- Use nullable reference types

## Common Anti-Patterns

❌ Project without Core sub-project
❌ Admin client without webpack config
❌ RCL without `_ViewImports.cshtml`
❌ Service without interface
❌ Content type without DTO
❌ Module without DI extension method
❌ Widget properties in content type fields
❌ `IInfoProvider` for content queries
❌ Synchronous methods when async available
❌ `IEventLogService` instead of `ILogger<T>`
