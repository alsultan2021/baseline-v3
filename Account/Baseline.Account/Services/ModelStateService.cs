using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Baseline.Account;

/// <summary>
/// v3 implementation of IModelStateService for controller validation.
/// </summary>
public sealed class ModelStateService : IModelStateService
{
    private const string ViewModelKey = "__ModelStateServiceViewModel__";
    private readonly Dictionary<string, List<string>> _errors = [];

    /// <inheritdoc/>
    public bool IsValid => _errors.Count == 0;

    /// <inheritdoc/>
    public void AddError(string key, string message)
    {
        if (!_errors.TryGetValue(key, out var list))
        {
            list = [];
            _errors[key] = list;
        }
        list.Add(message);
    }

    /// <inheritdoc/>
    public void MergeErrors(IDictionary<string, string[]> errors)
    {
        foreach (var (key, messages) in errors)
        {
            foreach (var message in messages)
            {
                AddError(key, message);
            }
        }
    }

    /// <inheritdoc/>
    public void StoreViewModel<T>(ITempDataDictionary tempData, T viewModel)
    {
        var json = JsonSerializer.Serialize(viewModel);
        tempData[ViewModelKey] = json;
    }

    /// <inheritdoc/>
    public T? RetrieveViewModel<T>(ITempDataDictionary tempData) where T : class
    {
        if (tempData.TryGetValue(ViewModelKey, out var value) && value is string json)
        {
            tempData.Remove(ViewModelKey);
            return JsonSerializer.Deserialize<T>(json);
        }
        return null;
    }

    /// <inheritdoc/>
    public void ClearViewModel(ITempDataDictionary tempData)
    {
        tempData.Remove(ViewModelKey);
    }
}
