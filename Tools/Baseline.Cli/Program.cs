using System.CommandLine;
using Baseline.Cli.Commands;
using Spectre.Console;

namespace Baseline.Cli;

/// <summary>
/// Baseline CLI - Developer productivity tool for Xperience by Kentico projects.
/// 
/// Commands:
/// - baseline new feature {name}     : Scaffold a new feature folder structure
/// - baseline new widget {name}      : Generate a new Page Builder widget
/// - baseline new section {name}     : Generate a new Page Builder section
/// - baseline new template {name}    : Generate a new page template
/// - baseline new content-type {name}: Generate content type boilerplate
/// - baseline generate services      : Generate DI registration code
/// - baseline analyze                : Analyze project for best practices
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Baseline CLI - Developer productivity tool for Xperience by Kentico")
        {
            NewCommand.Create(),
            GenerateCommand.Create(),
            AnalyzeCommand.Create()
        };

        rootCommand.Description = """
            Baseline CLI v3.0.0
            
            A developer productivity tool for Xperience by Kentico projects.
            Scaffolds features, widgets, templates, and generates boilerplate code.
            
            Examples:
              baseline new feature Blog
              baseline new widget HeroBanner
              baseline generate services
              baseline analyze
            """;

        return await rootCommand.InvokeAsync(args);
    }
}
