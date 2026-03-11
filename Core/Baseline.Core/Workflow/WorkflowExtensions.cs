using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Baseline.Core.Workflow;

/// <summary>
/// Workflow automation API for Baseline v3.
/// Provides fluent workflow definition and execution for content operations.
/// </summary>
public static class WorkflowExtensions
{
    /// <summary>
    /// Adds workflow automation services.
    /// </summary>
    public static IServiceCollection AddBaselineWorkflows(this IServiceCollection services)
    {
        services.AddScoped<IWorkflowEngine, WorkflowEngine>();
        services.AddScoped<IWorkflowBuilder, WorkflowBuilder>();
        return services;
    }
}

/// <summary>
/// Interface for building content workflows.
/// </summary>
public interface IWorkflowBuilder
{
    /// <summary>
    /// Start building a workflow for a content type.
    /// </summary>
    IContentWorkflow<T> ForContentType<T>() where T : class;
}

/// <summary>
/// Content workflow definition.
/// </summary>
public interface IContentWorkflow<T> where T : class
{
    /// <summary>
    /// Trigger workflow on content creation.
    /// </summary>
    IContentWorkflow<T> OnCreate();

    /// <summary>
    /// Trigger workflow on content update.
    /// </summary>
    IContentWorkflow<T> OnUpdate();

    /// <summary>
    /// Trigger workflow on content publish.
    /// </summary>
    IContentWorkflow<T> OnPublish();

    /// <summary>
    /// Trigger workflow on content deletion.
    /// </summary>
    IContentWorkflow<T> OnDelete();

    /// <summary>
    /// Add a condition for workflow execution.
    /// </summary>
    IContentWorkflow<T> When(Func<T, bool> condition);

    /// <summary>
    /// Add an async condition for workflow execution.
    /// </summary>
    IContentWorkflow<T> WhenAsync(Func<T, Task<bool>> condition);

    /// <summary>
    /// Execute an action when workflow triggers.
    /// </summary>
    IContentWorkflow<T> Then(Action<T> action);

    /// <summary>
    /// Execute an async action when workflow triggers.
    /// </summary>
    IContentWorkflow<T> ThenAsync(Func<T, CancellationToken, Task> action);

    /// <summary>
    /// Send a notification.
    /// </summary>
    IContentWorkflow<T> Notify(string recipientEmail, string subject, Func<T, string> messageBuilder);

    /// <summary>
    /// Update related content.
    /// </summary>
    IContentWorkflow<T> UpdateRelated<TRelated>(
        Func<T, IEnumerable<TRelated>> selector,
        Action<TRelated> updateAction) where TRelated : class;

    /// <summary>
    /// Log workflow execution.
    /// </summary>
    IContentWorkflow<T> Log(Func<T, string> messageBuilder, LogLevel level = LogLevel.Information);

    /// <summary>
    /// Build and register the workflow.
    /// </summary>
    WorkflowDefinition Build();
}

/// <summary>
/// Workflow definition container.
/// </summary>
public class WorkflowDefinition
{
    public string Name { get; set; } = "";
    public string ContentType { get; set; } = "";
    public List<WorkflowTrigger> Triggers { get; set; } = [];
    public List<WorkflowCondition> Conditions { get; set; } = [];
    public List<WorkflowAction> Actions { get; set; } = [];
}

/// <summary>
/// Workflow trigger types.
/// </summary>
public enum WorkflowTrigger
{
    Create,
    Update,
    Publish,
    Delete
}

/// <summary>
/// Workflow condition.
/// </summary>
public class WorkflowCondition
{
    public required Func<object, Task<bool>> Evaluate { get; init; }
    public string Description { get; set; } = "";
}

/// <summary>
/// Workflow action.
/// </summary>
public class WorkflowAction
{
    public required Func<object, IServiceProvider, CancellationToken, Task> Execute { get; init; }
    public string Description { get; set; } = "";
    public WorkflowActionType Type { get; set; }
}

