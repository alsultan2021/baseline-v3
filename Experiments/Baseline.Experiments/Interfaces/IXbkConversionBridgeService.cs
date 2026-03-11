namespace Baseline.Experiments.Interfaces;

/// <summary>
/// Bridges experiment conversions to XbK custom activities and uses
/// the XbK contact system for user identification.
/// </summary>
public interface IXbkConversionBridgeService
{
    /// <summary>
    /// Gets the current contact GUID from XbK contact management context.
    /// Falls back to cookie-based anonymous ID if no contact exists.
    /// </summary>
    /// <returns>User identifier string (contact GUID or anonymous ID).</returns>
    string GetCurrentUserId();

    /// <summary>
    /// Logs an experiment conversion as an XbK custom activity.
    /// </summary>
    /// <param name="experimentName">Experiment name.</param>
    /// <param name="variantName">Variant name the user was in.</param>
    /// <param name="goalCodeName">Goal code name that was converted.</param>
    /// <param name="value">Optional conversion value.</param>
    Task LogConversionActivityAsync(
        string experimentName,
        string variantName,
        string goalCodeName,
        decimal? value = null);

    /// <summary>
    /// Logs an experiment variant assignment as an XbK activity.
    /// </summary>
    /// <param name="experimentName">Experiment name.</param>
    /// <param name="variantName">Assigned variant name.</param>
    Task LogAssignmentActivityAsync(string experimentName, string variantName);
}
