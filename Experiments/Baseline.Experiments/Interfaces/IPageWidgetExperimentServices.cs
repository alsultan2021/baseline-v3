using Baseline.Experiments.Models;

namespace Baseline.Experiments.Interfaces;

/// <summary>
/// Service for page-level A/B testing experiments.
/// </summary>
public interface IPageExperimentService
{
    /// <summary>
    /// Gets the experiment variant for a page request.
    /// </summary>
    /// <param name="pagePath">Request page path.</param>
    /// <param name="userId">User identifier.</param>
    /// <returns>Page variant info, or null if no experiment.</returns>
    Task<PageVariantInfo?> GetPageVariantAsync(string pagePath, string userId);

    /// <summary>
    /// Gets active page experiments.
    /// </summary>
    /// <returns>Collection of page experiments.</returns>
    Task<IEnumerable<Experiment>> GetActivePageExperimentsAsync();

    /// <summary>
    /// Creates a page experiment with two variants.
    /// </summary>
    /// <param name="name">Experiment name.</param>
    /// <param name="targetPath">Original page path.</param>
    /// <param name="variantPath">Variant page path.</param>
    /// <param name="goalCodeName">Primary goal code name.</param>
    /// <returns>Created experiment.</returns>
    Task<Experiment> CreatePageExperimentAsync(
        string name,
        string targetPath,
        string variantPath,
        string goalCodeName);
}

/// <summary>
/// Information about a page variant assignment.
/// </summary>
public class PageVariantInfo
{
    /// <summary>
    /// Experiment identifier.
    /// </summary>
    public Guid ExperimentId { get; set; }

    /// <summary>
    /// Experiment name.
    /// </summary>
    public string ExperimentName { get; set; } = string.Empty;

    /// <summary>
    /// Assigned variant identifier.
    /// </summary>
    public Guid VariantId { get; set; }

    /// <summary>
    /// Assigned variant name.
    /// </summary>
    public string VariantName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the control variant.
    /// </summary>
    public bool IsControl { get; set; }

    /// <summary>
    /// Path to render for this variant.
    /// </summary>
    public string RenderPath { get; set; } = string.Empty;

    /// <summary>
    /// Original target path.
    /// </summary>
    public string OriginalPath { get; set; } = string.Empty;
}

/// <summary>
/// Service for widget-level A/B testing experiments.
/// </summary>
public interface IWidgetExperimentService
{
    /// <summary>
    /// Gets the widget variant configuration for a user.
    /// </summary>
    /// <param name="widgetIdentifier">Widget identifier.</param>
    /// <param name="userId">User identifier.</param>
    /// <returns>Widget variant info, or null if no experiment.</returns>
    Task<WidgetVariantInfo?> GetWidgetVariantAsync(string widgetIdentifier, string userId);

    /// <summary>
    /// Creates a widget experiment.
    /// </summary>
    /// <param name="name">Experiment name.</param>
    /// <param name="widgetIdentifier">Widget identifier.</param>
    /// <param name="variants">Variant configurations.</param>
    /// <param name="goalCodeName">Primary goal code name.</param>
    /// <returns>Created experiment.</returns>
    Task<Experiment> CreateWidgetExperimentAsync(
        string name,
        string widgetIdentifier,
        IEnumerable<WidgetVariantConfiguration> variants,
        string goalCodeName);
}

/// <summary>
/// Information about a widget variant assignment.
/// </summary>
public class WidgetVariantInfo
{
    /// <summary>
    /// Experiment identifier.
    /// </summary>
    public Guid ExperimentId { get; set; }

    /// <summary>
    /// Assigned variant identifier.
    /// </summary>
    public Guid VariantId { get; set; }

    /// <summary>
    /// Whether this is the control variant.
    /// </summary>
    public bool IsControl { get; set; }

    /// <summary>
    /// Widget configuration JSON.
    /// </summary>
    public string? Configuration { get; set; }
}

/// <summary>
/// Widget variant configuration for creation.
/// </summary>
public class WidgetVariantConfiguration
{
    /// <summary>
    /// Variant name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the control.
    /// </summary>
    public bool IsControl { get; set; }

    /// <summary>
    /// Traffic weight.
    /// </summary>
    public int Weight { get; set; } = 50;

    /// <summary>
    /// Widget configuration JSON.
    /// </summary>
    public string? Configuration { get; set; }
}
