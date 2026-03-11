using CMS.DataEngine;
using CMS.Modules;

namespace Baseline.Experiments.Infrastructure;

/// <summary>
/// Top-level installer for the Baseline.Experiments module.
/// Creates the resource and invokes all table installers.
/// </summary>
public interface IExperimentsModuleInstaller
{
    void Install();
}

public class ExperimentsModuleInstaller(
    IInfoProvider<ResourceInfo> resourceInfoProvider) : IExperimentsModuleInstaller
{
    private const string ResourceName = "Baseline.Experiments";
    private const string ResourceDisplayName = "Baseline Experiments";
    private const string ResourceDescription = "A/B testing and experimentation framework";

    public void Install()
    {
        var resourceInfo = InstallModule();
        InstallModuleClasses(resourceInfo);
    }

    private ResourceInfo InstallModule()
    {
        var resourceInfo = resourceInfoProvider.Get(ResourceName) ?? new ResourceInfo();

        resourceInfo.ResourceDisplayName = ResourceDisplayName;
        resourceInfo.ResourceName = ResourceName;
        resourceInfo.ResourceDescription = ResourceDescription;
        resourceInfo.ResourceIsInDevelopment = false;

        if (resourceInfo.HasChanged)
        {
            resourceInfoProvider.Set(resourceInfo);
        }

        return resourceInfo;
    }

    private static void InstallModuleClasses(ResourceInfo resourceInfo)
    {
        ExperimentInstaller.Install(resourceInfo);
        ExperimentVariantInstaller.Install(resourceInfo);
        ExperimentGoalInstaller.Install(resourceInfo);
    }
}
