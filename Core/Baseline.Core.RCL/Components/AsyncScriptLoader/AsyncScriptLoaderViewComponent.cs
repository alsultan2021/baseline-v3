using Microsoft.AspNetCore.Mvc;

namespace Baseline.Core.RCL.Components.AsyncScriptLoader;

/// <summary>
/// View component for async script loading.
/// Placed at the end of the page to load scripts after content is rendered.
/// </summary>
public sealed class AsyncScriptLoaderViewComponent : ViewComponent
{
    /// <summary>
    /// Renders the async script loader.
    /// </summary>
    /// <param name="scriptRunnerPath">Path to the Script Runner JS file.
    /// The file should contain this JavaScript code:
    /// <code>
    /// window.ScriptsLoaded = true;
    /// for (var queuedScripts = window.PreloadQueue || [], i = 0; i &lt; queuedScripts.length; i++)
    ///     queuedScripts[i]();
    /// </code>
    /// </param>
    /// <returns>The rendered view component.</returns>
    public IViewComponentResult Invoke(string scriptRunnerPath)
    {
        var model = new AsyncScriptLoaderViewModel(scriptRunnerPath);
        return View("~/Components/AsyncScriptLoader/AsyncScriptLoader.cshtml", model);
    }
}

/// <summary>
/// View model for async script loader.
/// </summary>
public sealed record AsyncScriptLoaderViewModel(string ScriptRunnerPath);