/// <summary>
/// Workflow action types.
/// </summary>
public enum WorkflowActionType
{
    Custom,
    Notify,
    UpdateRelated,
    Log
}

/// <summary>
/// Interface for workflow engine.
/// </summary>
public interface IWorkflowEngine
{
    /// <summary>
    /// Register a workflow definition.
    /// </summary>
    void Register(WorkflowDefinition workflow);

    /// <summary>
    /// Execute workflows for a content item event.
    /// </summary>
    Task ExecuteAsync<T>(T content, WorkflowTrigger trigger, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Get all registered workflows.
    /// </summary>
    IReadOnlyList<WorkflowDefinition> GetRegisteredWorkflows();
}

/// <summary>
/// Default workflow builder implementation.
/// </summary>
public class WorkflowBuilder : IWorkflowBuilder
{
    public IContentWorkflow<T> ForContentType<T>() where T : class
    {
        return new ContentWorkflowImpl<T>();
    }
}

/// <summary>
/// Content workflow implementation.
/// </summary>
internal class ContentWorkflowImpl<T> : IContentWorkflow<T> where T : class
{
    private readonly WorkflowDefinition _definition = new()
    {
        ContentType = typeof(T).Name
    };

    public IContentWorkflow<T> OnCreate()
    {
        _definition.Triggers.Add(WorkflowTrigger.Create);
        return this;
    }

    public IContentWorkflow<T> OnUpdate()
    {
        _definition.Triggers.Add(WorkflowTrigger.Update);
        return this;
    }

    public IContentWorkflow<T> OnPublish()
    {
        _definition.Triggers.Add(WorkflowTrigger.Publish);
        return this;
    }

    public IContentWorkflow<T> OnDelete()
    {
        _definition.Triggers.Add(WorkflowTrigger.Delete);
        return this;
    }

    public IContentWorkflow<T> When(Func<T, bool> condition)
    {
        _definition.Conditions.Add(new WorkflowCondition
        {
            Evaluate = obj => Task.FromResult(obj is T typed && condition(typed)),
            Description = "Synchronous condition"
        });
        return this;
    }

    public IContentWorkflow<T> WhenAsync(Func<T, Task<bool>> condition)
    {
        _definition.Conditions.Add(new WorkflowCondition
        {
            Evaluate = async obj => obj is T typed && await condition(typed),
            Description = "Async condition"
        });
        return this;
    }

    public IContentWorkflow<T> Then(Action<T> action)
    {
        _definition.Actions.Add(new WorkflowAction
        {
            Execute = (obj, _, _) =>
            {
                if (obj is T typed)
                    action(typed);
                return Task.CompletedTask;
            },
            Type = WorkflowActionType.Custom,
            Description = "Custom action"
        });
        return this;
    }

    public IContentWorkflow<T> ThenAsync(Func<T, CancellationToken, Task> action)
    {
        _definition.Actions.Add(new WorkflowAction
        {
            Execute = async (obj, _, ct) =>
            {
                if (obj is T typed)
                    await action(typed, ct);
            },
            Type = WorkflowActionType.Custom,
            Description = "Async custom action"
        });
        return this;
    }

    public IContentWorkflow<T> Notify(string recipientEmail, string subject, Func<T, string> messageBuilder)
    {
        _definition.Actions.Add(new WorkflowAction
        {
            Execute = async (obj, sp, ct) =>
            {
                if (obj is not T typed)
                    return;

                var message = messageBuilder(typed);
                var logger = sp.GetService<ILogger<ContentWorkflowImpl<T>>>();
                logger?.LogInformation("Workflow notification: To={Email}, Subject={Subject}, Message={Message}",
                    recipientEmail, subject, message);

                // In production, integrate with email service
                await Task.CompletedTask;
            },
            Type = WorkflowActionType.Notify,
            Description = $"Notify {recipientEmail}"
        });
        return this;
    }

