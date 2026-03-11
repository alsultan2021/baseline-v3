using System.CommandLine;
using Spectre.Console;

namespace Baseline.Cli.Commands;

/// <summary>
/// Command group for scaffolding new components.
/// </summary>
public static class NewCommand
{
    public static Command Create()
    {
        var command = new Command("new", "Scaffold new components");

        command.AddCommand(CreateFeatureCommand());
        command.AddCommand(CreateWidgetCommand());
        command.AddCommand(CreateSectionCommand());
        command.AddCommand(CreateTemplateCommand());
        command.AddCommand(CreateContentTypeCommand());

        return command;
    }

    private static Command CreateFeatureCommand()
    {
        var nameArg = new Argument<string>("name", "Name of the feature (e.g., Blog, Products)");
        var outputOption = new Option<string?>("--output", "Output directory (default: current)");

        var command = new Command("feature", "Scaffold a new feature folder structure")
        {
            nameArg,
            outputOption
        };

        command.SetHandler(async (name, output) =>
        {
            await ScaffoldFeature(name, output ?? Directory.GetCurrentDirectory());
        }, nameArg, outputOption);

        return command;
    }

    private static Command CreateWidgetCommand()
    {
        var nameArg = new Argument<string>("name", "Name of the widget (e.g., HeroBanner)");
        var featureOption = new Option<string?>("--feature", "Feature folder to add widget to");

        var command = new Command("widget", "Generate a new Page Builder widget")
        {
            nameArg,
            featureOption
        };

        command.SetHandler(async (name, feature) =>
        {
            await ScaffoldWidget(name, feature);
        }, nameArg, featureOption);

        return command;
    }

    private static Command CreateSectionCommand()
    {
        var nameArg = new Argument<string>("name", "Name of the section (e.g., TwoColumn)");

        var command = new Command("section", "Generate a new Page Builder section")
        {
            nameArg
        };

        command.SetHandler(async (name) =>
        {
            await ScaffoldSection(name);
        }, nameArg);

        return command;
    }

    private static Command CreateTemplateCommand()
    {
        var nameArg = new Argument<string>("name", "Name of the template (e.g., BlogPost)");
        var contentTypeOption = new Option<string?>("--content-type", "Content type this template applies to");

        var command = new Command("template", "Generate a new page template")
        {
            nameArg,
            contentTypeOption
        };

        command.SetHandler(async (name, contentType) =>
        {
            await ScaffoldTemplate(name, contentType);
        }, nameArg, contentTypeOption);

        return command;
    }

    private static Command CreateContentTypeCommand()
    {
        var nameArg = new Argument<string>("name", "Name of the content type (e.g., BlogPost)");
        var namespaceOption = new Option<string?>("--namespace", "Namespace prefix (default: project name)");

        var command = new Command("content-type", "Generate content type boilerplate")
        {
            nameArg,
            namespaceOption
        };

        command.SetHandler(async (name, ns) =>
        {
            await ScaffoldContentType(name, ns);
        }, nameArg, namespaceOption);

        return command;
    }

    private static async Task ScaffoldFeature(string name, string outputDir)
    {
        AnsiConsole.MarkupLine($"[green]Creating feature:[/] {name}");

        var featureDir = Path.Combine(outputDir, "Features", name);
        Directory.CreateDirectory(featureDir);
        Directory.CreateDirectory(Path.Combine(featureDir, "Components"));
        Directory.CreateDirectory(Path.Combine(featureDir, "Models"));
        Directory.CreateDirectory(Path.Combine(featureDir, "Services"));

        // Create ViewComponent
        var viewComponentPath = Path.Combine(featureDir, $"{name}ViewComponent.cs");
        await File.WriteAllTextAsync(viewComponentPath, GenerateViewComponent(name));

        // Create ViewModel
        var viewModelPath = Path.Combine(featureDir, "Models", $"{name}ViewModel.cs");
        await File.WriteAllTextAsync(viewModelPath, GenerateViewModel(name));

        // Create View
        var viewPath = Path.Combine(featureDir, $"Default.cshtml");
        await File.WriteAllTextAsync(viewPath, GenerateView(name));

        AnsiConsole.MarkupLine($"[green]✓[/] Created feature structure at: {featureDir}");
        AnsiConsole.MarkupLine($"  [dim]├── {name}ViewComponent.cs[/]");
        AnsiConsole.MarkupLine($"  [dim]├── Default.cshtml[/]");
        AnsiConsole.MarkupLine($"  [dim]├── Models/{name}ViewModel.cs[/]");
        AnsiConsole.MarkupLine($"  [dim]├── Components/[/]");
        AnsiConsole.MarkupLine($"  [dim]└── Services/[/]");
    }

