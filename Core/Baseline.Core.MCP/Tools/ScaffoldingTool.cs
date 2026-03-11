using System.ComponentModel;
using System.Text.Json;

using Microsoft.Extensions.Options;

using ModelContextProtocol.Server;

namespace Baseline.Core.MCP.Tools;

/// <summary>
/// Scaffolding plan tools for generating Baseline-compliant code structures.
/// These tools return deterministic file plans (no filesystem writes).
/// </summary>
[McpServerToolType]
public static class ScaffoldingTool
{
    /// <summary>
    /// Generates a feature module plan following Baseline conventions.
    /// Returns file paths and content for a complete feature structure.
    /// </summary>
    [McpServerTool(
        Name = nameof(GenerateFeatureModulePlan),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Generate Feature Module Plan"),
    Description("Generates a complete feature module structure plan following Baseline conventions. Returns file paths and content - no files are written.")]
    public static string GenerateFeatureModulePlan(
        IOptions<BaselineMCPConfiguration> options,
        [Description("Feature name in PascalCase (e.g., 'Booking', 'EventCalendar')")] string featureName,
        [Description("Include API controller (default: false)")] bool includeApi = false,
        [Description("Include MediatR operations (default: true)")] bool includeOperations = true,
        [Description("Include page template (default: true)")] bool includePageTemplate = true,
        [Description("Content type to integrate (optional, e.g., 'ChevalRoyal.BookingPage')")] string? contentType = null)
    {
        if (string.IsNullOrWhiteSpace(featureName))
            throw new ArgumentException("Feature name is required.", nameof(featureName));

        // Normalize feature name
        featureName = char.ToUpperInvariant(featureName[0]) + featureName[1..];
        var featureNameLower = char.ToLowerInvariant(featureName[0]) + featureName[1..];

        var files = new List<object>();
        var diRegistrations = new List<string>();

        // Base path
        string basePath = $"Features/{featureName}";

        // Page Template
        if (includePageTemplate)
        {
            files.Add(new
            {
                Path = $"{basePath}/{featureName}PageTemplate.cs",
                Type = "PageTemplate",
                Content = GeneratePageTemplateCs(featureName, contentType)
            });

            files.Add(new
            {
                Path = $"{basePath}/{featureName}PageTemplate.cshtml",
                Type = "RazorView",
                Content = GeneratePageTemplateCshtml(featureName)
            });

            files.Add(new
            {
                Path = $"{basePath}/{featureName}PageTemplate.css",
                Type = "Stylesheet",
                Content = GeneratePageTemplateCss(featureName)
            });
        }

        // MediatR Operations
        if (includeOperations)
        {
            files.Add(new
            {
                Path = $"{basePath}/Operations/Get{featureName}Query.cs",
                Type = "MediatRQuery",
                Content = GenerateQueryCs(featureName, contentType)
            });

            files.Add(new
            {
                Path = $"{basePath}/Operations/Get{featureName}QueryHandler.cs",
                Type = "MediatRHandler",
                Content = GenerateQueryHandlerCs(featureName, contentType)
            });

            files.Add(new
            {
                Path = $"{basePath}/Operations/{featureName}ViewModel.cs",
                Type = "ViewModel",
                Content = GenerateViewModelCs(featureName)
            });
        }

        // API Controller
        if (includeApi)
        {
            files.Add(new
            {
                Path = $"{basePath}/Api/{featureName}ApiController.cs",
                Type = "ApiController",
                Content = GenerateApiControllerCs(featureName)
            });

            diRegistrations.Add($"// {featureName} API endpoints registered via attribute routing");
        }

        // Service interface and implementation
        files.Add(new
        {
            Path = $"{basePath}/I{featureName}Service.cs",
            Type = "Interface",
            Content = GenerateServiceInterfaceCs(featureName)
        });

        files.Add(new
        {
            Path = $"{basePath}/{featureName}Service.cs",
            Type = "Service",
            Content = GenerateServiceCs(featureName, contentType)
        });

        diRegistrations.Add($"services.AddScoped<I{featureName}Service, {featureName}Service>();");

        // DI Extension
        files.Add(new
        {
            Path = $"{basePath}/{featureName}ServiceCollectionExtensions.cs",
            Type = "DIExtension",
            Content = GenerateDiExtensionCs(featureName, includeOperations)
        });

        var result = new
        {
            FeatureName = featureName,
            BasePath = basePath,
            ContentType = contentType,
            Options = new
            {
                IncludeApi = includeApi,
                IncludeOperations = includeOperations,
                IncludePageTemplate = includePageTemplate
            },
            Files = files,
            DiRegistrations = diRegistrations,
            ModuleEntryPoint = $"services.Add{featureName}Feature();",
            Instructions = new[]
            {
                $"1. Create files in 'Features/{featureName}/' directory",
                $"2. Add 'services.Add{featureName}Feature();' to Program.cs or ServiceCollectionAppExtensions.cs",
                "3. If using page template, register it in XperienceAdmin module",
                "4. Run 'dotnet build' to verify compilation"
            }
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Generates a content type integration plan with mapper, constants, and query service.
    /// </summary>
    [McpServerTool(
        Name = nameof(GenerateContentTypeIntegrationPlan),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Generate Content Type Integration Plan"),
    Description("Generates integration code for a content type including constants, mapper, and query service skeleton.")]
    public static string GenerateContentTypeIntegrationPlan(
        IOptions<BaselineMCPConfiguration> options,
        [Description("Content type code name (e.g., 'ChevalRoyal.BlogPost')")] string contentTypeName,
        [Description("DTO class name (e.g., 'BlogPostDto')")] string dtoName,
        [Description("Target folder path (default: 'Infrastructure/ContentTypes')")] string targetFolder = "Infrastructure/ContentTypes")
    {
        if (string.IsNullOrWhiteSpace(contentTypeName))
            throw new ArgumentException("Content type name is required.", nameof(contentTypeName));

        var typeParts = contentTypeName.Split('.');
        var shortName = typeParts.Last();
        dtoName = string.IsNullOrWhiteSpace(dtoName) ? $"{shortName}Dto" : dtoName;

        var files = new List<object>
        {
            new
            {
                Path = $"{targetFolder}/{shortName}Constants.cs",
                Type = "Constants",
                Content = $@"namespace Infrastructure.ContentTypes;

/// <summary>
/// Constants for {contentTypeName} content type.
/// </summary>
public static class {shortName}Constants
{{
    public const string ContentTypeName = ""{contentTypeName}"";
    public const string ContentTypeDisplayName = ""{shortName}"";

    public static class Fields
    {{
        // Add field name constants here
        // public const string Title = ""Title"";
    }}
}}"
            },
            new
            {
                Path = $"{targetFolder}/{dtoName}.cs",
                Type = "DTO",
                Content = $@"namespace Infrastructure.ContentTypes;

/// <summary>
/// Data transfer object for {contentTypeName}.
/// </summary>
public sealed record {dtoName}
{{
    public required Guid ContentItemGuid {{ get; init; }}
    public required string Name {{ get; init; }}
    
    // Add mapped properties here
}}"
            },
            new
            {
                Path = $"{targetFolder}/{shortName}Mapper.cs",
                Type = "Mapper",
                Content = $@"using CMS.ContentEngine;

namespace Infrastructure.ContentTypes;

/// <summary>
/// Maps {contentTypeName} content items to DTOs.
/// </summary>
public static class {shortName}Mapper
{{
    public static {dtoName} ToDto(IContentItemFieldsSource source)
    {{
        return new {dtoName}
        {{
            ContentItemGuid = source.SystemFields.ContentItemGUID,
            Name = source.SystemFields.ContentItemName,
            // Map additional fields here
        }};
    }}
    
    public static IEnumerable<{dtoName}> ToDtos(IEnumerable<IContentItemFieldsSource> sources) =>
        sources.Select(ToDto);
}}"
            },
            new
            {
                Path = $"{targetFolder}/I{shortName}QueryService.cs",
                Type = "Interface",
                Content = $@"namespace Infrastructure.ContentTypes;

/// <summary>
/// Query service for {contentTypeName} content items.
/// </summary>
public interface I{shortName}QueryService
{{
    Task<{dtoName}?> GetByGuidAsync(Guid guid, CancellationToken ct = default);
    Task<IEnumerable<{dtoName}>> GetAllAsync(int maxItems = 100, CancellationToken ct = default);
}}"
            },
            new
            {
                Path = $"{targetFolder}/{shortName}QueryService.cs",
                Type = "Service",
                Content = $@"using CMS.ContentEngine;

namespace Infrastructure.ContentTypes;

/// <summary>
/// Query service implementation for {contentTypeName}.
/// </summary>
public sealed class {shortName}QueryService(IContentQueryExecutor queryExecutor) : I{shortName}QueryService
{{
    public async Task<{dtoName}?> GetByGuidAsync(Guid guid, CancellationToken ct = default)
    {{
        var builder = new ContentItemQueryBuilder()
            .ForContentType({shortName}Constants.ContentTypeName, q => q
                .Where(w => w.WhereEquals(nameof(IContentItemFieldsSource.SystemFields.ContentItemGUID), guid))
                .TopN(1));
        
        var items = await queryExecutor.GetMappedResult<IContentItemFieldsSource>(builder, cancellationToken: ct);
        return items.FirstOrDefault() is {{ }} item ? {shortName}Mapper.ToDto(item) : null;
    }}
    
    public async Task<IEnumerable<{dtoName}>> GetAllAsync(int maxItems = 100, CancellationToken ct = default)
    {{
        var builder = new ContentItemQueryBuilder()
            .ForContentType({shortName}Constants.ContentTypeName, q => q.TopN(maxItems));
        
        var items = await queryExecutor.GetMappedResult<IContentItemFieldsSource>(builder, cancellationToken: ct);
        return {shortName}Mapper.ToDtos(items);
    }}
}}"
            }
        };

        var result = new
        {
            ContentType = contentTypeName,
            ShortName = shortName,
            DtoName = dtoName,
            TargetFolder = targetFolder,
            Files = files,
            DiRegistrations = new[]
            {
                $"services.AddScoped<I{shortName}QueryService, {shortName}QueryService>();"
            },
            Instructions = new[]
            {
                $"1. Create files in '{targetFolder}/' directory",
                "2. Update DTO properties based on actual content type fields",
                "3. Update mapper to handle all required fields",
                "4. Register query service in DI container"
            }
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Generates an API controller plan with specified endpoints.
    /// </summary>
    [McpServerTool(
        Name = nameof(GenerateApiControllerPlan),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Generate API Controller Plan"),
    Description("Generates an API controller with specified endpoints following Baseline conventions.")]
    public static string GenerateApiControllerPlan(
        IOptions<BaselineMCPConfiguration> options,
        [Description("Feature or resource name (e.g., 'Booking', 'Event')")] string featureName,
        [Description("Include GET list endpoint (default: true)")] bool includeGetList = true,
        [Description("Include GET by ID endpoint (default: true)")] bool includeGetById = true,
        [Description("Include POST create endpoint (default: false)")] bool includeCreate = false,
        [Description("Include PUT update endpoint (default: false)")] bool includeUpdate = false,
        [Description("Include DELETE endpoint (default: false)")] bool includeDelete = false,
        [Description("API route prefix (default: 'api')")] string routePrefix = "api")
    {
        if (string.IsNullOrWhiteSpace(featureName))
            throw new ArgumentException("Feature name is required.", nameof(featureName));

        featureName = char.ToUpperInvariant(featureName[0]) + featureName[1..];
        var featureNameLower = featureName.ToLowerInvariant();
        var featureNamePlural = $"{featureNameLower}s";

        var content = GenerateFullApiController(
            featureName, routePrefix, featureNamePlural,
            includeGetList, includeGetById, includeCreate, includeUpdate, includeDelete);

        var endpoints = new List<object>();
        if (includeGetList) endpoints.Add(new { Method = "GET", Route = $"/{routePrefix}/{featureNamePlural}", Description = $"Get all {featureNamePlural}" });
        if (includeGetById) endpoints.Add(new { Method = "GET", Route = $"/{routePrefix}/{featureNamePlural}/{{id}}", Description = $"Get {featureNameLower} by ID" });
        if (includeCreate) endpoints.Add(new { Method = "POST", Route = $"/{routePrefix}/{featureNamePlural}", Description = $"Create new {featureNameLower}" });
        if (includeUpdate) endpoints.Add(new { Method = "PUT", Route = $"/{routePrefix}/{featureNamePlural}/{{id}}", Description = $"Update {featureNameLower}" });
        if (includeDelete) endpoints.Add(new { Method = "DELETE", Route = $"/{routePrefix}/{featureNamePlural}/{{id}}", Description = $"Delete {featureNameLower}" });

        var result = new
        {
            FeatureName = featureName,
            RoutePrefix = routePrefix,
            Files = new[]
            {
                new
                {
                    Path = $"Features/{featureName}/Api/{featureName}ApiController.cs",
                    Type = "ApiController",
                    Content = content
                }
            },
            Endpoints = endpoints,
            Instructions = new[]
            {
                $"1. Create file in 'Features/{featureName}/Api/' directory",
                "2. Implement the TODO placeholders with actual service calls",
                "3. Add request/response DTOs as needed",
                "4. Configure authorization if required"
            }
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Generates a ViewComponent plan.
    /// </summary>
    [McpServerTool(
        Name = nameof(GenerateViewComponentPlan),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Generate ViewComponent Plan"),
    Description("Generates a ViewComponent with view and model following Baseline conventions.")]
    public static string GenerateViewComponentPlan(
        IOptions<BaselineMCPConfiguration> options,
        [Description("Component name (e.g., 'FeaturedProducts', 'RecentPosts')")] string componentName,
        [Description("Target feature folder (e.g., 'Commerce', 'Blog')")] string featureFolder,
        [Description("Include async invoke method (default: true)")] bool asyncInvoke = true)
    {
        if (string.IsNullOrWhiteSpace(componentName))
            throw new ArgumentException("Component name is required.", nameof(componentName));

        componentName = char.ToUpperInvariant(componentName[0]) + componentName[1..];
        featureFolder = string.IsNullOrWhiteSpace(featureFolder) ? componentName : featureFolder;

        var basePath = $"Features/{featureFolder}/Components/{componentName}";

        var files = new List<object>
        {
            new
            {
                Path = $"{basePath}/{componentName}ViewComponent.cs",
                Type = "ViewComponent",
                Content = GenerateViewComponentCs(componentName, asyncInvoke)
            },
            new
            {
                Path = $"{basePath}/{componentName}ViewModel.cs",
                Type = "ViewModel",
                Content = $@"namespace Features.{featureFolder}.Components.{componentName};

/// <summary>
/// View model for {componentName} component.
/// </summary>
public sealed record {componentName}ViewModel
{{
    public required string Title {{ get; init; }}
    public IReadOnlyList<object> Items {{ get; init; }} = [];
}}"
            },
            new
            {
                Path = $"{basePath}/Default.cshtml",
                Type = "RazorView",
                Content = $@"@model Features.{featureFolder}.Components.{componentName}.{componentName}ViewModel

<div class=""{componentName.ToLowerInvariant()}-component"">
    <h3>@Model.Title</h3>
    @if (Model.Items.Any())
    {{
        <ul>
            @foreach (var item in Model.Items)
            {{
                <li>@item</li>
            }}
        </ul>
    }}
    else
    {{
        <p>No items to display.</p>
    }}
</div>"
            }
        };

        var result = new
        {
            ComponentName = componentName,
            FeatureFolder = featureFolder,
            BasePath = basePath,
            Files = files,
            Usage = $"@await Component.InvokeAsync(\"{componentName}\")",
            UsageWithParams = $"@await Component.InvokeAsync(\"{componentName}\", new {{ title = \"My Title\" }})",
            Instructions = new[]
            {
                $"1. Create files in '{basePath}/' directory",
                "2. Update ViewModel with required properties",
                "3. Implement data fetching logic in ViewComponent",
                "4. Use in Razor views with Component.InvokeAsync"
            }
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    #region Private Generators

    private static string GeneratePageTemplateCs(string featureName, string? contentType)
    {
        var contentTypeAttr = string.IsNullOrWhiteSpace(contentType)
            ? $"// TODO: Add [assembly: RegisterPageTemplate(...)] for your content type"
            : $"[assembly: RegisterPageTemplate(\"{contentType}.{featureName}Template\", \"{featureName} Template\", typeof({featureName}PageTemplate))]";

        return $@"using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Microsoft.AspNetCore.Mvc;

{contentTypeAttr}

namespace Features.{featureName};

/// <summary>
/// Page template for {featureName} pages.
/// </summary>
public class {featureName}PageTemplate : PageTemplateBase
{{
    public override Task<IActionResult> RenderAsync(CancellationToken cancellationToken = default)
    {{
        return Task.FromResult<IActionResult>(new ViewResult
        {{
            ViewName = ""~/Features/{featureName}/{featureName}PageTemplate.cshtml""
        }});
    }}
}}";
    }

    private static string GeneratePageTemplateCshtml(string featureName)
    {
        return $@"@* {featureName} Page Template *@
@using Features.{featureName}

<div class=""{featureName.ToLowerInvariant()}-page"">
    <h1>{featureName}</h1>
    
    @* TODO: Add page content *@
</div>";
    }

    private static string GeneratePageTemplateCss(string featureName)
    {
        var cssClass = featureName.ToLowerInvariant();
        return $@"/* {featureName} Page Template Styles */

.{cssClass}-page {{
    padding: 2rem;
}}

.{cssClass}-page h1 {{
    margin-bottom: 1.5rem;
}}";
    }

    private static string GenerateQueryCs(string featureName, string? contentType)
    {
        return $@"using MediatR;

namespace Features.{featureName}.Operations;

/// <summary>
/// Query to retrieve {featureName} data.
/// </summary>
public sealed record Get{featureName}Query : IRequest<{featureName}ViewModel?>
{{
    public Guid? Id {{ get; init; }}
}}";
    }

    private static string GenerateQueryHandlerCs(string featureName, string? contentType)
    {
        return $@"using MediatR;

namespace Features.{featureName}.Operations;

/// <summary>
/// Handler for Get{featureName}Query.
/// </summary>
public sealed class Get{featureName}QueryHandler(I{featureName}Service service) 
    : IRequestHandler<Get{featureName}Query, {featureName}ViewModel?>
{{
    public async Task<{featureName}ViewModel?> Handle(Get{featureName}Query request, CancellationToken ct)
    {{
        // TODO: Implement query logic
        return new {featureName}ViewModel
        {{
            Title = ""{featureName}""
        }};
    }}
}}";
    }

    private static string GenerateViewModelCs(string featureName)
    {
        return $@"namespace Features.{featureName}.Operations;

/// <summary>
/// View model for {featureName}.
/// </summary>
public sealed record {featureName}ViewModel
{{
    public required string Title {{ get; init; }}
    
    // TODO: Add additional properties
}}";
    }

    private static string GenerateApiControllerCs(string featureName)
    {
        var route = featureName.ToLowerInvariant();
        return $@"using Microsoft.AspNetCore.Mvc;

namespace Features.{featureName}.Api;

/// <summary>
/// API controller for {featureName}.
/// </summary>
[ApiController]
[Route(""api/{route}s"")]
public class {featureName}ApiController(I{featureName}Service service) : ControllerBase
{{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {{
        // TODO: Implement
        return Ok(Array.Empty<object>());
    }}

    [HttpGet(""{{id:guid}}"")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {{
        // TODO: Implement
        return Ok();
    }}
}}";
    }

    private static string GenerateServiceInterfaceCs(string featureName)
    {
        return $@"namespace Features.{featureName};

/// <summary>
/// Service interface for {featureName} operations.
/// </summary>
public interface I{featureName}Service
{{
    Task<object?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<object>> GetAllAsync(CancellationToken ct = default);
}}";
    }

    private static string GenerateServiceCs(string featureName, string? contentType)
    {
        return $@"namespace Features.{featureName};

/// <summary>
/// Service implementation for {featureName} operations.
/// </summary>
public sealed class {featureName}Service : I{featureName}Service
{{
    public Task<object?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {{
        // TODO: Implement
        throw new NotImplementedException();
    }}

    public Task<IEnumerable<object>> GetAllAsync(CancellationToken ct = default)
    {{
        // TODO: Implement
        return Task.FromResult(Enumerable.Empty<object>());
    }}
}}";
    }

    private static string GenerateDiExtensionCs(string featureName, bool includeOperations)
    {
        var mediatRLine = includeOperations
            ? $"\n        // MediatR handlers auto-discovered from this assembly"
            : "";

        return $@"using Microsoft.Extensions.DependencyInjection;

namespace Features.{featureName};

/// <summary>
/// DI extensions for {featureName} feature.
/// </summary>
public static class {featureName}ServiceCollectionExtensions
{{
    /// <summary>
    /// Adds {featureName} feature services.
    /// </summary>
    public static IServiceCollection Add{featureName}Feature(this IServiceCollection services)
    {{
        services.AddScoped<I{featureName}Service, {featureName}Service>();{mediatRLine}
        
        return services;
    }}
}}";
    }

    private static string GenerateFullApiController(
        string featureName, string routePrefix, string routePlural,
        bool getList, bool getById, bool create, bool update, bool delete)
    {
        var endpoints = new List<string>();

        if (getList)
        {
            endpoints.Add($@"    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {{
        // TODO: Implement list retrieval
        return Ok(Array.Empty<object>());
    }}");
        }

        if (getById)
        {
            endpoints.Add($@"    [HttpGet(""{{id:guid}}"")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {{
        // TODO: Implement single item retrieval
        return Ok();
    }}");
        }

        if (create)
        {
            endpoints.Add($@"    [HttpPost]
    public async Task<IActionResult> Create([FromBody] object request, CancellationToken ct)
    {{
        // TODO: Implement creation
        return CreatedAtAction(nameof(GetById), new {{ id = Guid.NewGuid() }}, null);
    }}");
        }

        if (update)
        {
            endpoints.Add($@"    [HttpPut(""{{id:guid}}"")]
    public async Task<IActionResult> Update(Guid id, [FromBody] object request, CancellationToken ct)
    {{
        // TODO: Implement update
        return NoContent();
    }}");
        }

        if (delete)
        {
            endpoints.Add($@"    [HttpDelete(""{{id:guid}}"")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {{
        // TODO: Implement deletion
        return NoContent();
    }}");
        }

        return $@"using Microsoft.AspNetCore.Mvc;

namespace Features.{featureName}.Api;

/// <summary>
/// API controller for {featureName} resources.
/// </summary>
[ApiController]
[Route(""{routePrefix}/{routePlural}"")]
public class {featureName}ApiController : ControllerBase
{{
{string.Join("\n\n", endpoints)}
}}";
    }

    private static string GenerateViewComponentCs(string componentName, bool asyncInvoke)
    {
        if (asyncInvoke)
        {
            return $@"using Microsoft.AspNetCore.Mvc;

namespace Features.Components.{componentName};

/// <summary>
/// ViewComponent for {componentName}.
/// </summary>
public class {componentName}ViewComponent : ViewComponent
{{
    public async Task<IViewComponentResult> InvokeAsync(string? title = null)
    {{
        var model = new {componentName}ViewModel
        {{
            Title = title ?? ""{componentName}""
        }};

        return View(model);
    }}
}}";
        }

        return $@"using Microsoft.AspNetCore.Mvc;

namespace Features.Components.{componentName};

/// <summary>
/// ViewComponent for {componentName}.
/// </summary>
public class {componentName}ViewComponent : ViewComponent
{{
    public IViewComponentResult Invoke(string? title = null)
    {{
        var model = new {componentName}ViewModel
        {{
            Title = title ?? ""{componentName}""
        }};

        return View(model);
    }}
}}";
    }

    #endregion
}
