using Baseline.Ecommerce;

namespace Ecommerce.Services;

/// <summary>
/// IShoppingCartService → v3 ICartService
/// </summary>
public interface IShoppingCartService : ICartService { }

/// <summary>
/// IWebPageUrlProvider - provides URLs for e-commerce related pages.
/// </summary>
public interface IWebPageUrlProvider
{
    Task<string> StorePageUrl(string? languageName = null, CancellationToken cancellationToken = default);
    Task<string> ShoppingCartPageUrl(string? languageName = null, CancellationToken cancellationToken = default);
    Task<string> CheckoutPageUrl(string? languageName = null, CancellationToken cancellationToken = default);
    Task<string> LoginPageUrl(string? languageName = null, CancellationToken cancellationToken = default);
    Task<string> RegisterPageUrl(string? languageName = null, CancellationToken cancellationToken = default);
    Task<string> ForgotPasswordPageUrl(string? languageName = null, CancellationToken cancellationToken = default);
    Task<string> MyAccountPageUrl(string? languageName = null, CancellationToken cancellationToken = default);
    Task<string> OrderConfirmationPageUrl(string? languageName = null, CancellationToken cancellationToken = default);
    Task<string> OrderDetailPageUrl(int orderId, string? languageName = null, CancellationToken cancellationToken = default);
    Task<string> ProductPageUrl(int productId, string? languageName = null, CancellationToken cancellationToken = default);
}
