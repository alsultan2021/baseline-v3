namespace Baseline.Ecommerce.Installers;

/// <summary>
/// Installer for Baseline Ecommerce modules.
/// </summary>
public class BaselineEcommerceModuleInstaller
{
    /// <summary>
    /// Indicates whether the installation has been run.
    /// </summary>
    public bool InstallationRan { get; set; } = false;

    /// <summary>
    /// Installs all Baseline Ecommerce modules.
    /// </summary>
    public Task Install()
    {
        // Note: ProductFieldsSchema removed - use existing ProductFields schema instead
        // The "ProductFields" schema (ID 3cbbd637-5048-4b66-952f-7862e55977b9) already exists

        InstallationRan = true;

        return Task.CompletedTask;
    }
}