    private static async Task ScaffoldWidget(string name, string? feature)
    {
        AnsiConsole.MarkupLine($"[green]Creating widget:[/] {name}");

        var baseDir = feature != null
            ? Path.Combine(Directory.GetCurrentDirectory(), "Features", feature, "Components", "Widgets", name)
            : Path.Combine(Directory.GetCurrentDirectory(), "Components", "Widgets", name);

        Directory.CreateDirectory(baseDir);

        // Widget class
        var widgetPath = Path.Combine(baseDir, $"{name}Widget.cs");
        await File.WriteAllTextAsync(widgetPath, GenerateWidget(name));

        // Properties class
        var propsPath = Path.Combine(baseDir, $"{name}WidgetProperties.cs");
        await File.WriteAllTextAsync(propsPath, GenerateWidgetProperties(name));

        // ViewComponent
        var vcPath = Path.Combine(baseDir, $"{name}WidgetViewComponent.cs");
        await File.WriteAllTextAsync(vcPath, GenerateWidgetViewComponent(name));

        // View
        var viewPath = Path.Combine(baseDir, "Default.cshtml");
        await File.WriteAllTextAsync(viewPath, GenerateWidgetView(name));

        AnsiConsole.MarkupLine($"[green]✓[/] Created widget at: {baseDir}");
    }

    private static async Task ScaffoldSection(string name)
    {
        AnsiConsole.MarkupLine($"[green]Creating section:[/] {name}");

        var baseDir = Path.Combine(Directory.GetCurrentDirectory(), "Components", "Sections", name);
        Directory.CreateDirectory(baseDir);

        var sectionPath = Path.Combine(baseDir, $"{name}Section.cs");
        await File.WriteAllTextAsync(sectionPath, GenerateSection(name));

        var viewPath = Path.Combine(baseDir, "Default.cshtml");
        await File.WriteAllTextAsync(viewPath, GenerateSectionView(name));

        AnsiConsole.MarkupLine($"[green]✓[/] Created section at: {baseDir}");
    }

    private static async Task ScaffoldTemplate(string name, string? contentType)
    {
        AnsiConsole.MarkupLine($"[green]Creating template:[/] {name}");

        var baseDir = Path.Combine(Directory.GetCurrentDirectory(), "Features", name);
        Directory.CreateDirectory(baseDir);

        var templatePath = Path.Combine(baseDir, $"{name}PageTemplate.cs");
        await File.WriteAllTextAsync(templatePath, GeneratePageTemplate(name, contentType));

        var viewPath = Path.Combine(baseDir, $"{name}PageTemplate.cshtml");
        await File.WriteAllTextAsync(viewPath, GeneratePageTemplateView(name));

        AnsiConsole.MarkupLine($"[green]✓[/] Created template at: {baseDir}");
    }

    private static async Task ScaffoldContentType(string name, string? ns)
    {
        AnsiConsole.MarkupLine($"[green]Creating content type boilerplate:[/] {name}");

        var projectName = ns ?? Path.GetFileName(Directory.GetCurrentDirectory());
        var contentTypeName = $"{projectName}.{name}";

        AnsiConsole.MarkupLine($"[yellow]Content Type Name:[/] {contentTypeName}");
        AnsiConsole.MarkupLine($"[dim]Note: Create the content type in Xperience Admin, then run code generation.[/]");

        await Task.CompletedTask;
    }

    #region Code Generation Templates

