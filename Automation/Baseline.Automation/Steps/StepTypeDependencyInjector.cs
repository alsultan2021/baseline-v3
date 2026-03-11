using Microsoft.Extensions.DependencyInjection;

namespace Baseline.Automation.Steps;

/// <summary>
/// Resolves step type handlers and action implementations from the DI container.
/// Maps to CMS.AutomationEngine.Internal.StepTypeDependencyInjector.
/// </summary>
public class StepTypeDependencyInjector(IServiceProvider serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    /// <summary>
    /// Resolves all registered <see cref="IAutomationActionExecutor"/> implementations.
    /// </summary>
    public IEnumerable<IAutomationActionExecutor> GetActionExecutors() =>
        _serviceProvider.GetServices<IAutomationActionExecutor>();

    /// <summary>
    /// Resolves a specific action executor by step type.
    /// </summary>
    public IAutomationActionExecutor? GetActionExecutor(AutomationStepType stepType) =>
        GetActionExecutors().FirstOrDefault(e => e.StepType == stepType);

    /// <summary>
    /// Resolves all registered <see cref="IAutomationTriggerHandler"/> implementations.
    /// </summary>
    public IEnumerable<IAutomationTriggerHandler> GetTriggerHandlers() =>
        _serviceProvider.GetServices<IAutomationTriggerHandler>();

    /// <summary>
    /// Resolves a specific trigger handler by trigger type.
    /// </summary>
    public IAutomationTriggerHandler? GetTriggerHandler(AutomationTriggerType triggerType) =>
        GetTriggerHandlers().FirstOrDefault(h => h.TriggerType == triggerType);

    /// <summary>
    /// Resolves a service of type T from the container.
    /// </summary>
    public T? Resolve<T>() where T : class =>
        _serviceProvider.GetService<T>();

    /// <summary>
    /// Resolves a required service of type T from the container.
    /// </summary>
    public T ResolveRequired<T>() where T : notnull =>
        _serviceProvider.GetRequiredService<T>();
}
