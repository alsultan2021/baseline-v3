using System.Collections.Immutable;
using System.Text.Json;

using CMS.DataEngine;
using CMS.Membership;

using Baseline.Automation.Models;

using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(Baseline.Automation.Admin.UIPages.AutomationProcessSectionPage),
    slug: "automation-statistics",
    uiPageType: typeof(Baseline.Automation.Admin.UIPages.AutomationProcessStatisticsPage),
    name: "Statistics",
    templateName: "@baseline/automation/AutomationStatistics",
    order: 200,
    Icon = Icons.Graph)]

namespace Baseline.Automation.Admin.UIPages;

/// <summary>
/// Automation statistics page — shows the automation canvas with per-node contact counts.
/// </summary>
[UINavigation(true)]
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class AutomationProcessStatisticsPage(
    IAutomationProcessService processService,
    IAutomationEngine automationEngine,
    IInfoProvider<AutomationProcessInfo> processInfoProvider)
    : Page<AutomationStatisticsClientProperties>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>Object ID from the parent section page.</summary>
    [PageParameter(typeof(IntPageModelBinder))]
    public int ObjectId { get; set; }

    private Guid GetProcessGuid()
    {
        var info = processInfoProvider.Get()
            .WhereEquals(nameof(AutomationProcessInfo.AutomationProcessID), ObjectId)
            .FirstOrDefault();
        return info?.AutomationProcessGuid ?? Guid.Empty;
    }

    public override async Task<AutomationStatisticsClientProperties> ConfigureTemplateProperties(
        AutomationStatisticsClientProperties properties)
    {
        properties.PreventRefetch = true;

        var lastRecalculated = DateTimeOffset.UtcNow;
        properties.LastStatisticsRecalculationDateTime = lastRecalculated.ToString("o");
        properties.RecalculateButtonLabel = "Refresh";
        properties.RecalculatingButtonLabel = "Refreshing…";
        properties.LastStatisticsRecalculationLabelTemplate = "Statistics from: {0}";
        properties.LastStatisticsRecalculationTooltipTemplate = "Last recalculated: {0}";

        return properties;
    }

    /// <summary>Load the process graph with statistics on each node.</summary>
    [PageCommand(Permission = SystemPermissions.VIEW)]
    public async Task<ICommandResponse<LoadAutomationProcessResult>> LoadAutomationProcess()
    {
        var processGuid = GetProcessGuid();
        var result = new LoadAutomationProcessResult();

        if (processGuid == Guid.Empty)
            return ResponseFrom(result);

        var process = await processService.GetProcessAsync(processGuid);
        if (process == null)
            return ResponseFrom(result);

        var contacts = (await automationEngine.GetActiveContactsInProcessAsync(processGuid)).ToList();
        var contactsByStep = contacts
            .GroupBy(c => c.CurrentStepId)
            .ToDictionary(g => g.Key, g => g.Count());

        // Add trigger node with statistics
        if (process.Trigger != null)
        {
            var triggerConfig = new Dictionary<string, object>
            {
                ["triggerType"] = process.Trigger.TriggerType.ToString()
            };

            if (!string.IsNullOrEmpty(process.Trigger.Configuration))
            {
                try
                {
                    var extra = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        process.Trigger.Configuration, JsonOpts);
                    if (extra != null)
                        foreach (var kvp in extra)
                            triggerConfig.TryAdd(kvp.Key, kvp.Value);
                }
                catch { /* ignore malformed config */ }
            }

            var triggerCount = contactsByStep.TryGetValue(process.Trigger.Id, out var tc) ? tc : 0;
            result.Nodes.Add(new AutomationProcessNodeDto
            {
                Id = process.Trigger.Id,
                Name = process.Trigger.Name,
                StepType = "Trigger",
                IconName = GetTriggerIcon(process.Trigger.TriggerType),
                IsSaved = true,
                Statistics = [new() { IconName = "xp-personalisation-variants", Value = triggerCount, StatisticTooltip = $"{triggerCount} contacts" }],
                Configuration = triggerConfig
            });
        }

        // Add step nodes with statistics
        // Build a set of step IDs that are condition branch targets
        var branchTargetIds = new HashSet<Guid>();
        foreach (var step in process.Steps)
        {
            if (step.TrueBranchStepId.HasValue) branchTargetIds.Add(step.TrueBranchStepId.Value);
            if (step.FalseBranchStepId.HasValue) branchTargetIds.Add(step.FalseBranchStepId.Value);
        }

        Guid? previousId = process.Trigger?.Id;
        foreach (var step in process.Steps.OrderBy(s => s.Order))
        {
            var stepCount = contactsByStep.TryGetValue(step.Id, out var sc) ? sc : 0;
            result.Nodes.Add(new AutomationProcessNodeDto
            {
                Id = step.Id,
                Name = step.Name,
                StepType = step.StepType.ToString(),
                IconName = GetStepIcon(step.StepType),
                IsSaved = true,
                Statistics = [new() { IconName = "xp-personalisation-variants", Value = stepCount, StatisticTooltip = $"{stepCount} contacts" }]
            });

            // Skip linear connection if this step is a branch target
            if (previousId.HasValue && !branchTargetIds.Contains(step.Id))
            {
                result.Connections.Add(new AutomationProcessConnectionDto
                {
                    Id = $"{previousId.Value}->{step.Id}",
                    SourceNodeGuid = previousId.Value,
                    TargetNodeGuid = step.Id
                });
            }

            // For condition steps, add true/false branch connections
            if (step.StepType == AutomationStepType.Condition)
            {
                if (step.TrueBranchStepId.HasValue)
                {
                    result.Connections.Add(new AutomationProcessConnectionDto
                    {
                        Id = $"{step.Id}-[true]->{step.TrueBranchStepId.Value}",
                        SourceNodeGuid = step.Id,
                        TargetNodeGuid = step.TrueBranchStepId.Value,
                        SourceHandle = "true"
                    });
                }
                if (step.FalseBranchStepId.HasValue)
                {
                    result.Connections.Add(new AutomationProcessConnectionDto
                    {
                        Id = $"{step.Id}-[false]->{step.FalseBranchStepId.Value}",
                        SourceNodeGuid = step.Id,
                        TargetNodeGuid = step.FalseBranchStepId.Value,
                        SourceHandle = "false"
                    });
                }
            }

            previousId = step.Id;
        }

        return ResponseFrom(result);
    }

    /// <summary>Recalculate statistics.</summary>
    [PageCommand(Permission = SystemPermissions.VIEW)]
    public async Task<ICommandResponse> RecalculateStatistics()
    {
        // Stats are calculated on-the-fly from contacts, nothing to recalculate
        return ResponseFrom(new { })
            .AddSuccessMessage("Statistics recalculated successfully.");
    }

    private static string GetTriggerIcon(AutomationTriggerType triggerType) => triggerType switch
    {
        AutomationTriggerType.FormSubmission => "xp-form",
        AutomationTriggerType.MemberRegistration => "xp-user-frame",
        AutomationTriggerType.CustomActivity => "xp-custom-element",
        AutomationTriggerType.Webhook => "xp-earth",
        AutomationTriggerType.Manual => "xp-media-player",
        AutomationTriggerType.Scheduled => "xp-clock",
        AutomationTriggerType.OrderPlaced => "xp-box",
        AutomationTriggerType.OrderStatusChanged => "xp-arrows-crooked",
        AutomationTriggerType.CartAbandoned => "xp-shopping-cart",
        AutomationTriggerType.ProductPurchased => "xp-tag",
        AutomationTriggerType.PaymentFailed => "xp-id-card",
        AutomationTriggerType.RefundIssued => "xp-dollar-sign",
        AutomationTriggerType.ProductBackInStock => "xp-box",
        AutomationTriggerType.WishlistUpdated => "xp-star-full",
        AutomationTriggerType.CouponUsed => "xp-tag",
        AutomationTriggerType.SubscriptionCreated => "xp-check-circle",
        AutomationTriggerType.SubscriptionRenewed => "xp-check-circle",
        AutomationTriggerType.SubscriptionCancelled => "xp-ban-sign",
        AutomationTriggerType.LoyaltyTierChanged => "xp-trophy",
        AutomationTriggerType.SpendingThresholdReached => "xp-money-bill",
        _ => "xp-light-bulb"
    };

    private static string GetStepIcon(AutomationStepType stepType) => stepType switch
    {
        AutomationStepType.SendEmail => "xp-message",
        AutomationStepType.Wait => "xp-clock",
        AutomationStepType.LogCustomActivity => "xp-log-activity",
        AutomationStepType.SetContactFieldValue => "xp-edit",
        AutomationStepType.Condition => "xp-separate",
        AutomationStepType.Finish => "xp-flag",
        AutomationStepType.CallWebhook => "xp-earth",
        AutomationStepType.FlagContact => "xp-flag",
        AutomationStepType.UpdateContactGroup => "xp-users",
        AutomationStepType.SendNotification => "xp-bell",
        AutomationStepType.SyncToCrm => "xp-arrows-crooked",
        AutomationStepType.SendSms => "xp-mobile",
        AutomationStepType.SendNotificationEmail => "xp-message",
        AutomationStepType.AssignToSalesRep => "xp-user",
        AutomationStepType.LogContactFormSubmission => "xp-form",
        AutomationStepType.AwardLoyaltyPoints => "xp-star-full",
        AutomationStepType.ApplyCoupon => "xp-tag",
        AutomationStepType.CreateGiftCard => "xp-id-card",
        AutomationStepType.UpdateCustomerSegment => "xp-personalisation-variants",
        AutomationStepType.UpdateOrderStatus => "xp-box",
        AutomationStepType.SendOrderNotification => "xp-message",
        AutomationStepType.AddWalletCredit => "xp-money-bill",
        _ => "xp-arrows-crooked"
    };
}
