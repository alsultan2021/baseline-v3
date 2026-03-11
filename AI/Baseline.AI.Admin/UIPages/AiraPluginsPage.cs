using System.ComponentModel;
using System.Reflection;

using Baseline.AI;

using CMS.Membership;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages.Internal;

using Microsoft.SemanticKernel;

[assembly: UIPage(
    parentType: typeof(AiraApplication),
    slug: "aira-plugins",
    uiPageType: typeof(Baseline.AI.Admin.UIPages.AiraPluginsPage),
    name: "Plugins",
    templateName: TemplateNames.OVERVIEW,
    order: 501)]

namespace Baseline.AI.Admin.UIPages;

/// <summary>
/// Overview page displaying registered AIRA plugins with their functions and options.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class AiraPluginsPage(IAiraPluginRegistry registry) : OverviewPageBase
{
    public override Task ConfigurePage()
    {
        PageConfiguration.Caption = "AIRA Plugins";

        if (registry.Plugins.Count > 0)
        {
            foreach (var plugin in registry.Plugins)
            {
                var group = PageConfiguration.CardGroups.AddCardGroup();
                group.AddCard(BuildPluginCard(plugin));
            }
        }
        else
        {
            var emptyGroup = PageConfiguration.CardGroups.AddCardGroup();
            emptyGroup.AddCard(new OverviewCard
            {
                Headline = "No Plugins Registered",
                Components =
                [
                    new StringContentCardComponent
                    {
                        Content = "<p>Use <code>services.AddAiraPlugin&lt;T&gt;()</code> to register plugins.</p>",
                        ContentAsHtml = true
                    }
                ]
            });
        }

        return base.ConfigurePage();
    }

    private OverviewCard BuildPluginCard(IAiraPlugin plugin)
    {
        var type = plugin.GetType();
        var description = type.GetCustomAttribute<DescriptionAttribute>()?.Description;

        var targetNames = ResolveTargetProviderNames(plugin);
        var pluginOptions = registry.GetOptions(plugin.PluginName);

        var functions = type
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<KernelFunctionAttribute>() is not null)
            .Select(m =>
            {
                var attr = m.GetCustomAttribute<KernelFunctionAttribute>()!;
                var name = attr.Name ?? m.Name;
                var desc = m.GetCustomAttribute<DescriptionAttribute>()?.Description;
                return string.IsNullOrEmpty(desc) ? name : $"{name}: {desc}";
            })
            .ToList();

        var components = new List<IOverviewCardComponent>();

        if (!string.IsNullOrEmpty(description))
        {
            components.Add(new StringContentCardComponent
            {
                Content = description,
                ContentAsHtml = true
            });
        }

        if (functions.Count > 0)
        {
            components.Add(new UnorderedListCardComponent
            {
                Items = functions
                    .Select(f => new UnorderedListItem { Text = f })
                    .ToList()
            });
        }

        var extraInfo = $"<strong>Target providers:</strong> {System.Net.WebUtility.HtmlEncode(targetNames)}";

        if (!string.IsNullOrEmpty(pluginOptions.EnhancementPrompt))
        {
            extraInfo += $"<br /><strong>Enhancement:</strong> {System.Net.WebUtility.HtmlEncode(pluginOptions.EnhancementPrompt)}";
        }

        components.Add(new StringContentCardComponent
        {
            Content = $"<p>{extraInfo}</p>",
            ContentAsHtml = true
        });

        return new OverviewCard
        {
            Headline = plugin.PluginName,
            Components = components
        };
    }

    private static string ResolveTargetProviderNames(IAiraPlugin plugin)
    {
        if (plugin.TargetProviders is null || plugin.TargetProviders.Count == 0)
            return "All providers";

        return string.Join(", ", plugin.TargetProviders.Select(t => t.Name));
    }
}
