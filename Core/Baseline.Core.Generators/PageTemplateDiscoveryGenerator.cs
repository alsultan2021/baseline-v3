using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace Baseline.Core.Generators;

/// <summary>
/// Source generator that automatically discovers and registers page templates.
/// 
/// Convention: Classes in Features/{Name}/{Name}PageTemplate.cs are auto-registered.
/// 
/// Generates:
/// - Template registration code
/// - Default controller if not exists
/// - ViewComponent invocation helpers
/// </summary>
[Generator]
public class PageTemplateDiscoveryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register the marker attribute
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("PageTemplateAttribute.g.cs", SourceText.From(PageTemplateAttributeSource, Encoding.UTF8));
        });

        // Find all classes ending with "PageTemplate" or with [PageTemplate] attribute
        var templateDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsPageTemplateCandidate(s),
                transform: static (ctx, _) => GetPageTemplateInfo(ctx))
            .Where(static m => m is not null);

        var compilationAndTemplates = context.CompilationProvider.Combine(templateDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndTemplates, static (spc, source) => Execute(source.Left, source.Right!, spc));
    }

    private static bool IsPageTemplateCandidate(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDecl)
            return false;

        // Convention: class name ends with "PageTemplate"
        if (classDecl.Identifier.Text.EndsWith("PageTemplate"))
            return true;

        // Or has [PageTemplate] attribute
        return classDecl.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString() is "PageTemplate" or "PageTemplateAttribute");
    }

    private static PageTemplateInfo? GetPageTemplateInfo(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;

        if (classSymbol is null) return null;

        // Extract template info from class name or attribute
        var className = classDecl.Identifier.Text;
        var templateName = className.Replace("PageTemplate", "");

        // Get file path to determine feature folder
        var filePath = classDecl.SyntaxTree.FilePath;
        var featureFolder = ExtractFeatureFolder(filePath);

        // Check for attribute with custom settings
        var attribute = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name is "PageTemplateAttribute" or "PageTemplate");

        string? identifier = null;
        string? name = null;
        string? description = null;
        string? iconClass = null;
        string? contentType = null;

        if (attribute is not null)
        {
            foreach (var namedArg in attribute.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "Identifier":
                        identifier = namedArg.Value.Value?.ToString();
                        break;
                    case "Name":
                        name = namedArg.Value.Value?.ToString();
                        break;
                    case "Description":
                        description = namedArg.Value.Value?.ToString();
                        break;
                    case "IconClass":
                        iconClass = namedArg.Value.Value?.ToString();
                        break;
                    case "ContentType":
                        contentType = namedArg.Value.Value?.ToString();
                        break;
                }
            }
        }

        return new PageTemplateInfo
        {
            ClassName = classSymbol.ToDisplayString(),
            ShortName = className,
            TemplateName = templateName,
            FeatureFolder = featureFolder,
            Namespace = classSymbol.ContainingNamespace.ToDisplayString(),
            Identifier = identifier ?? $"{featureFolder}.{templateName}".ToLowerInvariant(),
            DisplayName = name ?? templateName,
            Description = description ?? $"Template for {templateName} pages",
            IconClass = iconClass ?? "xp-doc-inverted",
            ContentTypeName = contentType
        };
    }

    private static string ExtractFeatureFolder(string filePath)
    {
        // Extract feature folder from path like: Features/BasicPage/BasicPagePageTemplate.cs
        var parts = filePath.Replace("\\", "/").Split('/');
        var featuresIndex = Array.IndexOf(parts, "Features");
        if (featuresIndex >= 0 && featuresIndex + 1 < parts.Length)
        {
            return parts[featuresIndex + 1];
        }
        return "Default";
    }

    private static void Execute(Compilation compilation, ImmutableArray<PageTemplateInfo?> templates, SourceProductionContext context)
    {
        var validTemplates = templates.Where(t => t is not null).ToList();
        if (validTemplates.Count == 0) return;

        var source = GenerateTemplateRegistrations(validTemplates!);
        context.AddSource("DiscoveredPageTemplates.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private static string GenerateTemplateRegistrations(List<PageTemplateInfo> templates)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using Kentico.PageBuilder.Web.Mvc.PageTemplates;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine("namespace Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Auto-discovered page template registrations.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static class DiscoveredPageTemplatesExtensions");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Discovered template identifiers for use in content types.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static class TemplateIdentifiers");
        sb.AppendLine("    {");

        foreach (var template in templates)
        {
            sb.AppendLine($"        public const string {template.TemplateName} = \"{template.Identifier}\";");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets all discovered template identifiers.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static string[] GetAllTemplateIdentifiers() => new[]");
        sb.AppendLine("    {");

        foreach (var template in templates)
        {
            sb.AppendLine($"        TemplateIdentifiers.{template.TemplateName},");
        }

        sb.AppendLine("    };");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private class PageTemplateInfo
    {
        public string ClassName { get; set; } = "";
        public string ShortName { get; set; } = "";
        public string TemplateName { get; set; } = "";
        public string FeatureFolder { get; set; } = "";
        public string Namespace { get; set; } = "";
        public string Identifier { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public string IconClass { get; set; } = "";
        public string? ContentTypeName { get; set; }
    }

    private const string PageTemplateAttributeSource = @"// <auto-generated />
#nullable enable

namespace Baseline.Core.Generators;

/// <summary>
/// Marks a class as a page template for auto-discovery and registration.
/// Convention: Classes named {Name}PageTemplate in Features/{Name}/ are auto-discovered.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class PageTemplateAttribute : System.Attribute
{
    /// <summary>
    /// The unique identifier for the template. Auto-generated if not specified.
    /// </summary>
    public string? Identifier { get; set; }

    /// <summary>
    /// Display name for the template in the admin UI.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Description shown in the template selector.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Icon class for the template (e.g., ""xp-doc-inverted"").
    /// </summary>
    public string? IconClass { get; set; }

    /// <summary>
    /// Content type this template applies to (e.g., ""Generic.BasicPage"").
    /// </summary>
    public string? ContentType { get; set; }
}
";
}
