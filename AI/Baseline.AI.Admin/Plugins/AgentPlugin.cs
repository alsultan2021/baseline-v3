using System.ComponentModel;
using System.Text;

using Baseline.AI;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

using IBusinessAgent = Kentico.Xperience.Admin.Base.Internal.IBusinessAgent;

namespace Baseline.AI.Admin.Plugins;

/// <summary>
/// AIRA plugin for discovering registered AIRA agents.
/// Mimics MCP's <c>agent</c> capability — lists available specialized agents
/// and their capabilities so the orchestrator can route tasks effectively.
/// </summary>
[Description("Lists available AIRA agents and their capabilities for task delegation.")]
public sealed class AgentPlugin(
    IServiceProvider serviceProvider,
    ILogger<AgentPlugin> logger) : IAiraPlugin
{
    /// <inheritdoc />
    public string PluginName => "Agent";

    /// <summary>
    /// Lists all registered AIRA business agents.
    /// </summary>
    [KernelFunction("list_agents")]
    [Description("Lists all registered AIRA business agents with their names and descriptions. " +
                 "The AIRA orchestrator can automatically route tasks to these agents. " +
                 "Use this to understand what specialized agents are available.")]
    public string ListAgents()
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var agents = scope.ServiceProvider.GetServices<IBusinessAgent>().ToList();

            if (agents.Count == 0)
            {
                return "No specialized agents registered.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## Available AIRA Agents ({agents.Count})");
            sb.AppendLine();

            foreach (var agent in agents)
            {
                sb.AppendLine($"### {agent.DisplayName}");
                sb.AppendLine($"- **Name**: {agent.Name}");
                sb.AppendLine($"- **Description**: {agent.DisplayDescription}");
                sb.AppendLine();
            }

            sb.AppendLine("_The AIRA orchestrator routes tasks to agents automatically based on context._");

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Agent: Failed to list agents");
            return $"Error listing agents: {ex.Message}";
        }
    }

    /// <summary>
    /// Lists all registered AIRA plugins (tool groups).
    /// </summary>
    [KernelFunction("list_tools")]
    [Description("Lists all registered AIRA plugins (tool groups) and their functions. " +
                 "Helps understand what tools are available in this AIRA session.")]
    public string ListTools()
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var plugins = scope.ServiceProvider.GetServices<IAiraPlugin>().ToList();

            if (plugins.Count == 0)
            {
                return "No AIRA plugins registered.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## Available AIRA Plugins ({plugins.Count})");
            sb.AppendLine();

            foreach (var plugin in plugins)
            {
                var desc = plugin.GetType()
                    .GetCustomAttributes(typeof(DescriptionAttribute), false)
                    .OfType<DescriptionAttribute>()
                    .FirstOrDefault()?.Description;

                sb.AppendLine($"- **{plugin.PluginName}**: {desc ?? "(no description)"}");

                // List kernel functions
                var methods = plugin.GetType().GetMethods()
                    .Where(m => m.GetCustomAttributes(typeof(KernelFunctionAttribute), false).Length > 0);

                foreach (var method in methods)
                {
                    var funcAttr = (KernelFunctionAttribute)method
                        .GetCustomAttributes(typeof(KernelFunctionAttribute), false)[0];
                    var funcDesc = method
                        .GetCustomAttributes(typeof(DescriptionAttribute), false)
                        .OfType<DescriptionAttribute>()
                        .FirstOrDefault()?.Description;

                    sb.AppendLine($"  - `{funcAttr.Name}`: {Truncate(funcDesc, 80)}");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Agent: Failed to list tools");
            return $"Error listing tools: {ex.Message}";
        }
    }

    private static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "(no description)";
        }

        return text.Length > maxLength ? text[..maxLength] + "..." : text;
    }
}
