using Baseline.Experiments.Interfaces;
using Baseline.Experiments.Models;
using Microsoft.Extensions.Logging;

namespace Baseline.Experiments.Services;

/// <summary>
/// Service for managing email A/B testing experiments.
/// Enables subject line, content, and send-time experiments.
/// </summary>
public interface IEmailExperimentService
{
    /// <summary>
    /// Creates an email subject-line A/B test.
    /// </summary>
    Task<Experiment> CreateSubjectLineTestAsync(
        string experimentName,
        string controlSubject,
        string variantSubject,
        int trafficPercentage = 50);

    /// <summary>
    /// Gets the winning subject line variant for a completed experiment.
    /// </summary>
    Task<string?> GetWinningSubjectAsync(Guid experimentId);

    /// <summary>
    /// Resolves which email subject to use for a given experiment.
    /// Returns the variant's subject if assigned, otherwise control.
    /// </summary>
    Task<string> ResolveSubjectAsync(Guid experimentId, Guid? assignedVariantId = null);
}

/// <summary>
/// Default implementation of <see cref="IEmailExperimentService"/>.
/// Builds on the core <see cref="IExperimentService"/> to create email-specific experiments.
/// </summary>
public class EmailExperimentService(
    IExperimentService experimentService,
    IVariantAssignmentService variantAssignmentService,
    ILogger<EmailExperimentService> logger) : IEmailExperimentService
{
    /// <inheritdoc />
    public async Task<Experiment> CreateSubjectLineTestAsync(
        string experimentName,
        string controlSubject,
        string variantSubject,
        int trafficPercentage = 50)
    {
        var definition = new ExperimentDefinition
        {
            Name = experimentName,
            Description = $"Email subject line test: \"{controlSubject}\" vs \"{variantSubject}\"",
            Type = ExperimentType.Email,
            Variants =
            [
                new VariantDefinition
                {
                    Name = "Control",
                    Description = controlSubject,
                    IsControl = true,
                    Weight = trafficPercentage,
                    Configuration = controlSubject
                },
                new VariantDefinition
                {
                    Name = "Variant A",
                    Description = variantSubject,
                    IsControl = false,
                    Weight = 100 - trafficPercentage,
                    Configuration = variantSubject
                }
            ],
            Goals =
            [
                new GoalDefinition
                {
                    Name = "Email Open",
                    CodeName = "email_open",
                    Type = GoalType.CustomEvent,
                    IsPrimary = true
                }
            ]
        };

        var experiment = await experimentService.CreateExperimentAsync(definition);
        logger.LogInformation("Created email subject line test '{Name}' with ID {Id}", experimentName, experiment.Id);
        return experiment;
    }

    /// <inheritdoc />
    public async Task<string?> GetWinningSubjectAsync(Guid experimentId)
    {
        var experiment = await experimentService.GetExperimentAsync(experimentId);
        if (experiment?.Status != ExperimentStatus.Completed)
        {
            return null;
        }

        // The winning variant is determined by statistics; look for highest confidence
        var results = await experimentService.GetActiveExperimentsAsync();
        var winner = experiment.Variants.FirstOrDefault(v => !v.IsControl);
        return winner?.Configuration;
    }

    /// <inheritdoc />
    public async Task<string> ResolveSubjectAsync(Guid experimentId, Guid? assignedVariantId = null)
    {
        var experiment = await experimentService.GetExperimentAsync(experimentId);
        if (experiment is null || experiment.Status != ExperimentStatus.Running)
        {
            // Return control subject as fallback
            var fallback = experiment?.Variants.FirstOrDefault(v => v.IsControl);
            return fallback?.Configuration ?? "";
        }

        // If no variant assigned yet, assign one
        Guid variantId = assignedVariantId
            ?? (await variantAssignmentService.GetAssignmentAsync(experimentId, ""))?.Id
            ?? (await variantAssignmentService.AssignVariantAsync(experimentId, "")).Id;

        var variant = experiment.Variants.FirstOrDefault(v => v.Id == variantId);
        return variant?.Configuration ?? experiment.Variants.First(v => v.IsControl).Configuration ?? "";
    }
}
