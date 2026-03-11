using System.CommandLine;
using Spectre.Console;

namespace Baseline.Cli.Commands;

/// <summary>
/// Command group for code generation tasks.
/// </summary>
public static class GenerateCommand
{
    public static Command Create()
    {
        var command = new Command("generate", "Generate boilerplate code");

        command.AddAlias("gen");
        command.AddCommand(CreateServicesCommand());
        command.AddCommand(CreateViewImportsCommand());

        return command;
    }

    private static Command CreateServicesCommand()
    {
        var outputOption = new Option<string?>("--output", "Output file path");

        var command = new Command("services", "Generate DI registration code from [AutoRegister] attributes")
        {
            outputOption
        };

        command.SetHandler(async (output) =>
        {
            await GenerateServiceRegistrations(output);
        }, outputOption);

        return command;
    }

    private static Command CreateViewImportsCommand()
    {
        var command = new Command("view-imports", "Generate _ViewImports.cshtml with common usings");

        command.SetHandler(async () =>
        {
            await GenerateViewImports();
        });

        return command;
    }

    private static async Task GenerateServiceRegistrations(string? output)
    {
        AnsiConsole.MarkupLine("[green]Scanning for [AutoRegister] services...[/]");

        // Find all .cs files
        var csFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.cs", SearchOption.AllDirectories);
        var services = new List<(string Interface, string Implementation, string Lifetime)>();

        foreach (var file in csFiles)
        {
            var content = await File.ReadAllTextAsync(file);

            if (content.Contains("[AutoRegister"))
            {
                // Simple pattern matching for demo - real implementation would use Roslyn
                var className = ExtractClassName(content);
                var interfaces = ExtractInterfaces(content);
                var lifetime = content.Contains("Singleton") ? "Singleton" :
                               content.Contains("Transient") ? "Transient" : "Scoped";

                if (className != null)
                {
                    if (interfaces.Any())
                    {
                        foreach (var iface in interfaces)
                        {
                            services.Add((iface, className, lifetime));
                        }
                    }
                    else
                    {
                        services.Add((className, className, lifetime));
                    }
                }
            }
        }

        if (services.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No [AutoRegister] services found.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[green]Found {services.Count} services[/]");

        // Generate registration code
        var code = GenerateRegistrationCode(services);

        var outputPath = output ?? Path.Combine(Directory.GetCurrentDirectory(), "ServiceRegistrations.g.cs");
        await File.WriteAllTextAsync(outputPath, code);

        AnsiConsole.MarkupLine($"[green]✓[/] Generated: {outputPath}");
    }

    private static async Task GenerateViewImports()
    {
        AnsiConsole.MarkupLine("[green]Generating _ViewImports.cshtml...[/]");

        var content = @"@using Microsoft.AspNetCore.Mvc.TagHelpers
@using Kentico.Content.Web.Mvc
@using Kentico.PageBuilder.Web.Mvc
@using Kentico.PageBuilder.Web.Mvc.PageTemplates
@using Kentico.Web.Mvc
@using CMS.Websites

@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, Kentico.Content.Web.Mvc
@addTagHelper *, Kentico.PageBuilder.Web.Mvc
";

        var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "_ViewImports.cshtml");
        await File.WriteAllTextAsync(outputPath, content);

        AnsiConsole.MarkupLine($"[green]✓[/] Generated: {outputPath}");
    }

    private static string? ExtractClassName(string content)
    {
        // Simple extraction - real implementation would use Roslyn
        var classMatch = System.Text.RegularExpressions.Regex.Match(
            content, @"class\s+(\w+)");
        return classMatch.Success ? classMatch.Groups[1].Value : null;
    }

    private static List<string> ExtractInterfaces(string content)
    {
        // Simple extraction - real implementation would use Roslyn
        var interfaces = new List<string>();
        var match = System.Text.RegularExpressions.Regex.Match(
            content, @"class\s+\w+\s*:\s*([^{]+)");

        if (match.Success)
        {
            var parts = match.Groups[1].Value.Split(',');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("I") && !trimmed.Contains("("))
                {
                    interfaces.Add(trimmed);
                }
            }
        }

        return interfaces;
    }

    private static string GenerateRegistrationCode(List<(string Interface, string Implementation, string Lifetime)> services)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine("namespace Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine("public static class GeneratedServiceRegistrations");
        sb.AppendLine("{");
        sb.AppendLine("    public static IServiceCollection AddGeneratedServices(this IServiceCollection services)");
        sb.AppendLine("    {");

        foreach (var (iface, impl, lifetime) in services)
        {
            if (iface == impl)
            {
                sb.AppendLine($"        services.Add{lifetime}<{impl}>();");
            }
            else
            {
                sb.AppendLine($"        services.Add{lifetime}<{iface}, {impl}>();");
            }
        }

        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
