namespace Baseline.Automation.Events;

/// <summary>
/// Delegate signature for automation event handlers with before/after semantics.
/// Maps to CMS.Automation.Internal.AutomationHandler (AdvancedHandler pattern).
/// </summary>
public class AutomationHandler
{
    private readonly List<Action<AutomationEventArgs>> _beforeHandlers = [];
    private readonly List<Action<AutomationEventArgs>> _afterHandlers = [];

    /// <summary>Registers a handler to run before the operation.</summary>
    public AutomationHandler Before(Action<AutomationEventArgs> handler)
    {
        _beforeHandlers.Add(handler);
        return this;
    }

    /// <summary>Registers a handler to run after the operation.</summary>
    public AutomationHandler After(Action<AutomationEventArgs> handler)
    {
        _afterHandlers.Add(handler);
        return this;
    }

    /// <summary>Executes all before handlers. Returns false if cancelled.</summary>
    public bool RaiseBefore(AutomationEventArgs args)
    {
        foreach (var handler in _beforeHandlers)
        {
            handler(args);
            if (args.Cancel)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>Executes all after handlers.</summary>
    public void RaiseAfter(AutomationEventArgs args)
    {
        foreach (var handler in _afterHandlers)
        {
            handler(args);
        }
    }

    /// <summary>Clears all registered handlers.</summary>
    public void Clear()
    {
        _beforeHandlers.Clear();
        _afterHandlers.Clear();
    }
}
