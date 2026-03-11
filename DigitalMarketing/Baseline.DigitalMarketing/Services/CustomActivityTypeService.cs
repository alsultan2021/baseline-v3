using Baseline.DigitalMarketing.Interfaces;
using CMS.Activities;
using CMS.DataEngine;
using Microsoft.Extensions.Logging;

namespace Baseline.DigitalMarketing.Services;

/// <summary>
/// Read-only implementation of <see cref="ICustomActivityTypeService"/>.
/// Queries activity types registered in the XbK admin (Contact management → Activity types).
/// Registration is admin-only; this service provides programmatic read access and validation.
/// </summary>
public class CustomActivityTypeService(
    IInfoProvider<ActivityTypeInfo> activityTypeInfoProvider,
    ILogger<CustomActivityTypeService> logger) : ICustomActivityTypeService
{
    private readonly IInfoProvider<ActivityTypeInfo> _activityTypeInfoProvider = activityTypeInfoProvider;
    private readonly ILogger<CustomActivityTypeService> _logger = logger;

    /// <inheritdoc />
    /// <remarks>
    /// Activity types in XbK are managed through the admin UI.
    /// This method logs a warning — use the administration to create activity types.
    /// </remarks>
    public Task RegisterCustomActivityTypeAsync(string codeName, string displayName, string? description = null)
    {
        _logger.LogWarning(
            "RegisterCustomActivityTypeAsync called for {CodeName} — activity types should be created in the Xperience admin UI (Contact management → Activity types)",
            codeName);

        // Activity types are admin-managed in XbK; programmatic creation is not recommended.
        // Log the intent for observability but do not create — admin must configure.
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CustomActivityTypeInfo>> GetCustomActivityTypesAsync()
    {
        try
        {
            var types = _activityTypeInfoProvider
                .Get()
                .WhereEquals(nameof(ActivityTypeInfo.ActivityTypeIsCustom), true)
                .ToList();

            return await Task.FromResult(types.Select(t => new CustomActivityTypeInfo(
                CodeName: t.ActivityTypeName,
                DisplayName: t.ActivityTypeDisplayName,
                Description: t.ActivityTypeDescription,
                IsCustom: t.ActivityTypeIsCustom
            )));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching custom activity types");
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<bool> ActivityTypeExistsAsync(string codeName)
    {
        try
        {
            var exists = _activityTypeInfoProvider
                .Get()
                .WhereEquals(nameof(ActivityTypeInfo.ActivityTypeName), codeName)
                .TopN(1)
                .Any();

            return await Task.FromResult(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking activity type {CodeName}", codeName);
            return false;
        }
    }
}