    public IContentWorkflow<T> UpdateRelated<TRelated>(
        Func<T, IEnumerable<TRelated>> selector,
        Action<TRelated> updateAction) where TRelated : class
    {
        _definition.Actions.Add(new WorkflowAction
        {
            Execute = (obj, _, _) =>
            {
                if (obj is not T typed)
                    return Task.CompletedTask;

                var relatedItems = selector(typed);
                foreach (var item in relatedItems)
                {
                    updateAction(item);
                }

                return Task.CompletedTask;
            },
            Type = WorkflowActionType.UpdateRelated,
            Description = $"Update related {typeof(TRelated).Name}"
        });
        return this;
    }

    public IContentWorkflow<T> Log(Func<T, string> messageBuilder, LogLevel level = LogLevel.Information)
    {
        _definition.Actions.Add(new WorkflowAction
        {
            Execute = (obj, sp, _) =>
            {
                if (obj is not T typed)
                    return Task.CompletedTask;

                var logger = sp.GetService<ILogger<ContentWorkflowImpl<T>>>();
                var message = messageBuilder(typed);

                logger?.Log(level, "Workflow: {Message}", message);
                return Task.CompletedTask;
            },
            Type = WorkflowActionType.Log,
            Description = "Log message"
        });
        return this;
    }

    public WorkflowDefinition Build()
    {
        _definition.Name = $"{typeof(T).Name}Workflow";
        return _definition;
    }
}

/// <summary>
/// Default workflow engine implementation.
/// </summary>
public class WorkflowEngine : IWorkflowEngine
{
    private readonly List<WorkflowDefinition> _workflows = [];
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkflowEngine> _logger;

    public WorkflowEngine(IServiceProvider serviceProvider, ILogger<WorkflowEngine> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void Register(WorkflowDefinition workflow)
    {
        _workflows.Add(workflow);
        _logger.LogInformation("Registered workflow: {Name} for {ContentType}",
            workflow.Name, workflow.ContentType);
    }

    public async Task ExecuteAsync<T>(T content, WorkflowTrigger trigger, CancellationToken cancellationToken = default)
        where T : class
    {
        var contentType = typeof(T).Name;
        var matchingWorkflows = _workflows
            .Where(w => w.ContentType == contentType && w.Triggers.Contains(trigger))
            .ToList();

        foreach (var workflow in matchingWorkflows)
        {
            try
            {
                // Evaluate conditions
                var shouldExecute = true;
                foreach (var condition in workflow.Conditions)
                {
                    if (!await condition.Evaluate(content))
                    {
                        shouldExecute = false;
                        break;
                    }
                }

                if (!shouldExecute)
                {
                    _logger.LogDebug("Workflow {Name} skipped - conditions not met", workflow.Name);
                    continue;
                }

                // Execute actions
                foreach (var action in workflow.Actions)
                {
                    await action.Execute(content, _serviceProvider, cancellationToken);
                }

                _logger.LogInformation("Workflow {Name} executed successfully for {ContentType}",
                    workflow.Name, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Workflow {Name} failed for {ContentType}", workflow.Name, contentType);
            }
        }
    }

    public IReadOnlyList<WorkflowDefinition> GetRegisteredWorkflows() => _workflows.AsReadOnly();
}

/// <summary>
/// Example workflow configurations.
/// </summary>
public static class WorkflowExamples
{
    /// <summary>
    /// Example: Blog post approval workflow.
    /// </summary>
    public static WorkflowDefinition BlogPostApproval(IWorkflowBuilder builder)
    {
        // This is a usage example - BlogPost would be the generated content type
        /*
        return builder.ForContentType<BlogPost>()
            .OnCreate()
            .OnUpdate()
            .When(post => !post.IsPublished)
            .Notify("editor@example.com", "New blog post for review",
                post => $"Please review: {post.Title}")
            .Log(post => $"Blog post '{post.Title}' pending approval")
            .Build();
        */

        return new WorkflowDefinition { Name = "Example" };
    }
}
