using System.ComponentModel;
using System.Text.Json;

using Microsoft.Extensions.Options;

using ModelContextProtocol.Server;

namespace Baseline.Core.MCP.Tools;

/// <summary>
/// Project-level scaffolding tools for generating complete Kentico Xperience project structures.
/// These tools return deterministic file plans (no filesystem writes).
/// </summary>
[McpServerToolType]
public static class ProjectScaffoldingTool
{
    /// <summary>
    /// Generates a complete multi-project structure following the Baseline three-project pattern (Core + Admin + RCL).
    /// </summary>
    [McpServerTool(
        Name = nameof(GenerateProjectPlan),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Generate Project Plan"),
    Description("Generates a complete Kentico Xperience project structure plan. Supports standalone, multi-project (Core/Admin/RCL), or community package layouts. Returns file paths and content - no files are written.")]
    public static string GenerateProjectPlan(
        IOptions<BaselineMCPConfiguration> options,
        [Description("Project name in PascalCase (e.g., 'Newsletter', 'Gallery')")] string projectName,
        [Description("Root namespace (e.g., 'DancingGoat.Newsletter' or 'XperienceCommunity.Gallery')")] string rootNamespace,
        [Description("Project type: 'standalone' (single .csproj), 'multi' (Core+Admin+RCL), 'package' (NuGet community package)")] string projectType = "multi",
        [Description("Include Admin sub-project with React/TypeScript client (default: false)")] bool includeAdmin = false,
        [Description("Include RCL (Razor Class Library) sub-project for frontend components (default: true)")] bool includeRcl = true,
        [Description("Include API controller layer (default: false)")] bool includeApi = false,
        [Description("Target .NET framework (default: 'net10.0')")] string targetFramework = "net10.0",
        [Description("Xperience NuGet version (default: '31.2.0')")] string xperienceVersion = "31.2.0",
        [Description("Baseline module dependencies as comma-separated list (e.g., 'Core,Navigation,Account')")] string baselineDependencies = "Core",
        [Description("Content types this project integrates with, comma-separated code names")] string? contentTypes = null)
    {
        if (string.IsNullOrWhiteSpace(projectName))
            throw new ArgumentException("Project name is required.", nameof(projectName));
        if (string.IsNullOrWhiteSpace(rootNamespace))
            throw new ArgumentException("Root namespace is required.", nameof(rootNamespace));

        projectName = char.ToUpperInvariant(projectName[0]) + projectName[1..];
        var deps = baselineDependencies.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var ctList = string.IsNullOrWhiteSpace(contentTypes)
            ? Array.Empty<string>()
            : contentTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var projects = new List<object>();
        var files = new List<object>();
        var instructions = new List<string>();

        // Always generate Core project
        var coreNs = projectType == "standalone" ? rootNamespace : $"{rootNamespace}.Core";
        var coreProjName = projectType == "standalone" ? rootNamespace : $"{rootNamespace}.Core";

        projects.Add(new { Name = coreProjName, Type = "Core", Role = "Business logic, services, models" });
        files.AddRange(GenerateCoreProjectFiles(coreProjName, coreNs, projectName, targetFramework, xperienceVersion, deps, includeApi, ctList));
        instructions.Add($"1. Create Core project at 'src/{coreProjName}/'");

        // Admin sub-project
        if (includeAdmin && projectType != "standalone")
        {
            var adminNs = $"{rootNamespace}.Admin";
            projects.Add(new { Name = adminNs, Type = "Admin", Role = "Admin UI customizations, forms, pages" });
            files.AddRange(GenerateAdminProjectFiles(adminNs, projectName, targetFramework, xperienceVersion, coreProjName));
            instructions.Add($"2. Create Admin project at 'src/{adminNs}/'");
        }

        // RCL sub-project
        if (includeRcl && projectType != "standalone")
        {
            var rclNs = $"{rootNamespace}.RCL";
            projects.Add(new { Name = rclNs, Type = "RCL", Role = "Frontend Razor components, widgets, views" });
            files.AddRange(GenerateRCLProjectFiles(rclNs, projectName, targetFramework, xperienceVersion, coreProjName));
            instructions.Add($"3. Create RCL project at 'src/{rclNs}/'");
        }

        instructions.Add($"{instructions.Count + 1}. Add project references to the main web application .csproj");
        instructions.Add($"{instructions.Count + 1}. Add 'services.Add{projectName}(configuration);' to Program.cs or ServiceCollectionAppExtensions.cs");
        if (includeAdmin)
            instructions.Add($"{instructions.Count + 1}. Run 'npm install' in the Admin/Client directory, then 'npm run build'");
        instructions.Add($"{instructions.Count + 1}. Run 'dotnet build' to verify compilation");

        var result = new
        {
            ProjectName = projectName,
            RootNamespace = rootNamespace,
            ProjectType = projectType,
            TargetFramework = targetFramework,
            XperienceVersion = xperienceVersion,
            BaselineDependencies = deps,
            ContentTypes = ctList,
            Projects = projects,
            Files = files,
            ServiceRegistration = $"services.Add{projectName}(configuration);",
            Instructions = instructions
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Generates an Admin client project structure with React/TypeScript/Webpack.
    /// </summary>
    [McpServerTool(
        Name = nameof(GenerateAdminClientPlan),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Generate Admin Client Plan"),
    Description("Generates a React/TypeScript admin client structure with Webpack, following Kentico admin customization patterns. Returns file paths and content - no files are written.")]
    public static string GenerateAdminClientPlan(
        IOptions<BaselineMCPConfiguration> options,
        [Description("Project namespace (e.g., 'DancingGoat.Newsletter.Admin')")] string projectNamespace,
        [Description("Feature name (e.g., 'Newsletter', 'Gallery')")] string featureName,
        [Description("Include custom form components (default: true)")] bool includeFormComponents = true,
        [Description("Include custom admin pages/dashboards (default: false)")] bool includePages = false,
        [Description("Include Froala rich text editor integration (default: false)")] bool includeFroala = false,
        [Description("Xperience admin package version (default: '31.2.0')")] string xperienceAdminVersion = "31.2.0")
    {
        if (string.IsNullOrWhiteSpace(projectNamespace))
            throw new ArgumentException("Project namespace is required.", nameof(projectNamespace));

        featureName = char.ToUpperInvariant(featureName[0]) + featureName[1..];
        var featureNameLower = featureName.ToLowerInvariant();

        var files = new List<object>
        {
            // package.json
            new
            {
                Path = "Client/package.json",
                Type = "PackageJson",
                Content = GeneratePackageJson(featureNameLower, xperienceAdminVersion)
            },
            // webpack.config.js
            new
            {
                Path = "Client/webpack.config.js",
                Type = "WebpackConfig",
                Content = GenerateWebpackConfig()
            },
            // tsconfig.json
            new
            {
                Path = "Client/tsconfig.json",
                Type = "TSConfig",
                Content = GenerateTsConfig()
            },
            // .babelrc
            new
            {
                Path = "Client/.babelrc",
                Type = "BabelConfig",
                Content = GenerateBabelRc()
            },
            // Entry point
            new
            {
                Path = "Client/src/entry.tsx",
                Type = "EntryPoint",
                Content = GenerateEntryTsx(featureName, includeFormComponents, includePages)
            },
            // Types
            new
            {
                Path = "Client/src/types/index.ts",
                Type = "TypeDefinitions",
                Content = GenerateTypesTs(featureName)
            }
        };

        if (includeFormComponents)
        {
            files.Add(new
            {
                Path = $"Client/src/components/{featureName}FormComponent.tsx",
                Type = "FormComponent",
                Content = GenerateFormComponentTsx(featureName)
            });
        }

        if (includePages)
        {
            files.Add(new
            {
                Path = $"Client/src/pages/{featureName}DashboardPage.tsx",
                Type = "DashboardPage",
                Content = GenerateDashboardPageTsx(featureName)
            });
        }

        if (includeFroala)
        {
            files.Add(new
            {
                Path = "Client/src/froala/custom-plugins.ts",
                Type = "FroalaPlugin",
                Content = GenerateFroalaPlugin(featureName)
            });
        }

        var result = new
        {
            ProjectNamespace = projectNamespace,
            FeatureName = featureName,
            Files = files,
            Instructions = new[]
            {
                "1. Create Client/ directory in your Admin project",
                "2. Run 'npm install' in the Client/ directory",
                "3. Run 'npm run build' to compile TypeScript/React",
                "4. Ensure .csproj includes Client/dist/** as Content",
                "5. Set 'UseLocalAdminClient=true' in csproj PropertyGroup for development"
            }
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Generates an RCL (Razor Class Library) project structure with components, tag helpers, and views.
    /// </summary>
    [McpServerTool(
        Name = nameof(GenerateRCLProjectPlan),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Generate RCL Project Plan"),
    Description("Generates a Razor Class Library (RCL) project structure with reusable components, tag helpers, and views following Baseline conventions.")]
    public static string GenerateRCLProjectPlan(
        IOptions<BaselineMCPConfiguration> options,
        [Description("Project namespace (e.g., 'DancingGoat.Newsletter.RCL')")] string projectNamespace,
        [Description("Feature name (e.g., 'Newsletter', 'Gallery')")] string featureName,
        [Description("Core project reference path (e.g., '../DancingGoat.Newsletter.Core/DancingGoat.Newsletter.Core.csproj')")] string coreProjectRef,
        [Description("Include tag helpers (default: true)")] bool includeTagHelpers = true,
        [Description("Include sample widget (default: false)")] bool includeSampleWidget = false,
        [Description("Include sample ViewComponent (default: true)")] bool includeSampleViewComponent = true,
        [Description("Target framework (default: 'net10.0')")] string targetFramework = "net10.0",
        [Description("Xperience version (default: '31.2.0')")] string xperienceVersion = "31.2.0")
    {
        if (string.IsNullOrWhiteSpace(projectNamespace))
            throw new ArgumentException("Project namespace is required.", nameof(projectNamespace));

        featureName = char.ToUpperInvariant(featureName[0]) + featureName[1..];
        var featureNameLower = featureName.ToLowerInvariant();

        var files = new List<object>
        {
            // .csproj
            new
            {
                Path = $"{projectNamespace}.csproj",
                Type = "ProjectFile",
                Content = GenerateRCLCsproj(projectNamespace, targetFramework, xperienceVersion, coreProjectRef)
            },
            // _ViewImports.cshtml
            new
            {
                Path = "_ViewImports.cshtml",
                Type = "ViewImports",
                Content = $@"@using {projectNamespace}
@using {projectNamespace}.Components
@using {projectNamespace}.Features
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, {projectNamespace}"
            },
            // DI Extension
            new
            {
                Path = $"Extensions/{featureName}RCLServiceCollectionExtensions.cs",
                Type = "DIExtension",
                Content = $@"using Microsoft.Extensions.DependencyInjection;

namespace {projectNamespace}.Extensions;

public static class {featureName}RCLServiceCollectionExtensions
{{
    public static IServiceCollection Add{featureName}RCL(this IServiceCollection services)
    {{
        // Register RCL-specific services (view components, tag helpers, etc.)
        return services;
    }}
}}"
            }
        };

        if (includeTagHelpers)
        {
            files.Add(new
            {
                Path = $"TagHelpers/{featureName}TagHelper.cs",
                Type = "TagHelper",
                Content = GenerateTagHelperCs(projectNamespace, featureName)
            });
        }

        if (includeSampleViewComponent)
        {
            files.Add(new
            {
                Path = $"Components/{featureName}List/{featureName}ListViewComponent.cs",
                Type = "ViewComponent",
                Content = GenerateRCLViewComponentCs(projectNamespace, featureName)
            });
            files.Add(new
            {
                Path = $"Components/{featureName}List/{featureName}ListViewModel.cs",
                Type = "ViewModel",
                Content = $@"namespace {projectNamespace}.Components.{featureName}List;

public sealed record {featureName}ListViewModel
{{
    public required string Title {{ get; init; }}
    public IReadOnlyList<{featureName}ItemViewModel> Items {{ get; init; }} = [];
}}

public sealed record {featureName}ItemViewModel
{{
    public required string Name {{ get; init; }}
    public string Description {{ get; init; }} = """";
}}"
            });
            files.Add(new
            {
                Path = $"Components/{featureName}List/Default.cshtml",
                Type = "RazorView",
                Content = $@"@model {projectNamespace}.Components.{featureName}List.{featureName}ListViewModel

<div class=""{featureNameLower}-list"" xpc-preview-outline=""{featureName} List"" xpc-preview-outline-remove-element=""true"">
    <h3>@Model.Title</h3>
    @if (Model.Items.Any())
    {{
        <div class=""row"">
            @foreach (var item in Model.Items)
            {{
                <div class=""col-md-4 mb-3"">
                    <div class=""card"">
                        <div class=""card-body"">
                            <h5 class=""card-title"">@item.Name</h5>
                            <p class=""card-text"">@item.Description</p>
                        </div>
                    </div>
                </div>
            }}
        </div>
    }}
    else
    {{
        <p>No items to display.</p>
    }}
</div>"
            });
        }

        if (includeSampleWidget)
        {
            files.Add(new
            {
                Path = $"Widgets/{featureName}Widget/{featureName}WidgetViewComponent.cs",
                Type = "Widget",
                Content = GenerateWidgetCs(projectNamespace, featureName)
            });
            files.Add(new
            {
                Path = $"Widgets/{featureName}Widget/{featureName}WidgetProperties.cs",
                Type = "WidgetProperties",
                Content = $@"using Kentico.PageBuilder.Web.Mvc;

namespace {projectNamespace}.Widgets.{featureName}Widget;

public class {featureName}WidgetProperties : IWidgetProperties
{{
    public string Title {{ get; set; }} = """";
}}"
            });
            files.Add(new
            {
                Path = $"Widgets/{featureName}Widget/_{featureName}Widget.cshtml",
                Type = "WidgetView",
                Content = $@"@model {projectNamespace}.Widgets.{featureName}Widget.{featureName}WidgetViewModel

<div class=""{featureNameLower}-widget"">
    <h4>@Model.Title</h4>
    @* TODO: Add widget content *@
</div>"
            });
        }

        var result = new
        {
            ProjectNamespace = projectNamespace,
            FeatureName = featureName,
            Files = files,
            Instructions = new[]
            {
                $"1. Create project at 'src/{projectNamespace}/'",
                $"2. Add ProjectReference to main web app: <ProjectReference Include=\"../{projectNamespace}/{projectNamespace}.csproj\" />",
                $"3. Add 'services.Add{featureName}RCL();' to DI registration",
                "4. Run 'dotnet build' to verify"
            }
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Generates module registration code for integrating a project into Xperience.
    /// </summary>
    [McpServerTool(
        Name = nameof(GenerateModuleRegistrationPlan),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Generate Module Registration Plan"),
    Description("Generates Xperience module registration, DI extension methods, and Program.cs wiring code for a project.")]
    public static string GenerateModuleRegistrationPlan(
        IOptions<BaselineMCPConfiguration> options,
        [Description("Project namespace (e.g., 'DancingGoat.Newsletter')")] string projectNamespace,
        [Description("Feature name (e.g., 'Newsletter')")] string featureName,
        [Description("Include Xperience Module class with OnInit (default: true)")] bool includeModule = true,
        [Description("Include configuration options pattern (default: true)")] bool includeOptions = true,
        [Description("Include installer for custom database tables (default: false)")] bool includeInstaller = false,
        [Description("Include middleware registration (default: false)")] bool includeMiddleware = false,
        [Description("Sub-project registrations as comma-separated list (e.g., 'Core,Admin,RCL')")] string subProjects = "Core")
    {
        if (string.IsNullOrWhiteSpace(projectNamespace))
            throw new ArgumentException("Project namespace is required.", nameof(projectNamespace));

        featureName = char.ToUpperInvariant(featureName[0]) + featureName[1..];
        var subs = subProjects.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var files = new List<object>();

        // Module class
        if (includeModule)
        {
            files.Add(new
            {
                Path = $"Infrastructure/{featureName}Module.cs",
                Type = "Module",
                Content = GenerateModuleCs(projectNamespace, featureName, includeInstaller)
            });
        }

        // Options
        if (includeOptions)
        {
            files.Add(new
            {
                Path = $"Configuration/{featureName}Options.cs",
                Type = "Options",
                Content = $@"namespace {projectNamespace}.Configuration;

/// <summary>
/// Configuration options for {featureName}.
/// </summary>
public class {featureName}Options
{{
    public const string SectionName = ""{featureName}"";

    /// <summary>
    /// Whether {featureName} features are enabled.
    /// </summary>
    public bool Enabled {{ get; set; }} = true;
}}"
            });
        }

        // Installer
        if (includeInstaller)
        {
            files.Add(new
            {
                Path = $"Infrastructure/{featureName}Installer.cs",
                Type = "Installer",
                Content = GenerateInstallerCs(projectNamespace, featureName)
            });
        }

        // Middleware
        if (includeMiddleware)
        {
            files.Add(new
            {
                Path = $"Infrastructure/{featureName}Middleware.cs",
                Type = "Middleware",
                Content = GenerateMiddlewareCs(projectNamespace, featureName)
            });
        }

        // DI Extension (main orchestrator)
        files.Add(new
        {
            Path = $"Extensions/{featureName}ServiceCollectionExtensions.cs",
            Type = "DIExtension",
            Content = GenerateMainDiExtensionCs(projectNamespace, featureName, subs, includeOptions, includeMiddleware)
        });

        // Program.cs snippet
        var programSnippet = GenerateProgramSnippet(featureName, subs, includeMiddleware);

        var result = new
        {
            ProjectNamespace = projectNamespace,
            FeatureName = featureName,
            SubProjects = subs,
            Files = files,
            ProgramCsSnippet = programSnippet,
            AppSettingsSnippet = includeOptions ? $@"{{
  ""{featureName}"": {{
    ""Enabled"": true
  }}
}}" : null,
            Instructions = new[]
            {
                "1. Create registration files in your project",
                $"2. Add the following to Program.cs or ServiceCollectionAppExtensions.cs:",
                programSnippet,
                includeMiddleware ? $"3. Add 'app.Use{featureName}Middleware();' after UseRouting()" : "",
                includeOptions ? $"4. Add '{featureName}' section to appsettings.json" : "",
                "5. Run 'dotnet build' to verify"
            }.Where(s => !string.IsNullOrEmpty(s)).ToArray()
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    #region Core Project Generators

    private static List<object> GenerateCoreProjectFiles(
        string projName, string ns, string featureName, string tfm, string xpVersion,
        string[] deps, bool includeApi, string[] contentTypes)
    {
        var files = new List<object>
        {
            // .csproj
            new
            {
                Path = $"{projName}/{projName}.csproj",
                Type = "ProjectFile",
                Content = GenerateCoreCsproj(projName, ns, tfm, xpVersion, deps)
            },
            // Service interface
            new
            {
                Path = $"{projName}/Interfaces/I{featureName}Service.cs",
                Type = "Interface",
                Content = $@"namespace {ns}.Interfaces;

/// <summary>
/// Core service interface for {featureName} operations.
/// </summary>
public interface I{featureName}Service
{{
    Task<object?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<object>> GetAllAsync(CancellationToken ct = default);
}}"
            },
            // Service implementation
            new
            {
                Path = $"{projName}/Services/{featureName}Service.cs",
                Type = "Service",
                Content = $@"using {ns}.Interfaces;

namespace {ns}.Services;

/// <summary>
/// Core service implementation for {featureName}.
/// </summary>
public sealed class {featureName}Service : I{featureName}Service
{{
    public Task<object?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {{
        // TODO: implement with IContentRetriever
        throw new NotImplementedException();
    }}

    public Task<IEnumerable<object>> GetAllAsync(CancellationToken ct = default)
    {{
        // TODO: implement
        return Task.FromResult(Enumerable.Empty<object>());
    }}
}}"
            },
            // DI Extension
            new
            {
                Path = $"{projName}/Extensions/{featureName}ServiceCollectionExtensions.cs",
                Type = "DIExtension",
                Content = $@"using {ns}.Interfaces;
using {ns}.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {ns}.Extensions;

public static class {featureName}ServiceCollectionExtensions
{{
    public static IServiceCollection Add{featureName}(
        this IServiceCollection services,
        IConfiguration configuration)
    {{
        services.AddScoped<I{featureName}Service, {featureName}Service>();

        return services;
    }}
}}"
            }
        };

        // API controller in Core if standalone
        if (includeApi)
        {
            files.Add(new
            {
                Path = $"{projName}/Controllers/{featureName}ApiController.cs",
                Type = "ApiController",
                Content = $@"using {ns}.Interfaces;

using Microsoft.AspNetCore.Mvc;

namespace {ns}.Controllers;

[ApiController]
[Route(""api/{featureName.ToLowerInvariant()}s"")]
public class {featureName}ApiController(I{featureName}Service service) : ControllerBase
{{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {{
        var items = await service.GetAllAsync(ct);
        return Ok(items);
    }}

    [HttpGet(""{{id:guid}}"")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {{
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? Ok(item) : NotFound();
    }}
}}"
            });
        }

        // Content type integration stubs
        foreach (var ct in contentTypes)
        {
            var shortName = ct.Contains('.') ? ct.Split('.').Last() : ct;
            files.Add(new
            {
                Path = $"{projName}/Models/{shortName}Dto.cs",
                Type = "DTO",
                Content = $@"namespace {ns}.Models;

/// <summary>
/// DTO for {ct} content type.
/// </summary>
public sealed record {shortName}Dto
{{
    public required Guid ContentItemGuid {{ get; init; }}
    public required string Name {{ get; init; }}
    // TODO: map fields from {ct}
}}"
            });
        }

        return files;
    }

    private static string GenerateCoreCsproj(string projName, string ns, string tfm, string xpVersion, string[] deps)
    {
        var baselineRefs = string.Join("\n    ",
            deps.Select(d => $"<ProjectReference Include=\"../../src/v3/{GetBaselineProjectPath(d)}\" />"));

        return $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>{tfm}</TargetFramework>
    <RootNamespace>{ns}</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include=""Microsoft.AspNetCore.App"" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include=""Kentico.Xperience.WebApp"" Version=""{xpVersion}"" />
  </ItemGroup>

  <ItemGroup>
    {baselineRefs}
  </ItemGroup>

</Project>";
    }

    private static string GetBaselineProjectPath(string dep) => dep switch
    {
        "Core" => "Core/Baseline.Core/Baseline.Core.csproj",
        "Navigation" => "Navigation/Baseline.Navigation/Baseline.Navigation.csproj",
        "Account" => "Account/Baseline.Account/Baseline.Account.csproj",
        "Ecommerce" => "Ecommerce/Baseline.Ecommerce/Baseline.Ecommerce.csproj",
        "Search" => "Search/Baseline.Search/Baseline.Search.csproj",
        "Forms" => "Forms/Baseline.Forms/Baseline.Forms.csproj",
        "SEO" => "SEO/Baseline.SEO/Baseline.SEO.csproj",
        "AI" => "AI/Baseline.AI/Baseline.AI.csproj",
        "Localization" => "Localization/Baseline.Localization/Baseline.Localization.csproj",
        "TabbedPages" => "TabbedPages/Baseline.TabbedPages/Baseline.TabbedPages.csproj",
        "Experiments" => "Experiments/Baseline.Experiments/Baseline.Experiments.csproj",
        "EmailMarketing" => "EmailMarketing/Baseline.EmailMarketing/Baseline.EmailMarketing.csproj",
        "DataProtection" => "DataProtection/Baseline.DataProtection/Baseline.DataProtection.csproj",
        _ => $"{dep}/Baseline.{dep}/Baseline.{dep}.csproj"
    };

    #endregion

    #region Admin Project Generators

    private static List<object> GenerateAdminProjectFiles(
        string projName, string featureName, string tfm, string xpVersion, string coreProjRef)
    {
        return
        [
            new
            {
                Path = $"{projName}/{projName}.csproj",
                Type = "ProjectFile",
                Content = $@"<Project Sdk=""Microsoft.NET.Sdk.Razor"">

  <PropertyGroup>
    <TargetFramework>{tfm}</TargetFramework>
    <RootNamespace>{projName}</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <UseLocalAdminClient>true</UseLocalAdminClient>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include=""Microsoft.AspNetCore.App"" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include=""Kentico.Xperience.Admin"" Version=""{xpVersion}"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""../{coreProjRef}/{coreProjRef}.csproj"" />
  </ItemGroup>

  <ItemGroup>
    <None Include=""Client\**"" />
    <Content Include=""Client\dist\**"" CopyToOutputDirectory=""PreserveNewest"" />
  </ItemGroup>

</Project>"
            },
            new
            {
                Path = $"{projName}/Extensions/{featureName}AdminServiceCollectionExtensions.cs",
                Type = "DIExtension",
                Content = $@"using Microsoft.Extensions.DependencyInjection;

namespace {projName}.Extensions;

public static class {featureName}AdminServiceCollectionExtensions
{{
    public static IServiceCollection Add{featureName}Admin(this IServiceCollection services)
    {{
        // Register admin-specific services
        return services;
    }}
}}"
            },
            new
            {
                Path = $"{projName}/UIPages/{featureName}AdminModule.cs",
                Type = "AdminModule",
                Content = $@"using CMS;
using CMS.Base;
using CMS.Core;

[assembly: RegisterModule(typeof({projName}.UIPages.{featureName}AdminModule))]

namespace {projName}.UIPages;

public class {featureName}AdminModule : Module
{{
    public {featureName}AdminModule() : base(""{projName}"") {{ }}

    protected override void OnInit(ModuleInitParameters parameters)
    {{
        base.OnInit(parameters);
    }}
}}"
            }
        ];
    }

    #endregion

    #region RCL Project Generators

    private static List<object> GenerateRCLProjectFiles(
        string projName, string featureName, string tfm, string xpVersion, string coreProjRef)
    {
        return
        [
            new
            {
                Path = $"{projName}/{projName}.csproj",
                Type = "ProjectFile",
                Content = GenerateRCLCsproj(projName, tfm, xpVersion, $"../{coreProjRef}/{coreProjRef}.csproj")
            },
            new
            {
                Path = $"{projName}/_ViewImports.cshtml",
                Type = "ViewImports",
                Content = $@"@using {projName}
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, {projName}"
            },
            new
            {
                Path = $"{projName}/Extensions/{featureName}RCLServiceCollectionExtensions.cs",
                Type = "DIExtension",
                Content = $@"using Microsoft.Extensions.DependencyInjection;

namespace {projName}.Extensions;

public static class {featureName}RCLServiceCollectionExtensions
{{
    public static IServiceCollection Add{featureName}RCL(this IServiceCollection services)
    {{
        return services;
    }}
}}"
            },
            new
            {
                Path = $"{projName}/Components/.gitkeep",
                Type = "Placeholder",
                Content = ""
            },
            new
            {
                Path = $"{projName}/Features/.gitkeep",
                Type = "Placeholder",
                Content = ""
            }
        ];
    }

    private static string GenerateRCLCsproj(string projName, string tfm, string xpVersion, string coreRef)
    {
        return $@"<Project Sdk=""Microsoft.NET.Sdk.Razor"">

  <PropertyGroup>
    <TargetFramework>{tfm}</TargetFramework>
    <RootNamespace>{projName}</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include=""Microsoft.AspNetCore.App"" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include=""Kentico.Xperience.WebApp"" Version=""{xpVersion}"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""{coreRef}"" />
  </ItemGroup>

</Project>";
    }

    #endregion

    #region Admin Client Generators

    private static string GeneratePackageJson(string featureNameLower, string xpVersion)
    {
        return $@"{{
  ""name"": ""@xperience/{featureNameLower}-admin"",
  ""version"": ""1.0.0"",
  ""description"": ""{featureNameLower} admin UI"",
  ""scripts"": {{
    ""build"": ""webpack --mode=production"",
    ""build:dev"": ""webpack --mode=development"",
    ""start"": ""webpack --mode=development --watch""
  }},
  ""dependencies"": {{
    ""@babel/runtime"": ""^7.28.0"",
    ""@kentico/xperience-admin-base"": ""^{xpVersion}"",
    ""@kentico/xperience-admin-components"": ""^{xpVersion}"",
    ""react"": ""^18.3.1"",
    ""react-dom"": ""^18.3.1""
  }},
  ""devDependencies"": {{
    ""@babel/core"": ""^7.28.0"",
    ""@babel/plugin-transform-runtime"": ""^7.28.0"",
    ""@babel/preset-env"": ""^7.28.0"",
    ""@babel/preset-react"": ""^7.27.1"",
    ""@babel/preset-typescript"": ""^7.27.1"",
    ""@kentico/xperience-webpack-config"": ""^{xpVersion}"",
    ""@types/react"": ""^18.3.23"",
    ""@types/react-dom"": ""^18.3.7"",
    ""babel-loader"": ""^10.0.0"",
    ""webpack"": ""5.100.2"",
    ""webpack-cli"": ""^6.0.1""
  }},
  ""overrides"": {{
    ""react"": ""$react"",
    ""react-dom"": ""$react-dom"",
    ""json-joy"": "">= 11.19.0""
  }},
  ""browserslist"": [
    ""> .2% and last 3 versions"",
    ""not op_mini all"",
    ""not IE 11""
  ]
}}";
    }

    private static string GenerateWebpackConfig()
    {
        return @"const { webpackConfig } = require(""@kentico/xperience-webpack-config"");

module.exports = webpackConfig({
  output: {
    path: __dirname + ""/dist"",
  },
});";
    }

    private static string GenerateTsConfig()
    {
        return @"{
  ""compilerOptions"": {
    ""target"": ""ES2015"",
    ""module"": ""ES2015"",
    ""jsx"": ""react"",
    ""strict"": true,
    ""esModuleInterop"": true,
    ""skipLibCheck"": true,
    ""forceConsistentCasingInFileNames"": true,
    ""moduleResolution"": ""node"",
    ""resolveJsonModule"": true,
    ""isolatedModules"": true,
    ""noEmit"": true
  },
  ""include"": [""src/**/*""],
  ""exclude"": [""node_modules"", ""dist""]
}";
    }

    private static string GenerateBabelRc()
    {
        return @"{
  ""presets"": [
    ""@babel/preset-env"",
    ""@babel/preset-react"",
    ""@babel/preset-typescript""
  ],
  ""plugins"": [""@babel/plugin-transform-runtime""]
}";
    }

    private static string GenerateEntryTsx(string featureName, bool includeFormComponents, bool includePages)
    {
        var imports = new List<string>();
        var registrations = new List<string>();

        if (includeFormComponents)
        {
            imports.Add($"import {{ {featureName}FormComponent }} from './components/{featureName}FormComponent';");
            registrations.Add($"  // Register form components here");
        }

        if (includePages)
        {
            imports.Add($"import {{ {featureName}DashboardPage }} from './pages/{featureName}DashboardPage';");
            registrations.Add($"  // Register pages here");
        }

        return $@"// Admin client entry point for {featureName}
{string.Join("\n", imports)}

// Component registrations
{string.Join("\n", registrations)}

export {{ }};";
    }

    private static string GenerateTypesTs(string featureName)
    {
        return $@"// Type definitions for {featureName} admin components

export interface {featureName}Config {{
  enabled: boolean;
}}

export interface {featureName}Item {{
  id: string;
  name: string;
  // TODO: add fields
}}";
    }

    private static string GenerateFormComponentTsx(string featureName)
    {
        return $@"import React from 'react';

interface {featureName}FormComponentProps {{
  value: string;
  onChange: (value: string) => void;
}}

export const {featureName}FormComponent: React.FC<{featureName}FormComponentProps> = ({{ value, onChange }}) => {{
  return (
    <div className=""{featureName.ToLowerInvariant()}-form-component"">
      <input
        type=""text""
        value={{value}}
        onChange={{(e) => onChange(e.target.value)}}
      />
    </div>
  );
}};";
    }

    private static string GenerateDashboardPageTsx(string featureName)
    {
        return $@"import React from 'react';

export const {featureName}DashboardPage: React.FC = () => {{
  return (
    <div className=""{featureName.ToLowerInvariant()}-dashboard"">
      <h1>{featureName} Dashboard</h1>
      <p>Custom admin dashboard for {featureName}.</p>
    </div>
  );
}};";
    }

    private static string GenerateFroalaPlugin(string featureName)
    {
        return $@"// Custom Froala rich text editor plugin for {featureName}

export function register{featureName}Plugin(): void {{
  // TODO: register custom toolbar buttons or plugins
  console.log('{featureName} Froala plugin registered');
}}";
    }

    #endregion

    #region Module Registration Generators

    private static string GenerateModuleCs(string ns, string featureName, bool includeInstaller)
    {
        var installerCall = includeInstaller
            ? $@"
        var installer = new {featureName}Installer();
        installer.Install();"
            : "";

        return $@"using CMS;
using CMS.Base;
using CMS.Core;

[assembly: RegisterModule(typeof({ns}.Infrastructure.{featureName}Module))]

namespace {ns}.Infrastructure;

public class {featureName}Module : Module
{{
    public {featureName}Module() : base(""{ns}"") {{ }}

    protected override void OnInit(ModuleInitParameters parameters)
    {{
        base.OnInit(parameters);{installerCall}
    }}
}}";
    }

    private static string GenerateInstallerCs(string ns, string featureName)
    {
        return $@"using CMS.DataEngine;

namespace {ns}.Infrastructure;

/// <summary>
/// Handles custom database table creation/migration for {featureName}.
/// </summary>
internal class {featureName}Installer
{{
    public void Install()
    {{
        // TODO: create/check custom tables if needed
    }}
}}";
    }

    private static string GenerateMiddlewareCs(string ns, string featureName)
    {
        return $@"using Microsoft.AspNetCore.Http;

namespace {ns}.Infrastructure;

/// <summary>
/// Middleware for {featureName} request processing.
/// </summary>
public class {featureName}Middleware(RequestDelegate next)
{{
    public async Task InvokeAsync(HttpContext context)
    {{
        // TODO: add middleware logic
        await next(context);
    }}
}}";
    }

    private static string GenerateMainDiExtensionCs(
        string ns, string featureName, string[] subs, bool includeOptions, bool includeMiddleware)
    {
        var registrations = new List<string>();

        if (includeOptions)
        {
            registrations.Add($@"        services.Configure<Configuration.{featureName}Options>(
            configuration.GetSection(Configuration.{featureName}Options.SectionName));");
        }

        foreach (var sub in subs)
        {
            registrations.Add(sub switch
            {
                "Core" => $"        services.Add{featureName}(configuration);",
                "Admin" => $"        services.Add{featureName}Admin();",
                "RCL" => $"        services.Add{featureName}RCL();",
                _ => $"        // services.Add{featureName}{sub}();"
            });
        }

        var middlewareMethod = includeMiddleware
            ? $@"

    public static IApplicationBuilder Use{featureName}Middleware(this IApplicationBuilder app)
    {{
        return app.UseMiddleware<Infrastructure.{featureName}Middleware>();
    }}"
            : "";

        return $@"using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {ns}.Extensions;

public static class {featureName}ServiceCollectionExtensions
{{
    public static IServiceCollection Add{featureName}(
        this IServiceCollection services,
        IConfiguration configuration)
    {{
{string.Join("\n", registrations)}

        return services;
    }}{middlewareMethod}
}}";
    }

    private static string GenerateProgramSnippet(string featureName, string[] subs, bool includeMiddleware)
    {
        var lines = new List<string>
        {
            $"// {featureName} registration",
            $"builder.Services.Add{featureName}(builder.Configuration);"
        };

        if (includeMiddleware)
        {
            lines.Add("");
            lines.Add($"// After app.UseRouting()");
            lines.Add($"app.Use{featureName}Middleware();");
        }

        return string.Join("\n", lines);
    }

    #endregion

    #region RCL Component Generators

    private static string GenerateTagHelperCs(string ns, string featureName)
    {
        return $@"using Microsoft.AspNetCore.Razor.TagHelpers;

namespace {ns}.TagHelpers;

/// <summary>
/// Tag helper for rendering {featureName} elements.
/// Usage: <{featureName.ToLowerInvariant()} title=""My Title""></{featureName.ToLowerInvariant()}>
/// </summary>
[HtmlTargetElement(""{featureName.ToLowerInvariant()}"")]
public class {featureName}TagHelper : TagHelper
{{
    public string Title {{ get; set; }} = """";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {{
        output.TagName = ""div"";
        output.Attributes.SetAttribute(""class"", ""{featureName.ToLowerInvariant()}-component"");

        if (!string.IsNullOrEmpty(Title))
        {{
            output.PreContent.SetHtmlContent($""<h3>{{Title}}</h3>"");
        }}
    }}
}}";
    }

    private static string GenerateRCLViewComponentCs(string ns, string featureName)
    {
        return $@"using Microsoft.AspNetCore.Mvc;

namespace {ns}.Components.{featureName}List;

public class {featureName}ListViewComponent : ViewComponent
{{
    public async Task<IViewComponentResult> InvokeAsync(string? title = null)
    {{
        var model = new {featureName}ListViewModel
        {{
            Title = title ?? ""{featureName} List""
        }};

        return View(model);
    }}
}}";
    }

    private static string GenerateWidgetCs(string ns, string featureName)
    {
        return $@"using Kentico.PageBuilder.Web.Mvc;

using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: ""{ns}.{featureName}Widget"",
    viewComponentType: typeof({ns}.Widgets.{featureName}Widget.{featureName}WidgetViewComponent),
    name: ""{featureName}"",
    propertiesType: typeof({ns}.Widgets.{featureName}Widget.{featureName}WidgetProperties),
    Description = ""{featureName} widget"",
    IconClass = ""icon-rectangle-paragraph"")]

namespace {ns}.Widgets.{featureName}Widget;

public class {featureName}WidgetViewComponent : ViewComponent
{{
    public IViewComponentResult Invoke({featureName}WidgetProperties properties)
    {{
        var model = new {featureName}WidgetViewModel
        {{
            Title = properties.Title
        }};

        return View(""~/{featureName}Widget/_{featureName}Widget.cshtml"", model);
    }}
}}

public class {featureName}WidgetViewModel
{{
    public string Title {{ get; set; }} = """";
}}";
    }

    #endregion
}
