using Baseline.AI.Admin.Agents;
using Baseline.AI.Admin.Installers;
using Baseline.AI.Admin.Plugins;
using Baseline.AI.Events;

using Microsoft.Extensions.DependencyInjection;

using IBusinessAgent = Kentico.Xperience.Admin.Base.Internal.IBusinessAgent;
using KenticoAgent = Kentico.Xperience.Admin.Base.Internal.ISpecializedAgent;

namespace Baseline.AI.Admin;

/// <summary>
/// Extension methods for registering Baseline AI Admin services.
/// </summary>
public static class ServiceCollectionAdminExtensions
{
    /// <summary>
    /// Registers Baseline AI Admin services including the module installer.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBaselineAIAdmin(this IServiceCollection services)
    {
        // Register the module installer (called from BaselineAIAdminModule on Initialized)
        services.AddSingleton<BaselineAIModuleInstaller>();

        // Register event handler for auto-tagging etc.
        services.AddSingleton<AIContentEventHandler>();

        return services;
    }

    /// <summary>
    /// Registers the SEO Analyzer agent as a Kentico AIRA business agent.
    /// The agent appears in the AIRA chat and Agents settings page.
    /// Registered under both <c>IBusinessAgent</c> (for orchestrator filter) and
    /// <c>ISpecializedAgent</c> (for <c>InProductGuidanceService</c> discovery).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAiraSeoAnalyzer(this IServiceCollection services)
    {
        // HTTP client for fetching pages during SEO analysis
        services.AddHttpClient("SeoAnalyzer", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        });

        // Single instance, forwarded to both interfaces.
        services.AddSingleton<AiraSeoAnalyzerAgent>();

        // InProductGuidanceService resolves IEnumerable<ISpecializedAgent>
        services.AddSingleton<KenticoAgent>(sp =>
            sp.GetRequiredService<AiraSeoAnalyzerAgent>());

        // OrchestratorAgentInvocationFilter resolves IBusinessAgent[]
        services.AddSingleton<IBusinessAgent>(sp =>
            sp.GetRequiredService<AiraSeoAnalyzerAgent>());

        return services;
    }

    /// <summary>
    /// Registers the admin-level MCP-like AIRA tool plugins:
    /// ContentEdit, ContentCreate, Agent.
    /// Requires <c>AddAiraCoreTools()</c> or individual plugin registrations in Baseline.AI.
    /// </summary>
    public static IServiceCollection AddAiraAdminTools(this IServiceCollection services)
    {
        services.AddAiraPlugin<ContentEditPlugin>();
        services.AddAiraPlugin<ContentCreatePlugin>();
        services.AddAiraPlugin<AgentPlugin>();
        services.AddAiraPlugin<SeoGeoPlugin>();
        services.AddAiraPlugin<ReusableSchemaAiraPlugin>();

        return services;
    }
}
