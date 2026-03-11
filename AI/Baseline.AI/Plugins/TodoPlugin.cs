using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text;

using Baseline.AI;

using Microsoft.AspNetCore.Http;
using Microsoft.SemanticKernel;

namespace Baseline.AI.Plugins;

/// <summary>
/// AIRA plugin providing in-session task tracking.
/// Mimics MCP's <c>todo</c> / task management capability.
/// Todos persist in memory for the current admin user session.
/// </summary>
[Description("Tracks tasks and to-do items within the current AIRA session.")]
public sealed class TodoPlugin(IHttpContextAccessor httpContextAccessor) : IAiraPlugin
{
    /// <inheritdoc />
    public string PluginName => "Todo";

    private static readonly ConcurrentDictionary<string, List<TodoItem>> s_todos = new();

    /// <summary>
    /// Adds a new todo item.
    /// </summary>
    [KernelFunction("add_todo")]
    [Description("Adds a new task to the to-do list. " +
                 "Returns the updated list after adding.")]
    public string AddTodo(
        [Description("Task description")] string task,
        [Description("Priority: high, medium, or low (default: medium)")] string? priority = null)
    {
        string key = GetSessionKey();
        var list = s_todos.GetOrAdd(key, _ => []);

        lock (list)
        {
            int nextId = list.Count > 0 ? list.Max(t => t.Id) + 1 : 1;
            list.Add(new TodoItem
            {
                Id = nextId,
                Task = task,
                Priority = NormalizePriority(priority),
                CreatedAt = DateTime.UtcNow
            });
        }

        return FormatTodoList(list);
    }

    /// <summary>
    /// Lists all current todos.
    /// </summary>
    [KernelFunction("list_todos")]
    [Description("Lists all current to-do items with their status, priority, and ID.")]
    public string ListTodos()
    {
        string key = GetSessionKey();

        if (!s_todos.TryGetValue(key, out var list) || list.Count == 0)
        {
            return "No to-do items. Use add_todo to create one.";
        }

        return FormatTodoList(list);
    }

    /// <summary>
    /// Marks a todo as completed.
    /// </summary>
    [KernelFunction("complete_todo")]
    [Description("Marks a to-do item as completed by its ID number.")]
    public string CompleteTodo(
        [Description("The ID number of the to-do to complete")] int id)
    {
        string key = GetSessionKey();

        if (!s_todos.TryGetValue(key, out var list))
        {
            return "No to-do items found.";
        }

        lock (list)
        {
            var item = list.FirstOrDefault(t => t.Id == id);
            if (item is null)
            {
                return $"To-do #{id} not found.";
            }

            item.IsCompleted = true;
            item.CompletedAt = DateTime.UtcNow;
        }

        return FormatTodoList(list);
    }

    /// <summary>
    /// Removes a todo item.
    /// </summary>
    [KernelFunction("remove_todo")]
    [Description("Removes a to-do item by its ID number.")]
    public string RemoveTodo(
        [Description("The ID number of the to-do to remove")] int id)
    {
        string key = GetSessionKey();

        if (!s_todos.TryGetValue(key, out var list))
        {
            return "No to-do items found.";
        }

        lock (list)
        {
            int removed = list.RemoveAll(t => t.Id == id);
            if (removed == 0)
            {
                return $"To-do #{id} not found.";
            }
        }

        return list.Count > 0
            ? FormatTodoList(list)
            : "All to-do items cleared.";
    }

    /// <summary>
    /// Clears all todos for the session.
    /// </summary>
    [KernelFunction("clear_todos")]
    [Description("Removes all to-do items from the current session.")]
    public string ClearTodos()
    {
        string key = GetSessionKey();
        s_todos.TryRemove(key, out _);
        return "All to-do items cleared.";
    }

    private string GetSessionKey()
    {
        var userName = httpContextAccessor.HttpContext?.User?.Identity?.Name;
        return !string.IsNullOrEmpty(userName) ? $"aira-todo-{userName}" : "aira-todo-default";
    }

    private static string NormalizePriority(string? priority) =>
        priority?.ToLowerInvariant() switch
        {
            "high" or "h" => "high",
            "low" or "l" => "low",
            _ => "medium"
        };

    private static string FormatTodoList(List<TodoItem> list)
    {
        lock (list)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## To-Do List");
            sb.AppendLine();

            var pending = list.Where(t => !t.IsCompleted).ToList();
            var completed = list.Where(t => t.IsCompleted).ToList();

            if (pending.Count > 0)
            {
                sb.AppendLine($"### Pending ({pending.Count})");
                foreach (var t in pending)
                {
                    string pri = t.Priority == "high" ? " 🔴" : t.Priority == "low" ? " 🔵" : "";
                    sb.AppendLine($"- [ ] #{t.Id} {t.Task}{pri}");
                }

                sb.AppendLine();
            }

            if (completed.Count > 0)
            {
                sb.AppendLine($"### Completed ({completed.Count})");
                foreach (var t in completed)
                {
                    sb.AppendLine($"- [x] #{t.Id} ~~{t.Task}~~");
                }
            }

            return sb.ToString();
        }
    }

    private sealed class TodoItem
    {
        public int Id { get; init; }
        public string Task { get; init; } = "";
        public string Priority { get; init; } = "medium";
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; init; }
        public DateTime? CompletedAt { get; set; }
    }
}
