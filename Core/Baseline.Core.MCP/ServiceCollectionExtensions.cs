using System.Reflection;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Protocol;

namespace Baseline.Core.MCP;

/// <summary>
/// Extension methods for registering Baseline MCP server services.
/// Note: Widget/content modeling handled by official Kentico MCP servers.
/// This provides SQL, content retrieval, and development tools.
/// </summary>
public static class ServiceCollectionExtensions
{
    private static Implementation Implementation { get; } = new()
    {
        Name = "Baseline.Core.MCP",
        Version = "1.0.0"
    };

    /// <summary>
    /// Adds Baseline MCP server with default configuration.
    /// </summary>
    public static IServiceCollection AddBaselineMCPServer(this IServiceCollection services) =>
        services.AddBaselineMCPServer(_ => { });

    /// <summary>
    /// Adds Baseline MCP server with custom configuration.
    /// </summary>
    public static IServiceCollection AddBaselineMCPServer(
        this IServiceCollection services,
        Action<BaselineMCPConfiguration> configureOptions)
    {
        var configuration = new BaselineMCPConfiguration();
        configureOptions(configuration);
        BaselineMCPConfiguration.ValidateConfiguration(configuration);
        services.AddSingleton(Options.Create(configuration));

        var builder = services
            .AddMcpServer(options => options.ServerInfo = Implementation)
            .WithHttpTransport();

        configuration.ScannedAssemblies.Add(Assembly.GetExecutingAssembly()!);

        foreach (var assembly in configuration.ScannedAssemblies)
        {
            builder
                .WithToolsFromAssembly(assembly)
                .WithPromptsFromAssembly(assembly);
        }

        return services;
    }

    /// <summary>
    /// Configures the Baseline MCP server middleware.
    /// </summary>
    public static IApplicationBuilder UseBaselineMCPServer(this WebApplication app)
    {
        var options = app.Services
            .GetRequiredService<IOptions<BaselineMCPConfiguration>>()
            .Value;

        app.MapMcp(options.BasePath);

        return app;
    }
}
