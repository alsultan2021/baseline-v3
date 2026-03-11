using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Baseline.Core.RCL.Components.ImportModelState;

/// <summary>
/// Imports model state from TempData for Page Template POST redirect pattern.
/// </summary>
/// <remarks>
/// If using PageTemplate View Component with a POST action, the Controller's POST should have the [ExportModelState] Attribute
/// which will cause it to store the ModelState in the TempData, then redirect to your Page Template URL.
/// This View component will then hydrate the ModelState from the TempData.
/// </remarks>
public sealed class ImportModelStateViewComponent : ViewComponent
{
    private const string ModelStateKey = "ModelState";

    /// <summary>
    /// Imports model state from TempData.
    /// </summary>
    public IViewComponentResult Invoke()
    {
        MergeModelState(ModelState, TempData);
        return Content(string.Empty);
    }

    private static void MergeModelState(ModelStateDictionary modelState, ITempDataDictionary tempData)
    {
        if (tempData.TryGetValue(ModelStateKey, out var storedState) && storedState is ModelStateDictionary exportedState)
        {
            modelState.Merge(exportedState);
            tempData.Remove(ModelStateKey);
        }
    }
}
