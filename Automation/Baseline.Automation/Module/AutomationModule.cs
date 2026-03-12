using CMS;
using CMS.Activities;
using CMS.ContactManagement;
using CMS.Core;
using CMS.DataEngine;
using CMS.Membership;
using CMS.OnlineForms;

using Baseline.Automation.Services;
using Baseline.Automation.Triggers;

using Kentico.Xperience.Admin.Base;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: RegisterModule(typeof(Baseline.Automation.Module.AutomationModule))]

namespace Baseline.Automation.Module;

/// <summary>
/// Xperience module registration for the Baseline Automation Engine.
/// Handles initialization, event binding, module lifecycle,
/// and admin client module registration for embedded React templates.
/// </summary>
public class AutomationModule : AdminModule
{
    private IServiceProvider? _serviceProvider;

    public AutomationModule() : base(BaselineAutomationConstants.ModuleName) { }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        _serviceProvider = parameters.Services;

        RegisterClientModule("baseline", "automation");

        RegisterEventHandlers();
    }

    private void RegisterEventHandlers()
    {
        BizFormItemEvents.Insert.After += OnFormSubmitted;
        MemberInfo.TYPEINFO.Events.Insert.After += OnMemberRegistered;
        ActivityInfo.TYPEINFO.Events.Insert.After += OnActivityLogged;
    }

    private void OnFormSubmitted(object? sender, BizFormItemEventArgs e)
    {
        var formItem = e.Item;
        var formCodeName = formItem.BizFormClassName;

        // Try to resolve contact from form email field
        string? email = null;
        foreach (var col in new[] { "Email", "ContactEmail", "EmailAddress" })
        {
            if (formItem.TryGetValue(col, out object? val) && val is string s && !string.IsNullOrWhiteSpace(s))
            {
                email = s;
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceProvider!.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IAutomationTriggerDispatcher>();
                await dispatcher.FireForEmailAsync(
                    AutomationTriggerType.FormSubmission,
                    email,
                    new { FormCodeName = formCodeName });
            }
            catch (Exception ex)
            {
                var logger = Service.Resolve<ILogger<AutomationModule>>();
                logger?.LogError(ex, "Automation: error dispatching FormSubmission trigger for {Form}", formCodeName);
            }
        });
    }

    private void OnMemberRegistered(object? sender, ObjectEventArgs e)
    {
        if (e.Object is not MemberInfo member)
        {
            return;
        }

        int memberId = member.MemberID;

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceProvider!.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IAutomationTriggerDispatcher>();
                await dispatcher.FireForMemberAsync(
                    AutomationTriggerType.MemberRegistration,
                    memberId,
                    new { MemberEmail = member.MemberEmail });
            }
            catch (Exception ex)
            {
                var logger = Service.Resolve<ILogger<AutomationModule>>();
                logger?.LogError(ex, "Automation: error dispatching MemberRegistration trigger for member {Id}", memberId);
            }
        });
    }

    private void OnActivityLogged(object? sender, ObjectEventArgs e)
    {
        if (e.Object is not ActivityInfo activity)
        {
            return;
        }

        int contactId = activity.ActivityContactID;
        if (contactId <= 0)
        {
            return;
        }

        string activityType = activity.ActivityType;

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceProvider!.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IAutomationTriggerDispatcher>();
                await dispatcher.FireAsync(
                    AutomationTriggerType.CustomActivity,
                    contactId,
                    new { ActivityTypeName = activityType, ActivityValue = activity.ActivityValue });
            }
            catch (Exception ex)
            {
                var logger = Service.Resolve<ILogger<AutomationModule>>();
                logger?.LogError(ex, "Automation: error dispatching CustomActivity trigger for contact {Id}", contactId);
            }
        });
    }
}
