using System.Collections.Concurrent;

namespace Baseline.Automation.Services;

/// <summary>
/// In-memory implementation of <see cref="IProcessRepository"/>.
/// Thread-safe process storage for development and testing.
/// </summary>
public class InMemoryProcessRepository : IProcessRepository
{
    private readonly ConcurrentDictionary<Guid, AutomationProcess> _processes = new();

    public Task<AutomationProcess?> GetByIdAsync(Guid processId) =>
        Task.FromResult(_processes.TryGetValue(processId, out var process) ? process : null);

    public Task<IEnumerable<AutomationProcess>> GetAllAsync(bool? enabledOnly = null)
    {
        IEnumerable<AutomationProcess> result = _processes.Values;
        if (enabledOnly.HasValue)
        {
            result = result.Where(p => p.IsEnabled == enabledOnly.Value);
        }
        return Task.FromResult(result);
    }

    public Task<IEnumerable<AutomationProcess>> GetByTriggerTypeAsync(AutomationTriggerType triggerType) =>
        Task.FromResult(_processes.Values.Where(p => p.Trigger.TriggerType == triggerType));

    public Task<AutomationProcess> SaveAsync(AutomationProcess process)
    {
        _processes[process.Id] = process;
        return Task.FromResult(process);
    }

    public Task DeleteAsync(Guid processId)
    {
        _processes.TryRemove(processId, out _);
        return Task.CompletedTask;
    }
}

/// <summary>
/// In-memory implementation of <see cref="IProcessStateRepository"/>.
/// Thread-safe state and history storage for development and testing.
/// </summary>
public class InMemoryProcessStateRepository : IProcessStateRepository
{
    private readonly ConcurrentDictionary<Guid, ProcessContactState> _states = new();
    private readonly ConcurrentDictionary<Guid, List<ProcessStepHistory>> _history = new();

    public Task<ProcessContactState> CreateAsync(ProcessContactState state)
    {
        _states[state.Id] = state;
        return Task.FromResult(state);
    }

    public Task<ProcessContactState> UpdateAsync(ProcessContactState state)
    {
        _states[state.Id] = state;
        return Task.FromResult(state);
    }

    public Task<ProcessContactState?> GetByIdAsync(Guid id) =>
        Task.FromResult(_states.TryGetValue(id, out var state) ? state : null);

    public Task<ProcessContactState?> GetByProcessAndContactAsync(Guid processId, int contactId)
    {
        var state = _states.Values
            .Where(s => s.ProcessId == processId && s.ContactId == contactId)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefault();
        return Task.FromResult(state);
    }

    public Task<IEnumerable<ProcessContactState>> GetActiveByProcessAsync(Guid processId) =>
        Task.FromResult(_states.Values
            .Where(s => s.ProcessId == processId &&
                        s.Status is ProcessContactStatus.Active or ProcessContactStatus.Waiting));

    public Task<IEnumerable<ProcessContactState>> GetExpiredWaitingStatesAsync() =>
        Task.FromResult(_states.Values
            .Where(s => s.Status == ProcessContactStatus.Waiting &&
                        s.WaitUntil.HasValue &&
                        s.WaitUntil.Value <= DateTimeOffset.UtcNow));

    public Task<int> GetExecutionCountAsync(Guid processId, int contactId) =>
        Task.FromResult(_states.Values
            .Count(s => s.ProcessId == processId && s.ContactId == contactId));

    public Task AddStepHistoryAsync(ProcessStepHistory history)
    {
        var list = _history.GetOrAdd(history.ProcessContactStateId, _ => []);
        lock (list)
        {
            list.Add(history);
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<ProcessStepHistory>> GetStepHistoryAsync(Guid processContactStateId)
    {
        if (_history.TryGetValue(processContactStateId, out var list))
        {
            lock (list)
            {
                return Task.FromResult<IEnumerable<ProcessStepHistory>>(list.ToList());
            }
        }
        return Task.FromResult<IEnumerable<ProcessStepHistory>>([]);
    }
}