    private static string GenerateViewComponent(string name) => $@"using Microsoft.AspNetCore.Mvc;

namespace Features.{name};

/// <summary>
/// ViewComponent for {name} feature.
/// </summary>
public class {name}ViewComponent : ViewComponent
{{
    public {name}ViewComponent()
    {{
    }}

    public async Task<IViewComponentResult> InvokeAsync()
    {{
        var model = new Models.{name}ViewModel();
        
        return View(""~/Features/{name}/Default.cshtml"", model);
    }}
}}
";

    private static string GenerateViewModel(string name) => $@"namespace Features.{name}.Models;

/// <summary>
/// ViewModel for {name} feature.
/// </summary>
public class {name}ViewModel
{{
    public string Title {{ get; set; }} = """";
}}
";

    private static string GenerateView(string name) => $@"@model Features.{name}.Models.{name}ViewModel

<div class=""{name.ToLowerInvariant()}"">
    <h2>@Model.Title</h2>
</div>
";

    private static string GenerateWidget(string name) => $@"using Kentico.PageBuilder.Web.Mvc;

[assembly: RegisterWidget(
    identifier: {name}Widget.IDENTIFIER,
    viewComponentType: typeof({name}WidgetViewComponent),
    name: ""{name}"",
    propertiesType: typeof({name}WidgetProperties),
    IconClass = ""icon-ribbon"")]

namespace Components.Widgets.{name};

public static class {name}Widget
{{
    public const string IDENTIFIER = ""Baseline.Widget.{name}"";
}}
";

    private static string GenerateWidgetProperties(string name) => $@"using Kentico.PageBuilder.Web.Mvc;

namespace Components.Widgets.{name};

public class {name}WidgetProperties : IWidgetProperties
{{
    public string Title {{ get; set; }} = """";
}}
";

    private static string GenerateWidgetViewComponent(string name) => $@"using Microsoft.AspNetCore.Mvc;
using Kentico.PageBuilder.Web.Mvc;

namespace Components.Widgets.{name};

public class {name}WidgetViewComponent : ViewComponent
{{
    public IViewComponentResult Invoke(ComponentViewModel<{name}WidgetProperties> viewModel)
    {{
        return View(""~/Components/Widgets/{name}/Default.cshtml"", viewModel);
    }}
}}
";

    private static string GenerateWidgetView(string name) => $@"@using Kentico.PageBuilder.Web.Mvc
@model ComponentViewModel<{name}WidgetProperties>

<div class=""widget-{name.ToLowerInvariant()}"">
    <h3>@Model.Properties.Title</h3>
</div>
";

    private static string GenerateSection(string name) => $@"using Kentico.PageBuilder.Web.Mvc;

[assembly: RegisterSection(
    identifier: {name}Section.IDENTIFIER,
    name: ""{name} Section"",
    customViewName: ""~/Components/Sections/{name}/Default.cshtml"",
    IconClass = ""icon-square"")]

namespace Components.Sections.{name};

public static class {name}Section
{{
    public const string IDENTIFIER = ""Baseline.Section.{name}"";
}}
";

    private static string GenerateSectionView(string name) => $@"@using Kentico.PageBuilder.Web.Mvc

<div class=""section-{name.ToLowerInvariant()}"">
    <zone />
</div>
";

    private static string GeneratePageTemplate(string name, string? contentType) => $@"using Kentico.PageBuilder.Web.Mvc.PageTemplates;

[assembly: RegisterPageTemplate(
    identifier: {name}PageTemplate.IDENTIFIER,
    name: ""{name}"",
    customViewName: ""~/Features/{name}/{name}PageTemplate.cshtml"",
    ContentTypeNames = new[] {{ {(contentType != null ? $"\"{contentType}\"" : "/* Add content type */")} }},
    IconClass = ""xp-doc-inverted"")]

namespace Features.{name};

public static class {name}PageTemplate
{{
    public const string IDENTIFIER = ""Baseline.Template.{name}"";
}}
";

    private static string GeneratePageTemplateView(string name) => $@"@using Kentico.PageBuilder.Web.Mvc
@using Kentico.PageBuilder.Web.Mvc.PageTemplates

<page-template-properties />
<editable-area area-identifier=""main"" />
";

    #endregion
}
