using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace Baseline.Core.Generators;

/// <summary>
/// Source generator that automatically generates DI registration code for services.
/// 
/// Usage:
/// 1. Mark your class with [AutoRegister] attribute
/// 2. The generator creates extension methods for IServiceCollection
/// 
/// Example:
/// [AutoRegister(Lifetime = ServiceLifetime.Scoped)]
/// public class MyService : IMyService { }
/// 
/// Generates:
/// services.AddScoped&lt;IMyService, MyService&gt;();
/// </summary>
[Generator]
public class AutoRegisterServicesGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register attribute source
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("AutoRegisterAttribute.g.cs", SourceText.From(AutoRegisterAttributeSource, Encoding.UTF8));
            ctx.AddSource("ServiceLifetimeEnum.g.cs", SourceText.From(ServiceLifetimeEnumSource, Encoding.UTF8));
        });

        // Find all classes with [AutoRegister] attribute
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsCandidateClass(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        // Combine with compilation
        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

        // Generate the source
        context.RegisterSourceOutput(compilationAndClasses, static (spc, source) => Execute(source.Left, source.Right!, spc));
    }

    private static bool IsCandidateClass(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDecl &&
               classDecl.AttributeLists.Count > 0 &&
               !classDecl.Modifiers.Any(SyntaxKind.AbstractKeyword) &&
               !classDecl.Modifiers.Any(SyntaxKind.StaticKeyword);
    }

    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;

        foreach (var attributeList in classDecl.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = attribute.Name.ToString();
                if (name is "AutoRegister" or "AutoRegisterAttribute")
                {
                    return classDecl;
                }
            }
        }

        return null;
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax?> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
            return;

        var distinctClasses = classes.Where(c => c is not null).Distinct().ToList();
        if (distinctClasses.Count == 0)
            return;

        var registrations = new List<ServiceRegistration>();

        foreach (var classDecl in distinctClasses)
        {
            if (classDecl is null) continue;

            var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;

            if (classSymbol is null) continue;

            var attribute = classSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name is "AutoRegisterAttribute" or "AutoRegister");

            if (attribute is null) continue;

            // Get lifetime from attribute (default to Scoped)
            var lifetime = "Scoped";
            foreach (var namedArg in attribute.NamedArguments)
            {
                if (namedArg.Key == "Lifetime")
                {
                    var value = namedArg.Value.Value?.ToString();
                    if (value is not null)
                    {
                        lifetime = value switch
                        {
                            "0" => "Singleton",
                            "1" => "Scoped",
                            "2" => "Transient",
                            _ => value
                        };
                    }
                }
            }

            // Find implemented interfaces (excluding system interfaces)
            var interfaces = classSymbol.AllInterfaces
                .Where(i => !i.ContainingNamespace.ToString().StartsWith("System"))
                .ToList();

            var fullClassName = classSymbol.ToDisplayString();

            if (interfaces.Count > 0)
            {
                foreach (var iface in interfaces)
                {
                    registrations.Add(new ServiceRegistration
                    {
                        InterfaceType = iface.ToDisplayString(),
                        ImplementationType = fullClassName,
                        Lifetime = lifetime
                    });
                }
            }
            else
            {
                // Self-registration
                registrations.Add(new ServiceRegistration
                {
                    InterfaceType = null,
                    ImplementationType = fullClassName,
                    Lifetime = lifetime
                });
            }
        }

        if (registrations.Count == 0) return;

        // Generate the extension method
        var source = GenerateExtensionMethod(registrations);
        context.AddSource("AutoRegisteredServices.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private static string GenerateExtensionMethod(List<ServiceRegistration> registrations)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine("namespace Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Auto-generated service registrations from [AutoRegister] attributes.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static class AutoRegisteredServicesExtensions");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Registers all services marked with [AutoRegister] attribute.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static IServiceCollection AddAutoRegisteredServices(this IServiceCollection services)");
        sb.AppendLine("    {");

        foreach (var reg in registrations)
        {
            if (reg.InterfaceType is null)
            {
                sb.AppendLine($"        services.Add{reg.Lifetime}<{reg.ImplementationType}>();");
            }
            else
            {
                sb.AppendLine($"        services.Add{reg.Lifetime}<{reg.InterfaceType}, {reg.ImplementationType}>();");
            }
        }

        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private class ServiceRegistration
    {
        public string? InterfaceType { get; set; }
        public string ImplementationType { get; set; } = "";
        public string Lifetime { get; set; } = "Scoped";
    }

    private const string AutoRegisterAttributeSource = @"// <auto-generated />
#nullable enable

namespace Baseline.Core.Generators;

/// <summary>
/// Marks a class for automatic DI registration.
/// The generator will create registration code based on implemented interfaces.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AutoRegisterAttribute : System.Attribute
{
    /// <summary>
    /// The service lifetime for registration. Defaults to Scoped.
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;
}
";

    private const string ServiceLifetimeEnumSource = @"// <auto-generated />
#nullable enable

namespace Baseline.Core.Generators;

/// <summary>
/// Service lifetime options for [AutoRegister] attribute.
/// </summary>
public enum ServiceLifetime
{
    Singleton = 0,
    Scoped = 1,
    Transient = 2
}
";
}
