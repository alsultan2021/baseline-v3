using CMS.Websites;

namespace Baseline.Core;

/// <summary>
/// Interface for ViewModels that are associated with a web page content item.
/// This provides a common contract for ViewModels that represent web pages.
/// </summary>
public interface IWebPageBasedViewModel
{
    /// <summary>
    /// The web page content item associated with this ViewModel.
    /// </summary>
    public IWebPageFieldsSource WebPage { get; init; }
}
