using System.Text.Json;
using Baseline.Automation.Models;
using CMS.DataEngine;
using Microsoft.Extensions.Logging;

namespace Baseline.Automation.Services;

/// <summary>
/// Database-backed implementation of <see cref="IProcessRepository"/>
/// using <see cref="AutomationProcessInfo"/> Kentico Info class.
/// </summary>
internal sealed class DatabaseProcessRepository(
    IInfoProvider<AutomationProcessInfo> processProvider,
    ILogger<DatabaseProcessRepository> logger) : IProcessRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <inheritdoc/>
    public async Task<AutomationProcess?> GetByIdAsync(Guid processId)
    {
        var infos = await processProvider.Get()
            .WhereEquals(nameof(AutomationProcessInfo.AutomationProcessGuid), processId)
            .GetEnumerableTypedResultAsync();

        var info = infos.FirstOrDefault();
        return info is not null ? MapToDomain(info) : null;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AutomationProcess>> GetAllAsync(bool? enabledOnly = null)
    {
        var query = processProvider.Get();

        if (enabledOnly.HasValue)
        {
            query = query.WhereEquals(nameof(AutomationProcessInfo.AutomationProcessIsEnabled), enabledOnly.Value);
        }

        var infos = await query.GetEnumerableTypedResultAsync();
        return infos.Select(MapToDomain).ToList();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AutomationProcess>> GetByTriggerTypeAsync(AutomationTriggerType triggerType)
    {
        var all = await GetAllAsync(enabledOnly: true);
        return all.Where(p => p.Trigger.TriggerType == triggerType).ToList();
    }

    /// <inheritdoc/>
    public async Task<AutomationProcess> SaveAsync(AutomationProcess process)
    {
        var infos = await processProvider.Get()
            .WhereEquals(nameof(AutomationProcessInfo.AutomationProcessGuid), process.Id)
            .GetEnumerableTypedResultAsync();

        var info = infos.FirstOrDefault() ?? new AutomationProcessInfo();
        MapToInfo(process, info);
        await processProvider.SetAsync(info);

        logger.LogDebug("Saved automation process {ProcessId} ({Name})", process.Id, process.Name);
        return process;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid processId)
    {
        var infos = await processProvider.Get()
            .WhereEquals(nameof(AutomationProcessInfo.AutomationProcessGuid), processId)
            .GetEnumerableTypedResultAsync();

        var info = infos.FirstOrDefault();
        if (info is not null)
        {
            processProvider.Delete(info);
            logger.LogInformation("Deleted automation process {ProcessId}", processId);
        }
    }

    private static AutomationProcess MapToDomain(AutomationProcessInfo info)
    {
        AutomationTrigger trigger;
        try
        {
            trigger = JsonSerializer.Deserialize<AutomationTrigger>(info.AutomationProcessTriggerJson, JsonOptions)
                ?? new AutomationTrigger { TriggerType = AutomationTriggerType.Webhook, Name = "Unknown" };
        }
        catch
        {
            trigger = new AutomationTrigger { TriggerType = AutomationTriggerType.Webhook, Name = "Unknown" };
        }

        List<AutomationStep> steps;
        try
        {
            steps = JsonSerializer.Deserialize<List<AutomationStep>>(info.AutomationProcessStepsJson, JsonOptions) ?? [];
        }
        catch
        {
            steps = [];
        }

        return new AutomationProcess
        {
            Id = info.AutomationProcessGuid,
            Name = info.AutomationProcessDisplayName,
            Description = info.AutomationProcessDescription,
            IsEnabled = info.AutomationProcessIsEnabled,
            Recurrence = Enum.TryParse<ProcessRecurrence>(info.AutomationProcessRecurrence, out var rec) ? rec : ProcessRecurrence.IfNotAlreadyRunning,
            Trigger = trigger,
            Steps = steps,
            CreatedAt = info.AutomationProcessCreatedWhen
        };
    }

    private static void MapToInfo(AutomationProcess process, AutomationProcessInfo info)
    {
        info.AutomationProcessGuid = process.Id;
        info.AutomationProcessName = process.Name.Replace(" ", "").Replace("-", "");
        info.AutomationProcessDisplayName = process.Name;
        info.AutomationProcessDescription = process.Description ?? string.Empty;
        info.AutomationProcessIsEnabled = process.IsEnabled;
        info.AutomationProcessRecurrence = process.Recurrence.ToString();
        info.AutomationProcessTriggerJson = JsonSerializer.Serialize(process.Trigger, JsonOptions);
        info.AutomationProcessStepsJson = JsonSerializer.Serialize(process.Steps, JsonOptions);
        info.AutomationProcessCreatedWhen = process.CreatedAt.DateTime;
        info.AutomationProcessLastModified = DateTime.UtcNow;
    }
}

/// <summary>
/// Database-backed implementation of <see cref="IProcessStateRepository"/>.
/// </summary>
internal sealed class DatabaseProcessStateRepository(
    IInfoProvider<AutomationProcessContactStateInfo> stateProvider,
    IInfoProvider<AutomationStepHistoryInfo> historyProvider,
    ILogger<DatabaseProcessStateRepository> logger) : IProcessStateRepository
{
    /// <inheritdoc/>
    public async Task<ProcessContactState> CreateAsync(ProcessContactState state)
    {
        var info = new AutomationProcessContactStateInfo();
        MapToInfo(state, info);
        await stateProvider.SetAsync(info);

        state = state with { Id = info.AutomationProcessContactStateGuid };
        logger.LogDebug("Created contact state {StateId} for contact {ContactId} in process {ProcessId}",
            state.Id, state.ContactId, state.ProcessId);
        return state;
    }

    /// <inheritdoc/>
    public async Task<ProcessContactState> UpdateAsync(ProcessContactState state)
    {
        var infos = await stateProvider.Get()
            .WhereEquals(nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateGuid), state.Id)
            .GetEnumerableTypedResultAsync();

        var info = infos.FirstOrDefault();
        if (info is null)
        {
            return await CreateAsync(state);
        }

        MapToInfo(state, info);
        await stateProvider.SetAsync(info);
        return state;
    }

    /// <inheritdoc/>
    public async Task<ProcessContactState?> GetByIdAsync(Guid id)
    {
        var infos = await stateProvider.Get()
            .WhereEquals(nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateGuid), id)
            .GetEnumerableTypedResultAsync();

        var info = infos.FirstOrDefault();
        return info is not null ? MapToDomain(info) : null;
    }

    /// <inheritdoc/>
    public async Task<ProcessContactState?> GetByProcessAndContactAsync(Guid processId, int contactId)
    {
        var infos = await stateProvider.Get()
            .WhereEquals(nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateProcessGuid), processId)
            .WhereEquals(nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateContactID), contactId)
            .WhereNotEquals(nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateStatus), ProcessContactStatus.Completed.ToString())
            .WhereNotEquals(nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateStatus), ProcessContactStatus.Removed.ToString())
            .GetEnumerableTypedResultAsync();

        var info = infos.FirstOrDefault();
        return info is not null ? MapToDomain(info) : null;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ProcessContactState>> GetActiveByProcessAsync(Guid processId)
    {
        var infos = await stateProvider.Get()
            .WhereEquals(nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateProcessGuid), processId)
            .WhereNotEquals(nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateStatus), ProcessContactStatus.Completed.ToString())
            .WhereNotEquals(nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateStatus), ProcessContactStatus.Removed.ToString())
            .GetEnumerableTypedResultAsync();

        return infos.Select(MapToDomain).ToList();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ProcessContactState>> GetExpiredWaitingStatesAsync()
    {
        var infos = await stateProvider.Get()
            .WhereEquals(nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateStatus), ProcessContactStatus.Waiting.ToString())
            .WhereLessOrEquals(nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateWaitUntil), DateTime.UtcNow)
            .GetEnumerableTypedResultAsync();

        return infos.Select(MapToDomain).ToList();
    }

    /// <inheritdoc/>
    public async Task<int> GetExecutionCountAsync(Guid processId, int contactId)
    {
        var infos = await stateProvider.Get()
            .WhereEquals(nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateProcessGuid), processId)
            .WhereEquals(nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateContactID), contactId)
            .GetEnumerableTypedResultAsync();

        return infos.Count();
    }

    /// <inheritdoc/>
    public async Task AddStepHistoryAsync(ProcessStepHistory history)
    {
        var info = new AutomationStepHistoryInfo
        {
            AutomationStepHistoryGuid = history.Id,
            AutomationStepHistoryContactStateGuid = history.ProcessContactStateId,
            AutomationStepHistoryStepGuid = history.StepId,
            AutomationStepHistoryStepName = history.StepType.ToString(),
            AutomationStepHistoryStepType = history.StepType.ToString(),
            AutomationStepHistorySuccess = history.Success,
            AutomationStepHistoryErrorMessage = history.ResultDetails ?? string.Empty,
            AutomationStepHistoryExecutedAt = history.EnteredAt.DateTime,
            AutomationStepHistoryCompletedAt = history.CompletedAt?.DateTime ?? DateTime.MinValue
        };

        var stateInfos = await stateProvider.Get()
            .WhereEquals(nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateGuid), history.ProcessContactStateId)
            .GetEnumerableTypedResultAsync();

        var stateInfo = stateInfos.FirstOrDefault();
        if (stateInfo is not null)
        {
            info.AutomationStepHistoryContactStateID = stateInfo.AutomationProcessContactStateID;
        }

        await historyProvider.SetAsync(info);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ProcessStepHistory>> GetStepHistoryAsync(Guid processContactStateId)
    {
        var stateInfos = await stateProvider.Get()
            .WhereEquals(nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateGuid), processContactStateId)
            .GetEnumerableTypedResultAsync();

        var stateInfo = stateInfos.FirstOrDefault();
        if (stateInfo is null)
        {
            return [];
        }

        var histories = await historyProvider.Get()
            .WhereEquals(nameof(AutomationStepHistoryInfo.AutomationStepHistoryContactStateID), stateInfo.AutomationProcessContactStateID)
            .OrderBy(nameof(AutomationStepHistoryInfo.AutomationStepHistoryExecutedAt))
            .GetEnumerableTypedResultAsync();

        return histories.Select(h => new ProcessStepHistory
        {
            Id = h.AutomationStepHistoryGuid,
            ProcessContactStateId = processContactStateId,
            StepId = h.AutomationStepHistoryStepGuid,
            StepType = Enum.TryParse<AutomationStepType>(h.AutomationStepHistoryStepType, out var st) ? st : AutomationStepType.Finish,
            Success = h.AutomationStepHistorySuccess,
            ResultDetails = string.IsNullOrEmpty(h.AutomationStepHistoryErrorMessage) ? null : h.AutomationStepHistoryErrorMessage,
            EnteredAt = h.AutomationStepHistoryExecutedAt,
            CompletedAt = h.AutomationStepHistoryCompletedAt == DateTime.MinValue ? null : (DateTimeOffset)h.AutomationStepHistoryCompletedAt
        }).ToList();
    }

    private static ProcessContactState MapToDomain(AutomationProcessContactStateInfo info)
    {
        return new ProcessContactState
        {
            Id = info.AutomationProcessContactStateGuid,
            ProcessId = info.AutomationProcessContactStateProcessGuid,
            ContactId = info.AutomationProcessContactStateContactID,
            CurrentStepId = info.AutomationProcessContactStateCurrentStepGuid,
            Status = Enum.TryParse<ProcessContactStatus>(info.AutomationProcessContactStateStatus, out var status)
                ? status
                : ProcessContactStatus.Active,
            WaitUntil = info.AutomationProcessContactStateWaitUntil == DateTime.MinValue
                ? null
                : info.AutomationProcessContactStateWaitUntil,
            ContextData = string.IsNullOrEmpty(info.AutomationProcessContactStateTriggerData)
                ? null
                : info.AutomationProcessContactStateTriggerData,
            StepEnteredAt = info.AutomationProcessContactStateStepEnteredAt,
            StartedAt = info.AutomationProcessContactStateStartedAt,
            FinishedAt = info.AutomationProcessContactStateCompletedAt == DateTime.MinValue
                ? null
                : info.AutomationProcessContactStateCompletedAt
        };
    }

    private static void MapToInfo(ProcessContactState state, AutomationProcessContactStateInfo info)
    {
        info.AutomationProcessContactStateGuid = state.Id;
        info.AutomationProcessContactStateProcessGuid = state.ProcessId;
        info.AutomationProcessContactStateContactID = state.ContactId;
        info.AutomationProcessContactStateCurrentStepGuid = state.CurrentStepId;
        info.AutomationProcessContactStateStatus = state.Status.ToString();
        info.AutomationProcessContactStateWaitUntil = state.WaitUntil?.DateTime ?? DateTime.MinValue;
        info.AutomationProcessContactStateTriggerData = state.ContextData ?? string.Empty;
        info.AutomationProcessContactStateStepEnteredAt = state.StepEnteredAt.DateTime;
        info.AutomationProcessContactStateStartedAt = state.StartedAt.DateTime;
        info.AutomationProcessContactStateCompletedAt = state.FinishedAt?.DateTime ?? DateTime.MinValue;
        info.AutomationProcessContactStateLastModified = DateTime.UtcNow;
    }
}
