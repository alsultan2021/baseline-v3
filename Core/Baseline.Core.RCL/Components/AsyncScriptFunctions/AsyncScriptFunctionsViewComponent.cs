using Microsoft.AspNetCore.Mvc;

namespace Baseline.Core.RCL.Components.AsyncScriptFunctions;

/// <summary>
/// Adds the window.LoadScript and window.OnScriptsLoaded methods to the page.
/// </summary>
/// <remarks>
/// <para>
/// Use <c>window.LoadScript(options)</c> to load your JavaScript files.
/// </para>
/// <para>
/// Options:
/// <list type="bullet">
/// <item><description><c>src</c>: The URL of the JavaScript file. Use Html.AddFileVersionToPath(string) to add a File Version to the path.</description></item>
/// <item><description><c>header</c>: (optional) If the JavaScript should be placed in the header or footer.</description></item>
/// <item><description><c>crossorigin</c>: (optional) Cross-origin value if any.</description></item>
/// <item><description><c>appendAtEnd</c>: (optional) Scripts are loaded non-appended-at-end (in order added), then append-at-end (in order added). Use this if you want your script to load AFTER the normal scripts.</description></item>
/// </list>
/// </para>
/// <para>
/// Use <c>window.OnScriptsLoaded(function, identifier)</c> to run scripts once all JavaScript is loaded.
/// </para>
/// <para>
/// Options:
/// <list type="bullet">
/// <item><description><c>function</c>: The function to run (can use arrow function notation).</description></item>
/// <item><description><c>identifier</c>: (optional) If identifiers match, it will only run the logic once. Useful for widgets with initialization JS logic.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class AsyncScriptFunctionsViewComponent : ViewComponent
{
    /// <summary>
    /// Renders the async script functions.
    /// </summary>
    public IViewComponentResult Invoke() =>
        View("~/Components/AsyncScriptFunctions/AsyncScriptFunctions.cshtml");
}
