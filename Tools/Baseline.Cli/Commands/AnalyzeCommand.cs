using System.CommandLine;
using Spectre.Console;

namespace Baseline.Cli.Commands;

/// <summary>
/// Command for analyzing projects for best practices and issues.
/// </summary>
public static class AnalyzeCommand
{
    public static Command Create()
    {
        var verboseOption = new Option<bool>("--verbose", "Show detailed output");

        var command = new Command("analyze", "Analyze project for best practices and issues")
        {
            verboseOption
        };

        command.SetHandler(async (verbose) =>
        {
            await AnalyzeProject(verbose);
        }, verboseOption);

        return command;
    }

    private static async Task AnalyzeProject(bool verbose)
    {
        AnsiConsole.Write(new Rule("[green]Baseline Project Analysis[/]"));
        AnsiConsole.WriteLine();

        var issues = new List<(string Category, string Message, string Severity)>();

        await AnsiConsole.Status()
            .StartAsync("Analyzing project...", async ctx =>
            {
                ctx.Status("Checking for missing service registrations...");
                issues.AddRange(await CheckServiceRegistrations());

                ctx.Status("Checking Page Builder conventions...");
                issues.AddRange(await CheckPageBuilderConventions());

                ctx.Status("Checking SEO compliance...");
                issues.AddRange(await CheckSeoCompliance());

                ctx.Status("Checking caching patterns...");
                issues.AddRange(await CheckCachingPatterns());
            });

        // Display results
        var table = new Table();
        table.AddColumn("Severity");
        table.AddColumn("Category");
        table.AddColumn("Issue");

        foreach (var (category, message, severity) in issues)
        {
            var severityMarkup = severity switch
            {
                "Error" => "[red]Error[/]",
                "Warning" => "[yellow]Warning[/]",
                "Info" => "[blue]Info[/]",
                _ => severity
            };

            table.AddRow(severityMarkup, category, message);
        }

        if (issues.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]✓ No issues found![/]");
        }
        else
        {
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            var errors = issues.Count(i => i.Severity == "Error");
            var warnings = issues.Count(i => i.Severity == "Warning");

            AnsiConsole.MarkupLine($"[bold]Summary:[/] {errors} errors, {warnings} warnings");
        }
    }

    private static async Task<List<(string, string, string)>> CheckServiceRegistrations()
    {
        var issues = new List<(string, string, string)>();

        // Check for ViewComponents without registration
        var csFiles = Directory.Exists(Directory.GetCurrentDirectory())
            ? Directory.GetFiles(Directory.GetCurrentDirectory(), "*ViewComponent.cs", SearchOption.AllDirectories)
            : [];

        foreach (var file in csFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            var fileName = Path.GetFileName(file);

            // Check if it's a widget ViewComponent without proper registration
            if (content.Contains("IWidgetProperties") && !content.Contains("[assembly: RegisterWidget"))
            {
                issues.Add(("Registration", $"{fileName}: Widget ViewComponent may be missing [RegisterWidget] attribute", "Warning"));
            }
        }

        return issues;
    }

    private static async Task<List<(string, string, string)>> CheckPageBuilderConventions()
    {
        var issues = new List<(string, string, string)>();

        // Check for missing view files
        var widgetDirs = Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Components", "Widgets"))
            ? Directory.GetDirectories(Path.Combine(Directory.GetCurrentDirectory(), "Components", "Widgets"))
            : [];

        foreach (var dir in widgetDirs)
        {
            var hasView = File.Exists(Path.Combine(dir, "Default.cshtml"));
            if (!hasView)
            {
                issues.Add(("Convention", $"Widget '{Path.GetFileName(dir)}' missing Default.cshtml view", "Error"));
            }
        }

        await Task.CompletedTask;
        return issues;
    }

    private static async Task<List<(string, string, string)>> CheckSeoCompliance()
    {
        var issues = new List<(string, string, string)>();

        // Check for content types without SEO metadata
        var csFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.cs", SearchOption.AllDirectories);

        foreach (var file in csFiles)
        {
            var content = await File.ReadAllTextAsync(file);

            // Check if it's a generated content type without IBaseMetadata
            if (content.Contains("IContentItemFieldsSource") &&
                !content.Contains("IBaseMetadata") &&
                !content.Contains("MetaData_Title"))
            {
                var fileName = Path.GetFileName(file);
                issues.Add(("SEO", $"{fileName}: Content type may be missing SEO metadata (IBaseMetadata)", "Info"));
            }
        }

        return issues;
    }

    private static async Task<List<(string, string, string)>> CheckCachingPatterns()
    {
        var issues = new List<(string, string, string)>();

        var csFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.cs", SearchOption.AllDirectories);

        foreach (var file in csFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            var fileName = Path.GetFileName(file);

            // Check for content queries without caching
            if (content.Contains("IContentQueryExecutor") &&
                content.Contains("GetMappedResult") &&
                !content.Contains("GetOrCacheAsync") &&
                !content.Contains("IProgressiveCache"))
            {
                issues.Add(("Performance", $"{fileName}: Content query may benefit from caching", "Info"));
            }
        }

        return issues;
    }
}
