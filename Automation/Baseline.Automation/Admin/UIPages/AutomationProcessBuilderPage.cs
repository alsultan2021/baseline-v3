using System.Text.Json;
using System.Xml.Linq;

using CMS.Activities;
using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using CMS.EmailLibrary;
using CMS.MacroEngine;
using CMS.Membership;
using CMS.OnlineForms;

using Baseline.Automation.Models;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(Baseline.Automation.Admin.UIPages.AutomationProcessSectionPage),
    slug: "automation-builder",
    uiPageType: typeof(Baseline.Automation.Admin.UIPages.AutomationProcessBuilderPage),
    name: "Automation Builder",
    templateName: "@baseline/automation/AutomationBuilder",
    order: 100,
    Icon = "xp-l-header-cols-3-footer")]

namespace Baseline.Automation.Admin.UIPages;

/// <summary>
/// Visual automation process builder page — loads process graph into the React canvas.
/// Clones Kentico's native AutomationProcessBuilderPage.
/// </summary>
[UINavigation(true)]
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class AutomationProcessBuilderPage(
    IAutomationProcessService processService,
    IInfoProvider<AutomationProcessInfo> processInfoProvider,
    IInfoProvider<BizFormInfo> formInfoProvider,
    IInfoProvider<ActivityTypeInfo> activityTypeInfoProvider,
    IInfoProvider<MacroRuleInfo> macroRuleInfoProvider,
    IInfoProvider<MacroRuleCategoryInfo> macroRuleCategoryInfoProvider,
    IInfoProvider<EmailConfigurationInfo> emailConfigurationInfoProvider,
    IInfoProvider<EmailChannelInfo> emailChannelInfoProvider,
    IInfoProvider<ChannelInfo> channelInfoProvider,
    IInfoProvider<ContentItemLanguageMetadataInfo> contentItemLangMetaInfoProvider)
    : Page<AutomationBuilderClientProperties>
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

    public override Task<PageValidationResult> ValidatePage()
    {
        var info = processInfoProvider.Get()
            .WhereEquals(nameof(AutomationProcessInfo.AutomationProcessID), ObjectId)
            .FirstOrDefault();

        return Task.FromResult(new PageValidationResult
        {
            IsValid = info is not null,
            ErrorMessageKey = "base.forms.error.objectnotinitialized"
        });
    }

    public override async Task<AutomationBuilderClientProperties> ConfigureTemplateProperties(
        AutomationBuilderClientProperties properties)
    {
        properties.PreventRefetch = true;

        var processGuid = GetProcessGuid();
        if (processGuid == Guid.Empty) return properties;

        var process = await processService.GetProcessAsync(processGuid);
        if (process == null) return properties;

        properties.IsAutomationProcessEnabled = process.IsEnabled;
        properties.IsEditingAllowed = true; // Permission already checked via attribute
        properties.HasHistoryData = false; // Could check step history

        properties.SaveButton.Label = "Save";

        return properties;
    }

    /// <summary>Load the full process graph (nodes + connections) for the React canvas.</summary>
    [PageCommand]
    public async Task<ICommandResponse<LoadAutomationProcessResult>> LoadAutomationProcess()
    {
        var processGuid = GetProcessGuid();
        var result = new LoadAutomationProcessResult();

        if (processGuid == Guid.Empty)
            return ResponseFrom(result);

        var process = await processService.GetProcessAsync(processGuid);
        if (process == null)
            return ResponseFrom(result);

        // Add trigger node
        if (process.Trigger != null)
        {
            var triggerConfig = new Dictionary<string, object>
            {
                ["triggerType"] = process.Trigger.TriggerType.ToString()
            };

            // Merge any stored trigger configuration
            if (!string.IsNullOrEmpty(process.Trigger.Configuration))
            {
                try
                {
                    var extra = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        process.Trigger.Configuration, JsonOpts);
                    if (extra != null)
                    {
                        foreach (var kvp in extra)
                        {
                            triggerConfig.TryAdd(kvp.Key, kvp.Value);
                        }
                    }
                }
                catch { /* ignore malformed config */ }
            }

            result.Nodes.Add(new AutomationProcessNodeDto
            {
                Id = process.Trigger.Id,
                Name = process.Trigger.Name,
                StepType = "Trigger",
                IconName = GetTriggerIcon(process.Trigger.TriggerType),
                IsSaved = true,
                Statistics = [],
                Configuration = triggerConfig
            });
        }

        // Add step nodes
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
            // Deserialize step configuration if present
            Dictionary<string, object>? stepConfig = null;
            if (!string.IsNullOrEmpty(step.Configuration))
            {
                try
                {
                    stepConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        step.Configuration, JsonOpts);
                }
                catch { /* ignore malformed config */ }
            }

            result.Nodes.Add(new AutomationProcessNodeDto
            {
                Id = step.Id,
                Name = step.Name,
                StepType = step.StepType.ToString(),
                IconName = GetStepIcon(step.StepType),
                IsSaved = true,
                Statistics = [],
                Configuration = stepConfig
            });

            // Skip linear connection if this step is a branch target (it gets connected via sourceHandle)
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

    /// <summary>Enable the automation process.</summary>
    [PageCommand(Permission = SystemPermissions.UPDATE)]
    public async Task<ICommandResponse<SetIsAutomationProcessEnabledResult>> EnableAutomationProcess()
    {
        var processGuid = GetProcessGuid();
        if (processGuid != Guid.Empty)
        {
            var info = processInfoProvider.Get()
                .WhereEquals(nameof(AutomationProcessInfo.AutomationProcessGuid), processGuid)
                .FirstOrDefault();
            if (info != null)
            {
                info.AutomationProcessIsEnabled = true;
                processInfoProvider.Set(info);
            }
        }

        return ResponseFrom(new SetIsAutomationProcessEnabledResult { IsEnabled = true });
    }

    /// <summary>Disable the automation process.</summary>
    [PageCommand(Permission = SystemPermissions.UPDATE)]
    public async Task<ICommandResponse<SetIsAutomationProcessEnabledResult>> DisableAutomationProcess()
    {
        var processGuid = GetProcessGuid();
        if (processGuid != Guid.Empty)
        {
            var info = processInfoProvider.Get()
                .WhereEquals(nameof(AutomationProcessInfo.AutomationProcessGuid), processGuid)
                .FirstOrDefault();
            if (info != null)
            {
                info.AutomationProcessIsEnabled = false;
                processInfoProvider.Set(info);
            }
        }

        return ResponseFrom(new SetIsAutomationProcessEnabledResult { IsEnabled = false });
    }

    /// <summary>Save the process graph from the React builder.</summary>
    [PageCommand(Permission = SystemPermissions.UPDATE)]
    public async Task<ICommandResponse<AutomationBuilderSaveResult>> Save(
        AutomationBuilderSaveCommandArguments args)
    {
        var processGuid = GetProcessGuid();
        if (processGuid == Guid.Empty)
            return ResponseFrom(new AutomationBuilderSaveResult { Status = "error" })
                .AddErrorMessage("Process not found.");

        var info = processInfoProvider.Get()
            .WhereEquals(nameof(AutomationProcessInfo.AutomationProcessGuid), processGuid)
            .FirstOrDefault();

        if (info == null)
            return ResponseFrom(new AutomationBuilderSaveResult { Status = "error" })
                .AddErrorMessage("Process not found.");

        // Serialize steps from builder nodes (skip trigger nodes)
        var triggerNode = args.Nodes.FirstOrDefault(n =>
            string.Equals(n.StepType, "Trigger", StringComparison.OrdinalIgnoreCase));

        // Build branch target lookup from connections with SourceHandle
        var trueBranches = new Dictionary<Guid, Guid>();
        var falseBranches = new Dictionary<Guid, Guid>();
        foreach (var conn in args.Connections)
        {
            if (string.Equals(conn.SourceHandle, "true", StringComparison.OrdinalIgnoreCase))
                trueBranches[conn.SourceNodeGuid] = conn.TargetNodeGuid;
            else if (string.Equals(conn.SourceHandle, "false", StringComparison.OrdinalIgnoreCase))
                falseBranches[conn.SourceNodeGuid] = conn.TargetNodeGuid;
        }

        var steps = args.Nodes
            .Where(n => !string.Equals(n.StepType, "Trigger", StringComparison.OrdinalIgnoreCase))
            .Select((n, i) => new AutomationStep
            {
                Id = n.Id,
                Name = n.Name,
                StepType = Enum.TryParse<AutomationStepType>(n.StepType, out var st) ? st : AutomationStepType.SendEmail,
                Order = i,
                Configuration = n.Configuration != null
                    ? JsonSerializer.Serialize(n.Configuration, JsonOpts)
                    : null,
                TrueBranchStepId = trueBranches.TryGetValue(n.Id, out var trueId) ? trueId : null,
                FalseBranchStepId = falseBranches.TryGetValue(n.Id, out var falseId) ? falseId : null
            })
            .ToList();

        info.AutomationProcessStepsJson = JsonSerializer.Serialize(steps, JsonOpts);

        // Persist trigger data
        if (triggerNode != null)
        {
            var triggerTypeStr = triggerNode.Configuration?.TryGetValue("triggerType", out var tt) == true
                ? tt?.ToString() ?? ""
                : "";
            var trigger = new AutomationTrigger
            {
                Id = triggerNode.Id,
                Name = triggerNode.Name,
                TriggerType = Enum.TryParse<AutomationTriggerType>(triggerTypeStr, out var parsed)
                    ? parsed
                    : AutomationTriggerType.Webhook,
                Configuration = triggerNode.Configuration != null
                    ? JsonSerializer.Serialize(triggerNode.Configuration, JsonOpts)
                    : null
            };
            info.AutomationProcessTriggerJson = JsonSerializer.Serialize(trigger, JsonOpts);
        }

        processInfoProvider.Set(info);

        return ResponseFrom(new AutomationBuilderSaveResult { Status = "validationSuccess" })
            .AddSuccessMessage("Process saved successfully.");
    }

    /// <summary>Returns available step type tiles for the "Add step" picker UI.</summary>
    [PageCommand]
    public Task<ICommandResponse<GetStepTypeTileItemsResult>> GetStepTypeTileItems()
    {
        // Only expose the 6 Kentico-equivalent step types (matching native builder)
        var supported = new[]
        {
            AutomationStepType.SendEmail,
            AutomationStepType.Wait,
            AutomationStepType.Condition,
            AutomationStepType.LogCustomActivity,
            AutomationStepType.SetContactFieldValue,
            AutomationStepType.Finish
        };

        var items = supported.Select(t => new StepTypeTileItem
        {
            StepType = t.ToString(),
            Label = t switch
            {
                AutomationStepType.LogCustomActivity => "Log custom activity",
                AutomationStepType.SetContactFieldValue => "Set contact field value",
                AutomationStepType.SendEmail => "Send email",
                _ => FormatLabel(t.ToString())
            },
            IconName = GetStepIcon(t),
            Description = null
        }).ToList();

        return Task.FromResult(ResponseFrom(new GetStepTypeTileItemsResult { Items = items }));
    }

    /// <summary>Returns available trigger type tiles for the "Add trigger" picker UI.</summary>
    [PageCommand]
    public Task<ICommandResponse<GetTriggerTypeTileItemsResult>> GetTriggerTypeTileItems()
    {
        var supported = new[]
        {
            // Kentico-equivalent triggers
            AutomationTriggerType.FormSubmission,
            AutomationTriggerType.MemberRegistration,
            AutomationTriggerType.CustomActivity,
            // Ecommerce triggers
            AutomationTriggerType.OrderPlaced,
            AutomationTriggerType.OrderStatusChanged,
            AutomationTriggerType.CartAbandoned,
            AutomationTriggerType.ProductPurchased,
            AutomationTriggerType.PaymentFailed,
            AutomationTriggerType.RefundIssued,
            AutomationTriggerType.ProductBackInStock,
            AutomationTriggerType.WishlistUpdated,
            AutomationTriggerType.CouponUsed,
            AutomationTriggerType.SubscriptionCreated,
            AutomationTriggerType.SubscriptionRenewed,
            AutomationTriggerType.SubscriptionCancelled,
            AutomationTriggerType.LoyaltyTierChanged,
            AutomationTriggerType.SpendingThresholdReached
        };

        var items = supported.Select(t => new TriggerTypeTileItem
        {
            TriggerType = t.ToString(),
            Label = t switch
            {
                AutomationTriggerType.FormSubmission => "Form",
                AutomationTriggerType.MemberRegistration => "Registration",
                AutomationTriggerType.CustomActivity => "Custom activity",
                AutomationTriggerType.OrderPlaced => "Order placed",
                AutomationTriggerType.OrderStatusChanged => "Order status changed",
                AutomationTriggerType.CartAbandoned => "Cart abandoned",
                AutomationTriggerType.ProductPurchased => "Product purchased",
                AutomationTriggerType.PaymentFailed => "Payment failed",
                AutomationTriggerType.RefundIssued => "Refund issued",
                AutomationTriggerType.ProductBackInStock => "Product back in stock",
                AutomationTriggerType.WishlistUpdated => "Wishlist updated",
                AutomationTriggerType.CouponUsed => "Coupon used",
                AutomationTriggerType.SubscriptionCreated => "Subscription created",
                AutomationTriggerType.SubscriptionRenewed => "Subscription renewed",
                AutomationTriggerType.SubscriptionCancelled => "Subscription cancelled",
                AutomationTriggerType.LoyaltyTierChanged => "Loyalty tier changed",
                AutomationTriggerType.SpendingThresholdReached => "Spending threshold",
                _ => FormatLabel(t.ToString())
            },
            IconName = GetTriggerIcon(t),
            Description = t switch
            {
                AutomationTriggerType.FormSubmission => "Triggers the process for visitors who submit the selected form.",
                AutomationTriggerType.MemberRegistration => "Triggers the process for visitors who register as members.",
                AutomationTriggerType.CustomActivity => "Triggers the process for visitors who perform the selected custom activity.",
                AutomationTriggerType.OrderPlaced => "Triggers when a customer places an order.",
                AutomationTriggerType.OrderStatusChanged => "Triggers when an order status changes to a specific value.",
                AutomationTriggerType.CartAbandoned => "Triggers when a shopping cart is abandoned after inactivity.",
                AutomationTriggerType.ProductPurchased => "Triggers when a specific product is purchased.",
                AutomationTriggerType.PaymentFailed => "Triggers when a payment attempt fails.",
                AutomationTriggerType.RefundIssued => "Triggers when a refund is issued for an order.",
                AutomationTriggerType.ProductBackInStock => "Triggers when a product becomes available again.",
                AutomationTriggerType.WishlistUpdated => "Triggers when a customer's wishlist is updated.",
                AutomationTriggerType.CouponUsed => "Triggers when a coupon code is redeemed.",
                AutomationTriggerType.SubscriptionCreated => "Triggers when a new subscription is created.",
                AutomationTriggerType.SubscriptionRenewed => "Triggers when a subscription is renewed.",
                AutomationTriggerType.SubscriptionCancelled => "Triggers when a subscription is cancelled.",
                AutomationTriggerType.LoyaltyTierChanged => "Triggers when a customer's loyalty tier changes.",
                AutomationTriggerType.SpendingThresholdReached => "Triggers when a customer reaches a spending threshold.",
                _ => null
            }
        }).ToList();

        return Task.FromResult(ResponseFrom(new GetTriggerTypeTileItemsResult { Items = items }));
    }

    private static string FormatLabel(string enumName) =>
        System.Text.RegularExpressions.Regex.Replace(enumName, "([a-z])([A-Z])", "$1 $2");

    /// <summary>Returns form field definitions for a given step/trigger type.</summary>
    [PageCommand]
    public async Task<ICommandResponse<GetStepFormDefinitionResult>> GetStepFormDefinition(
        GetStepFormDefinitionArgs args)
    {
        var fields = args.StepType == "Trigger" && !string.IsNullOrEmpty(args.TriggerType)
            ? await GetTriggerFormFieldsAsync(args.TriggerType)
            : await GetStepFormFieldsAsync(args.StepType);

        // Append codeName field to all step/trigger forms (matches native CodeNameComponent)
        fields.Add(new()
        {
            Name = "codeName",
            Label = "Code name",
            FieldType = "codename",
            Required = true
        });

        var headline = args.StepType == "Trigger" && !string.IsNullOrEmpty(args.TriggerType)
            ? GetTriggerFormHeadline(args.TriggerType)
            : GetStepFormHeadline(args.StepType);

        var result = new GetStepFormDefinitionResult
        {
            StepType = args.StepType,
            Headline = headline,
            Fields = fields
        };

        return ResponseFrom(result);
    }

    /// <summary>Returns macro rules for the condition picker, filtered to automation condition step rules.</summary>
    [PageCommand]
    public Task<ICommandResponse<GetConditionRulesResult>> GetConditionRules()
    {
        var categories = macroRuleCategoryInfoProvider.Get()
            .WhereEquals(nameof(MacroRuleCategoryInfo.MacroRuleCategoryEnabled), true)
            .GetEnumerableTypedResult()
            .Select(c => new ConditionRuleCategory
            {
                Id = c.MacroRuleCategoryName,
                Label = c.MacroRuleCategoryDisplayName
            })
            .ToList();

        var rules = macroRuleInfoProvider.Get()
            .WhereEquals(nameof(MacroRuleInfo.MacroRuleEnabled), true)
            .GetEnumerableTypedResult()
            .Where(r => r.MacroRuleUsageLocation.HasFlag(MacroRuleUsageLocation.AutomationConditionStep))
            .ToList();

        // Build the category mapping: MacroRuleID → category name
        // Use InfoProvider to query the binding table directly
        var bindingProvider = Provider<MacroRuleMacroRuleCategoryInfo>.Instance;
        var allBindings = bindingProvider.Get().GetEnumerableTypedResult().ToList();

        var categoryLookup = macroRuleCategoryInfoProvider.Get()
            .GetEnumerableTypedResult()
            .ToDictionary(c => c.MacroRuleCategoryID, c => c.MacroRuleCategoryName);

        var categoryByRuleId = new Dictionary<int, string>();
        foreach (var b in allBindings)
        {
            if (categoryLookup.TryGetValue(b.MacroRuleCategoryID, out var catName))
                categoryByRuleId[b.MacroRuleID] = catName;
        }

        var items = rules.Select(r =>
        {
            categoryByRuleId.TryGetValue(r.MacroRuleID, out var catName);
            return new ConditionRuleItem
            {
                Value = r.MacroRuleGUID.ToString(),
                Label = r.MacroRuleDisplayName,
                CategoryId = catName ?? "",
                RuleText = r.MacroRuleText,
                Parameters = ParseMacroRuleParameters(r.MacroRuleParameters)
            };
        }).ToList();

        return Task.FromResult(ResponseFrom(new GetConditionRulesResult { Items = items, Categories = categories }));
    }

    /// <summary>Returns email configurations for the object selector.</summary>
    [PageCommand]
    public Task<ICommandResponse<GetEmailsForSelectorResult>> GetEmailsForSelector()
    {
        var allChannels = channelInfoProvider.Get()
            .GetEnumerableTypedResult()
            .ToDictionary(c => c.ChannelID, c => c.ChannelDisplayName);

        var channels = emailChannelInfoProvider.Get()
            .GetEnumerableTypedResult()
            .Select(c => new EmailChannelOption
            {
                Id = c.EmailChannelID,
                Name = allChannels.TryGetValue(c.EmailChannelChannelID, out var displayName)
                    ? displayName
                    : c.EmailChannelSendingDomain
            })
            .ToList();

        var emailConfigs = emailConfigurationInfoProvider.Get()
            .WhereEquals("EmailConfigurationPurpose", "Automation")
            .GetEnumerableTypedResult()
            .ToList();

        var contentItemIds = emailConfigs
            .Select(e => e.EmailConfigurationContentItemID)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        var displayNames = contentItemLangMetaInfoProvider.Get()
            .WhereIn("ContentItemLanguageMetadataContentItemID", contentItemIds)
            .GetEnumerableTypedResult()
            .GroupBy(m => m.ContentItemLanguageMetadataContentItemID)
            .ToDictionary(
                g => g.Key,
                g => (DisplayName: g.First().ContentItemLanguageMetadataDisplayName,
                      Status: g.First().ContentItemLanguageMetadataLatestVersionStatus));

        var emails = emailConfigs
            .Select(e =>
            {
                var channel = channels.FirstOrDefault(c => c.Id == e.EmailConfigurationEmailChannelID);
                displayNames.TryGetValue(e.EmailConfigurationContentItemID, out var meta);
                return new EmailSelectorItem
                {
                    Guid = e.EmailConfigurationGUID.ToString(),
                    Name = !string.IsNullOrEmpty(meta.DisplayName) ? meta.DisplayName : e.EmailConfigurationName,
                    Purpose = FormatPurpose(e.EmailConfigurationPurpose.ToString()),
                    ChannelName = channel?.Name ?? "",
                    Status = FormatEmailStatus(meta.Status)
                };
            })
            .ToList();

        return Task.FromResult(ResponseFrom(new GetEmailsForSelectorResult { Emails = emails, Channels = channels }));
    }

    /// <summary>Parses MacroRuleParameters XML into a list of parameter definitions.</summary>
    private List<ConditionRuleParameter> ParseMacroRuleParameters(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return [];

        try
        {
            var doc = XDocument.Parse(xml);
            var fields = doc.Root?.Elements("field") ?? [];

            return fields.Select(f =>
            {
                var colName = f.Attribute("column")?.Value ?? "";
                var colType = f.Attribute("columntype")?.Value ?? "text";
                var props = f.Element("properties");
                var settings = f.Element("settings");
                var controlName = settings?.Element("controlname")?.Value ?? "";
                var caption = props?.Element("fieldcaption")?.Value ?? colName;
                var defaultValue = props?.Element("defaultvalue")?.Value;
                var placeholder = settings?.Element("Placeholder")?.Value
                    ?? settings?.Element("WatermarkText")?.Value
                    ?? caption;

                string controlType = controlName switch
                {
                    "Kentico.Administration.DropDownSelector" => "dropdown",
                    "Kentico.Administration.ObjectCodeNameSelector" => "dropdown",
                    "Kentico.Administration.EmailSelector" => "selectExisting",
                    "Kentico.Administration.WebPageSelector" => "selectExisting",
                    "Kentico.Administration.NumberInput" => "number",
                    _ => "text"
                };

                List<StepFormFieldOption>? options = null;
                var optionsText = settings?.Element("Options")?.Value;
                if (!string.IsNullOrEmpty(optionsText))
                {
                    options = optionsText
                        .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
                        .Select(line =>
                        {
                            var parts = line.Split(';', 2);
                            return new StepFormFieldOption
                            {
                                Value = parts[0],
                                Label = parts.Length > 1 ? parts[1] : parts[0]
                            };
                        })
                        .ToList();
                }

                // ObjectCodeNameSelector: populate options from the DB by ObjectType
                if (controlName == "Kentico.Administration.ObjectCodeNameSelector" && options is null)
                {
                    var objectType = settings?.Element("ObjectType")?.Value;
                    if (!string.IsNullOrEmpty(objectType))
                    {
                        options = GetObjectSelectorOptions(objectType);
                    }
                }

                // ContactFieldConfigurator: populate contact field options
                var configurator = settings?.Element("configuratorfulltype")?.Value ?? "";
                if (configurator.Contains("ContactFieldConfigurator", StringComparison.OrdinalIgnoreCase))
                {
                    options = GetContactFieldOptions();
                }

                // EmailSelector: populate email configuration options
                if (controlName == "Kentico.Administration.EmailSelector" && options is null)
                {
                    options = GetEmailSelectorOptions();
                }

                // WebPageSelector: populate web page options
                if (controlName == "Kentico.Administration.WebPageSelector" && options is null)
                {
                    options = GetWebPageSelectorOptions();
                }

                return new ConditionRuleParameter
                {
                    Name = colName,
                    Caption = caption,
                    ControlType = controlType,
                    Placeholder = placeholder,
                    DefaultValue = defaultValue,
                    Options = options
                };
            }).ToList();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>Returns options for ObjectCodeNameSelector fields by querying the DB.</summary>
    private static List<StepFormFieldOption> GetObjectSelectorOptions(string objectType)
    {
        try
        {
            var query = new ObjectQuery(objectType);
            return query.GetEnumerableTypedResult()
                .Select(obj => new StepFormFieldOption
                {
                    Value = obj.Generalized.ObjectCodeName,
                    Label = obj.Generalized.ObjectDisplayName
                })
                .OrderBy(o => o.Label)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>Returns contact field options for the ContactFieldConfigurator dropdown.</summary>
    private static List<StepFormFieldOption> GetContactFieldOptions() =>
    [
        new() { Value = "ContactFirstName", Label = "First name" },
        new() { Value = "ContactMiddleName", Label = "Middle name" },
        new() { Value = "ContactLastName", Label = "Last name" },
        new() { Value = "ContactJobTitle", Label = "Job title" },
        new() { Value = "ContactAddress1", Label = "Address" },
        new() { Value = "ContactCity", Label = "City" },
        new() { Value = "ContactZIP", Label = "ZIP" },
        new() { Value = "ContactMobilePhone", Label = "Mobile phone" },
        new() { Value = "ContactBusinessPhone", Label = "Business phone" },
        new() { Value = "ContactEmail", Label = "Email" },
        new() { Value = "ContactCompanyName", Label = "Company name" },
        new() { Value = "ContactNotes", Label = "Notes" },
        new() { Value = "ContactCampaign", Label = "Campaign" },
    ];

    /// <summary>Returns email configuration options for EmailSelector parameters.</summary>
    private List<StepFormFieldOption> GetEmailSelectorOptions()
    {
        try
        {
            var contentItemIds = emailConfigurationInfoProvider.Get()
                .GetEnumerableTypedResult()
                .Select(e => (e.EmailConfigurationGUID, e.EmailConfigurationName, e.EmailConfigurationContentItemID))
                .ToList();

            var displayNames = contentItemLangMetaInfoProvider.Get()
                .WhereIn("ContentItemLanguageMetadataContentItemID",
                    contentItemIds.Select(e => e.EmailConfigurationContentItemID).Where(id => id > 0).Distinct().ToList())
                .GetEnumerableTypedResult()
                .GroupBy(m => m.ContentItemLanguageMetadataContentItemID)
                .ToDictionary(g => g.Key, g => g.First().ContentItemLanguageMetadataDisplayName);

            return contentItemIds
                .Select(e =>
                {
                    displayNames.TryGetValue(e.EmailConfigurationContentItemID, out var displayName);
                    return new StepFormFieldOption
                    {
                        Value = e.EmailConfigurationGUID.ToString(),
                        Label = !string.IsNullOrEmpty(displayName) ? displayName : e.EmailConfigurationName
                    };
                })
                .OrderBy(o => o.Label)
                .ToList();
        }
        catch { return []; }
    }

    /// <summary>Returns web page options for WebPageSelector parameters.</summary>
    private List<StepFormFieldOption> GetWebPageSelectorOptions()
    {
        try
        {
            var pages = new ObjectQuery("cms.webpageitem")
                .GetEnumerableTypedResult()
                .Select(p => new StepFormFieldOption
                {
                    Value = p["WebPageItemGUID"]?.ToString() ?? "",
                    Label = p["WebPageItemTreePath"]?.ToString() ?? p.Generalized.ObjectDisplayName
                })
                .Where(o => !string.IsNullOrEmpty(o.Value))
                .OrderBy(o => o.Label)
                .ToList();
            return pages;
        }
        catch { return []; }
    }

    private static string FormatPurpose(string purpose) => purpose switch
    {
        "Automation" => "Automation",
        "FormAutoresponder" => "Form autoresponder",
        "Confirmation" => "Confirmation",
        "Regular" => "Regular",
        "CommerceOrderStatusChange" => "Commerce order status change",
        _ => purpose
    };

    private static string FormatEmailStatus(VersionStatus status) => status switch
    {
        VersionStatus.InitialDraft => "Draft (Initial)",
        VersionStatus.Draft => "Draft",
        VersionStatus.Published => "Published",
        _ => "Draft"
    };

    private static string GetStepFormHeadline(string stepType) => stepType switch
    {
        "SendEmail" => "Send email settings",
        "Wait" => "Wait settings",
        "Condition" => "Condition settings",
        "LogCustomActivity" => "Log custom activity settings",
        "SetContactFieldValue" => "Set contact field value settings",
        "Finish" => "Finish settings",
        "Trigger" => "Trigger settings",
        _ => $"{FormatLabel(stepType)} settings"
    };

    private static string GetTriggerFormHeadline(string triggerType) => triggerType switch
    {
        "FormSubmission" => "Form settings",
        "MemberRegistration" => "Registration settings",
        "CustomActivity" => "Custom activity settings",
        "Webhook" => "Webhook settings",
        "Manual" => "Manual settings",
        "Scheduled" => "Scheduled settings",
        "OrderPlaced" => "Order placed settings",
        "OrderStatusChanged" => "Order status changed settings",
        "CartAbandoned" => "Cart abandoned settings",
        "ProductPurchased" => "Product purchased settings",
        "PaymentFailed" => "Payment failed settings",
        "RefundIssued" => "Refund issued settings",
        "ProductBackInStock" => "Product back in stock settings",
        "WishlistUpdated" => "Wishlist updated settings",
        "CouponUsed" => "Coupon used settings",
        "SubscriptionCreated" => "Subscription created settings",
        "SubscriptionRenewed" => "Subscription renewed settings",
        "SubscriptionCancelled" => "Subscription cancelled settings",
        "LoyaltyTierChanged" => "Loyalty tier changed settings",
        "SpendingThresholdReached" => "Spending threshold reached settings",
        _ => $"{FormatLabel(triggerType)} settings"
    };

    private async Task<List<StepFormFieldDefinition>> GetTriggerFormFieldsAsync(string triggerType)
    {
        var fields = new List<StepFormFieldDefinition>
        {
            new() { Name = "name", Label = "Trigger name", FieldType = "text", Required = true }
        };

        switch (triggerType)
        {
            case "FormSubmission":
                {
                    var forms = formInfoProvider.Get().GetEnumerableTypedResult().ToList();
                    var options = forms.Select(f => new StepFormFieldOption
                    {
                        Value = f.FormName,
                        Label = f.FormDisplayName,
                        EditUrl = $"/admin/forms/{f.FormID}/builder"
                    }).ToList();
                    fields.Add(new()
                    {
                        Name = "formCodeName",
                        Label = "Select a form",
                        FieldType = "select",
                        Required = true,
                        Options = options
                    });
                    break;
                }
            case "CustomActivity":
                {
                    var types = activityTypeInfoProvider.Get()
                        .WhereEquals(nameof(ActivityTypeInfo.ActivityTypeIsCustom), true)
                        .GetEnumerableTypedResult().ToList();
                    var options = types.Select(t => new StepFormFieldOption
                    {
                        Value = t.ActivityTypeName,
                        Label = t.ActivityTypeDisplayName
                    }).ToList();
                    fields.Add(new()
                    {
                        Name = "activityType",
                        Label = "Select an activity type",
                        FieldType = "select",
                        Required = true,
                        Options = options
                    });
                    break;
                }
            case "Webhook":
                fields.Add(new() { Name = "webhookUrl", Label = "Webhook URL", FieldType = "text", Required = true, Placeholder = "https://..." });
                fields.Add(new() { Name = "secret", Label = "Secret", FieldType = "text", Placeholder = "Webhook secret for validation" });
                break;
            case "Scheduled":
                fields.Add(new() { Name = "cronExpression", Label = "Schedule (cron)", FieldType = "text", Required = true, Placeholder = "e.g. 0 9 * * 1" });
                fields.Add(new() { Name = "timeZone", Label = "Time zone", FieldType = "text", Placeholder = "e.g. UTC" });
                break;
            case "OrderPlaced":
                fields.Add(new() { Name = "minOrderValue", Label = "Minimum order value", FieldType = "number", Placeholder = "e.g. 100" });
                break;
            case "OrderStatusChanged":
                fields.Add(new() { Name = "targetStatus", Label = "Target status", FieldType = "text", Required = true, Placeholder = "e.g. Shipped" });
                break;
            case "CartAbandoned":
                fields.Add(new() { Name = "abandonmentValue", Label = "Abandonment delay", FieldType = "number", DefaultValue = "1", Required = true, Placeholder = "e.g. 1" });
                fields.Add(new()
                {
                    Name = "abandonmentUnit",
                    Label = "Time unit",
                    FieldType = "radio",
                    Required = true,
                    DefaultValue = "hours",
                    Options =
                    [
                        new() { Value = "minutes", Label = "Minute(s)" },
                        new() { Value = "hours", Label = "Hour(s)" },
                        new() { Value = "days", Label = "Day(s)" }
                    ]
                });
                fields.Add(new() { Name = "minCartValue", Label = "Minimum cart value", FieldType = "number", Placeholder = "e.g. 50.00" });
                fields.Add(new() { Name = "excludeReturningCustomers", Label = "Exclude returning customers", FieldType = "checkbox", DefaultValue = "false" });
                fields.Add(new() { Name = "maxReminders", Label = "Max reminders per contact", FieldType = "number", DefaultValue = "3", Placeholder = "e.g. 3" });
                break;
            case "ProductPurchased":
            case "ProductBackInStock":
                fields.Add(new() { Name = "productSkuId", Label = "Product SKU", FieldType = "text", Placeholder = "SKU identifier" });
                break;
            case "CouponUsed":
                fields.Add(new() { Name = "couponCode", Label = "Coupon code", FieldType = "text", Placeholder = "Specific coupon code (optional)" });
                break;
            case "LoyaltyTierChanged":
                fields.Add(new() { Name = "targetTier", Label = "Target tier", FieldType = "text", Placeholder = "e.g. Gold" });
                break;
            case "SpendingThresholdReached":
                fields.Add(new() { Name = "thresholdAmount", Label = "Threshold amount", FieldType = "number", Required = true, Placeholder = "e.g. 500" });
                break;
        }

        return await Task.FromResult(fields);
    }

    private async Task<List<StepFormFieldDefinition>> GetStepFormFieldsAsync(string stepType)
    {
        switch (stepType)
        {
            case "SendEmail":
                {
                    // Load emails from DB for "Choose an email" select (matches native "Select existing" button)
                    var emails = await Task.FromResult(new List<StepFormFieldOption>());
                    // TODO: Load actual email content items when email infrastructure is available
                    return
                    [
                        new() { Name = "name", Label = "Step name", FieldType = "text", Required = true },
                    new() { Name = "emailGuid", Label = "Choose an email", FieldType = "objectSelector", Required = true, HelpText = "Emails with the 'Form autoresponder' purpose are only allowed if the process uses the 'Form' trigger type." }
                    ];
                }
            case "Wait":
                {
                    return await Task.FromResult<List<StepFormFieldDefinition>>(
                    [
                        new() { Name = "name", Label = "Step name", FieldType = "text", Required = true },
                    new() { Name = "periodType", Label = "Specific date or interval", FieldType = "radio", Required = true, DefaultValue = "interval", Options = [new() { Value = "interval", Label = "Specific interval" }, new() { Value = "date", Label = "Specific date and time" }] },
                    new() { Name = "intervalValue", Label = "After", FieldType = "number", Required = true, DefaultValue = "1", VisibleWhen = new() { FieldName = "periodType", Value = "interval" } },
                    new() { Name = "intervalUnit", Label = "Specific time interval (hours, days, months)", FieldType = "radio", Required = true, DefaultValue = "minutes", Options = [new() { Value = "minutes", Label = "Minute(s)" }, new() { Value = "hours", Label = "Hour(s)" }, new() { Value = "days", Label = "Day(s)" }, new() { Value = "months", Label = "Month(s)" }], VisibleWhen = new() { FieldName = "periodType", Value = "interval" } },
                    new() { Name = "waitUntilDate", Label = "Date and time", FieldType = "datetime", Required = true, VisibleWhen = new() { FieldName = "periodType", Value = "date" } }
                    ]);
                }
            case "Condition":
                {
                    return await Task.FromResult<List<StepFormFieldDefinition>>(
                    [
                        new() { Name = "name", Label = "Step name", FieldType = "text", Required = true },
                    new() { Name = "condition", Label = "Select a condition", FieldType = "conditionBuilder", Required = true }
                    ]);
                }
            case "LogCustomActivity":
                {
                    // Load activity types for select
                    var types = activityTypeInfoProvider.Get()
                        .WhereEquals(nameof(ActivityTypeInfo.ActivityTypeIsCustom), true)
                        .GetEnumerableTypedResult().ToList();
                    var activityOptions = types.Select(t => new StepFormFieldOption
                    {
                        Value = t.ActivityTypeName,
                        Label = t.ActivityTypeDisplayName
                    }).ToList();

                    return
                    [
                        new() { Name = "name", Label = "Step name", FieldType = "text", Required = true },
                    new() { Name = "activityType", Label = "Activity type", FieldType = "select", Required = true, Options = activityOptions },
                    new() { Name = "title", Label = "Activity title", FieldType = "text" },
                    new() { Name = "value", Label = "Activity value", FieldType = "text" },
                    new() { Name = "activityChannel", Label = "Activity channel", FieldType = "select", Options = [] }
                    ];
                }
            case "SetContactFieldValue":
                {
                    // Standard contact fields shown by native automation builder
                    var contactFields = new List<StepFormFieldOption>
                    {
                        new() { Value = "ContactAddress1", Label = "Address" },
                        new() { Value = "ContactBirthday", Label = "Birthday" },
                        new() { Value = "ContactBusinessPhone", Label = "Business phone" },
                        new() { Value = "ContactCity", Label = "City" },
                        new() { Value = "ContactCompanyName", Label = "Company" },
                        new() { Value = "ContactFirstName", Label = "First name" },
                        new() { Value = "ContactJobTitle", Label = "Job title" },
                        new() { Value = "ContactLastName", Label = "Last name" },
                        new() { Value = "ContactMiddleName", Label = "Middle name" },
                        new() { Value = "ContactNotes", Label = "Notes" },
                        new() { Value = "ContactMobilePhone", Label = "Private phone" },
                        new() { Value = "ContactZIP", Label = "ZIP code" }
                    };

                    return
                    [
                        new() { Name = "name", Label = "Step name", FieldType = "text", Required = true },
                    new() { Name = "contactFieldName", Label = "Contact field name", FieldType = "combobox", Required = true, Options = contactFields, Placeholder = "Choose an option" }
                    ];
                }
            case "Finish":
                return await Task.FromResult<List<StepFormFieldDefinition>>(
                [
                    new() { Name = "name", Label = "Step name", FieldType = "text", Required = true }
                ]);
            case "Trigger":
                return await Task.FromResult<List<StepFormFieldDefinition>>(
                [
                    new() { Name = "name", Label = "Trigger name", FieldType = "text", Required = true }
                ]);
            default:
                return await Task.FromResult<List<StepFormFieldDefinition>>(
                [
                    new() { Name = "name", Label = "Step name", FieldType = "text", Required = true }
                ]);
        }
    }

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
}
