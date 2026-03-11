using System.Text.Json;
using Baseline.Experiments.Configuration;
using Baseline.Experiments.Interfaces;
using Baseline.Experiments.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Experiments.Services;

/// <summary>
/// Cookie-backed variant assignment service. Persists assignments to HTTP cookies
/// so they survive app restarts and are consistent across requests.
/// Falls back to <see cref="VariantAssignmentService"/> for in-memory tracking
/// and uses <see cref="ITrafficSplitService"/> for initial variant selection.
/// </summary>
public class CookieVariantAssignmentService(
    IExperimentService experimentService,
    ITrafficSplitService trafficSplitService,
    IHttpContextAccessor httpContextAccessor,
    IOptions<BaselineExperimentsOptions> options,
    ILogger<CookieVariantAssignmentService> logger) : IVariantAssignmentService
{
    private readonly IExperimentService _experimentService = experimentService;
    private readonly ITrafficSplitService _trafficSplitService = trafficSplitService;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly BaselineExperimentsOptions _options = options.Value;
    private readonly ILogger<CookieVariantAssignmentService> _logger = logger;

    /// <inheritdoc />
    public async Task<ExperimentVariant> AssignVariantAsync(Guid experimentId, string userId)
    {
        // Check cookie for existing assignment
        var existing = await GetAssignmentAsync(experimentId, userId);
        if (existing != null)
        {
            return existing;
        }

        var experiment = await _experimentService.GetExperimentAsync(experimentId)
            ?? throw new InvalidOperationException($"Experiment {experimentId} not found");

        var variant = _trafficSplitService.SelectVariant(experiment, userId);

        // Persist to cookie
        SaveAssignmentToCookie(experimentId, variant.Id);

        _logger.LogInformation(
            "Assigned user {UserId} to variant {VariantId} for experiment {ExperimentId} (cookie-persisted)",
            userId, variant.Id, experimentId);

        return variant;
    }

    /// <inheritdoc />
    public async Task<ExperimentVariant?> GetAssignmentAsync(Guid experimentId, string userId)
    {
        var assignments = ReadAssignmentsFromCookie();
        if (!assignments.TryGetValue(experimentId.ToString(), out var variantIdStr))
        {
            return null;
        }

        if (!Guid.TryParse(variantIdStr, out var variantId))
        {
            return null;
        }

        var experiment = await _experimentService.GetExperimentAsync(experimentId);
        return experiment?.Variants.FirstOrDefault(v => v.Id == variantId);
    }

    /// <inheritdoc />
    public Task<IEnumerable<ExperimentAssignment>> GetUserAssignmentsAsync(string userId)
    {
        var assignments = ReadAssignmentsFromCookie();
        var result = assignments.Select(kvp => new ExperimentAssignment
        {
            ExperimentId = Guid.TryParse(kvp.Key, out var expId) ? expId : Guid.Empty,
            VariantId = Guid.TryParse(kvp.Value, out var varId) ? varId : Guid.Empty,
            UserId = userId,
            AssignedAtUtc = DateTime.UtcNow,
            AssignmentSource = "cookie"
        }).Where(a => a.ExperimentId != Guid.Empty && a.VariantId != Guid.Empty);

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task ClearAssignmentsAsync(string userId)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context != null)
        {
            context.Response.Cookies.Delete(_options.ExperimentCookieName);
            _logger.LogDebug("Cleared experiment cookie for user {UserId}", userId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ForceAssignmentAsync(Guid experimentId, Guid variantId, string userId)
    {
        SaveAssignmentToCookie(experimentId, variantId);

        _logger.LogInformation(
            "Force-assigned user {UserId} to variant {VariantId} for experiment {ExperimentId}",
            userId, variantId, experimentId);

        await Task.CompletedTask;
    }

    private Dictionary<string, string> ReadAssignmentsFromCookie()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return [];
        }

        var cookieValue = context.Request.Cookies[_options.ExperimentCookieName];
        if (string.IsNullOrEmpty(cookieValue))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(cookieValue) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse experiment cookie, clearing");
            context.Response.Cookies.Delete(_options.ExperimentCookieName);
            return [];
        }
    }

    private void SaveAssignmentToCookie(Guid experimentId, Guid variantId)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            _logger.LogDebug("No HttpContext available, cannot persist variant assignment to cookie");
            return;
        }

        var assignments = ReadAssignmentsFromCookie();
        assignments[experimentId.ToString()] = variantId.ToString();

        var cookieValue = JsonSerializer.Serialize(assignments);
        var cookieOptions = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(_options.CookieExpirationDays),
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = false // experiment cookie is not essential — respects consent
        };

        context.Response.Cookies.Append(_options.ExperimentCookieName, cookieValue, cookieOptions);
    }
}
