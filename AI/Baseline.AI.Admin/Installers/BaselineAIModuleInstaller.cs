using CMS.DataEngine;
using CMS.Modules;

using Microsoft.Extensions.Logging;

namespace Baseline.AI.Admin.Installers;

/// <summary>
/// Installer for the Baseline AI Admin module.
/// Handles creation of the custom module, database tables, and default settings.
/// Follows the AIUN community module installer pattern.
/// </summary>
public class BaselineAIModuleInstaller(
    IInfoProvider<ResourceInfo> resourceInfoProvider,
    ILogger<BaselineAIModuleInstaller> logger)
{
    /// <inheritdoc/>
    public void Install()
    {
        try
        {
            var resource = resourceInfoProvider.Get("XperienceCommunity.Baseline.AI") ?? new ResourceInfo();

            _ = InitializeResource(resource);
            BaselineAISettingsInstaller.Install(resource);
            AIKnowledgeBaseInstaller.Install(resource);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during Baseline AI module installation");
            throw;
        }
    }

    public ResourceInfo InitializeResource(ResourceInfo resource)
    {
        resource.ResourceDisplayName = "Baseline AI";
        resource.ResourceName = "XperienceCommunity.Baseline.AI";
        resource.ResourceDescription = "Provides AI-powered features including vector search, chatbot, auto-tagging, and RAG capabilities.";
        resource.ResourceIsInDevelopment = false;

        if (resource.HasChanged)
        {
            resourceInfoProvider.Set(resource);
            logger.LogDebug("Baseline AI module resource created/updated");
        }

        return resource;
    }

}
