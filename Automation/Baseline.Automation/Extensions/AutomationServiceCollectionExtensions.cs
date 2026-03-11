using Baseline.Automation.Actions;
using Baseline.Automation.Configuration;
using Baseline.Automation.Email;
using Baseline.Automation.Engine;
using Baseline.Automation.Installers;
using Baseline.Automation.Scheduling;
using Baseline.Automation.Services;
using Baseline.Automation.Steps;
using Baseline.Automation.Triggers;
using Microsoft.Extensions.DependencyInjection;

namespace Baseline.Automation;

/// <summary>
/// Extension methods for registering Baseline Automation Engine services.
/// </summary>
public static class AutomationServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Baseline Automation Engine to the service collection.
    /// </summary>
    public static IServiceCollection AddBaselineAutomation(
        this IServiceCollection services,
        Action<AutomationOptions>? configure = null)
    {
        // Register options
        services.AddOptions<AutomationOptions>()
            .BindConfiguration(AutomationOptions.SectionName)
            .Configure(opt => configure?.Invoke(opt));

        services.AddOptions<TriggerOptions>()
            .BindConfiguration("Baseline:Automation:Triggers");

        // Build options for conditional registration
        var options = new AutomationOptions();
        configure?.Invoke(options);

        // --- Core Engine ---
        services.AddScoped<IAutomationEngine, AutomationEngine>();
        services.AddScoped<IAutomationProcessService, AutomationProcessService>();
        services.AddScoped<IAutomationConditionEvaluator, AutomationConditionEvaluator>();

        // --- Event Dispatcher ---
        services.AddScoped<IAutomationTriggerDispatcher, AutomationTriggerDispatcher>();

        // --- Engine Manager ---
        services.AddScoped<AutomationManager>();

        // --- Trigger Executor ---
        services.AddScoped<AutomationTriggerExecutor>();

        // --- Step DI Resolver ---
        services.AddScoped<StepTypeDependencyInjector>();

        // --- Email Adapter ---
        services.AddScoped<IAutomationEmailAdapter, DefaultAutomationEmailAdapter>();

        // --- Repositories ---
        if (options.UseInMemoryStorage)
        {
            services.AddSingleton<IProcessRepository, InMemoryProcessRepository>();
            services.AddSingleton<IProcessStateRepository, InMemoryProcessStateRepository>();
        }
        else
        {
            services.AddScoped<IProcessRepository, DatabaseProcessRepository>();
            services.AddScoped<IProcessStateRepository, DatabaseProcessStateRepository>();
        }

        // --- Module Installer ---
        services.AddTransient<AutomationModuleInstaller>();

        // --- Built-in Action Executors (original interface) ---
        services.AddScoped<IAutomationActionExecutor, SendEmailActionExecutor>();
        services.AddScoped<IAutomationActionExecutor, WaitActionExecutor>();
        services.AddScoped<IAutomationActionExecutor, LogActivityActionExecutor>();
        services.AddScoped<IAutomationActionExecutor, SetContactFieldValueActionExecutor>();
        services.AddScoped<IAutomationActionExecutor, ConditionActionExecutor>();

        // --- Generic Action Executors ---
        services.AddScoped<IAutomationActionExecutor, FlagContactActionExecutor>();
        services.AddScoped<IAutomationActionExecutor, UpdateContactGroupActionExecutor>();
        services.AddScoped<IAutomationActionExecutor, CallWebhookActionExecutor>();
        services.AddScoped<IAutomationActionExecutor, SendNotificationActionExecutor>();

        // --- New Action implementations (IAutomationAction) ---
        services.AddScoped<IAutomationAction, EmailAction>();
        services.AddScoped<IAutomationAction, StartProcessAction>();

        // --- Trigger Handlers ---
        services.AddScoped<IAutomationTriggerHandler, FormSubmissionTriggerHandler>();
        services.AddScoped<IAutomationTriggerHandler, MemberRegistrationTriggerHandler>();
        services.AddScoped<IAutomationTriggerHandler, CustomActivityTriggerHandler>();
        services.AddScoped<IAutomationTriggerHandler, WebhookTriggerHandler>();

        // --- Ecommerce Trigger Handlers ---
        services.AddScoped<IAutomationTriggerHandler, OrderPlacedTriggerHandler>();
        services.AddScoped<IAutomationTriggerHandler, OrderStatusChangedTriggerHandler>();
        services.AddScoped<IAutomationTriggerHandler, CartAbandonedTriggerHandler>();
        services.AddScoped<IAutomationTriggerHandler, ProductPurchasedTriggerHandler>();
        services.AddScoped<IAutomationTriggerHandler, PaymentFailedTriggerHandler>();
        services.AddScoped<IAutomationTriggerHandler, RefundIssuedTriggerHandler>();
        services.AddScoped<IAutomationTriggerHandler, ProductBackInStockTriggerHandler>();
        services.AddScoped<IAutomationTriggerHandler, WishlistUpdatedTriggerHandler>();
        services.AddScoped<IAutomationTriggerHandler, CouponUsedTriggerHandler>();
        services.AddScoped<IAutomationTriggerHandler, SubscriptionCreatedTriggerHandler>();
        services.AddScoped<IAutomationTriggerHandler, SubscriptionRenewedTriggerHandler>();
        services.AddScoped<IAutomationTriggerHandler, SubscriptionCancelledTriggerHandler>();
        services.AddScoped<IAutomationTriggerHandler, LoyaltyTierChangedTriggerHandler>();
        services.AddScoped<IAutomationTriggerHandler, SpendingThresholdReachedTriggerHandler>();

        // --- Background Services ---
        if (options.EnableBackgroundProcessing)
        {
            services.AddHostedService<AutomationBackgroundService>();
            services.AddHostedService<AutomationStatisticsCalculationTask>();
        }

        // --- Statistics ---
        services.AddScoped<IAutomationProcessStatisticsCalculator, NoOpStatisticsCalculator>();

        // HttpClient for webhook calls
        services.AddHttpClient("AutomationWebhook");

        return services;
    }

    /// <summary>
    /// No-op statistics calculator used as default when no real implementation is registered.
    /// </summary>
    private class NoOpStatisticsCalculator : IAutomationProcessStatisticsCalculator
    {
        public Task CalculateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CalculateForProcessAsync(Guid processId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task IncrementContactAtStepAsync(int stepId) => Task.CompletedTask;
        public Task DecrementContactAtStepAsync(int stepId) => Task.CompletedTask;
        public Task RecordStepPassedAsync(int stepId, TimeSpan duration) => Task.CompletedTask;
    }
}
