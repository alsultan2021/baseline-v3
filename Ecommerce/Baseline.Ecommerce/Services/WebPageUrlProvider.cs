using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;

namespace Baseline.Ecommerce;

/// <summary>
/// V3 implementation of IWebPageUrlProvider for e-commerce page URLs.
/// </summary>
public class WebPageUrlProvider(
    IWebsiteChannelContext websiteChannelContext,
    IContentQueryExecutor contentQueryExecutor,
    IWebPageUrlRetriever webPageUrlRetriever,
    IPreferredLanguageRetriever preferredLanguageRetriever) : global::Ecommerce.Services.IWebPageUrlProvider
{
    // Page type code names - customize these for your project
    private const string StorePageType = "Generic.StorePage";
    private const string ShoppingCartPageType = "Generic.ShoppingCart";
    private const string CheckoutPageType = "Generic.Checkout";
    private const string LoginPageType = "Generic.LoginPage";
    private const string RegistrationPageType = "Generic.RegistrationPage";
    private const string ForgotPasswordPageType = "Generic.ForgotPasswordPage";
    private const string MyAccountPageType = "Generic.MyAccountPage";
    private const string OrderConfirmationPageType = "Generic.OrderConfirmationPage";

    public async Task<string> StorePageUrl(string? languageName = null, CancellationToken cancellationToken = default)
        => await GetPageUrlByTypeAsync(StorePageType, languageName, cancellationToken) ?? "/store";

    public async Task<string> ShoppingCartPageUrl(string? languageName = null, CancellationToken cancellationToken = default)
        => await GetPageUrlByTypeAsync(ShoppingCartPageType, languageName, cancellationToken) ?? "/shoppingcart";

    public async Task<string> CheckoutPageUrl(string? languageName = null, CancellationToken cancellationToken = default)
        => await GetPageUrlByTypeAsync(CheckoutPageType, languageName, cancellationToken) ?? "/checkout";

    public async Task<string> LoginPageUrl(string? languageName = null, CancellationToken cancellationToken = default)
        => await GetPageUrlByTypeAsync(LoginPageType, languageName, cancellationToken) ?? "/Account/LogIn";

    public async Task<string> RegisterPageUrl(string? languageName = null, CancellationToken cancellationToken = default)
        => await GetPageUrlByTypeAsync(RegistrationPageType, languageName, cancellationToken) ?? "/Account/Registration";

    public async Task<string> ForgotPasswordPageUrl(string? languageName = null, CancellationToken cancellationToken = default)
        => await GetPageUrlByTypeAsync(ForgotPasswordPageType, languageName, cancellationToken) ?? "/Account/ForgottenPassword";

    public async Task<string> MyAccountPageUrl(string? languageName = null, CancellationToken cancellationToken = default)
        => await GetPageUrlByTypeAsync(MyAccountPageType, languageName, cancellationToken) ?? "/Account/MyAccount";

    public async Task<string> OrderConfirmationPageUrl(string? languageName = null, CancellationToken cancellationToken = default)
        => await GetPageUrlByTypeAsync(OrderConfirmationPageType, languageName, cancellationToken) ?? "/order-confirmation";

    public Task<string> OrderDetailPageUrl(int orderId, string? languageName = null, CancellationToken cancellationToken = default)
        => Task.FromResult($"/my-account/orders/{orderId}");

    public Task<string> ProductPageUrl(int productId, string? languageName = null, CancellationToken cancellationToken = default)
        => Task.FromResult($"/products/{productId}");

    private async Task<string?> GetPageUrlByTypeAsync(string contentTypeName, string? languageName, CancellationToken cancellationToken)
    {
        try
        {
            var language = languageName ?? preferredLanguageRetriever.Get();
            var channelName = websiteChannelContext.WebsiteChannelName;

            var queryBuilder = new ContentItemQueryBuilder()
                .ForContentType(
                    contentTypeName,
                    q => q.ForWebsite(channelName).TopN(1))
                .InLanguage(language);

            var pages = await contentQueryExecutor.GetMappedWebPageResult<IWebPageFieldsSource>(queryBuilder, cancellationToken: cancellationToken);
            var page = pages.FirstOrDefault();

            if (page == null) return null;

            var urlResult = await webPageUrlRetriever.Retrieve(page.SystemFields.WebPageItemID, language);
            return urlResult.RelativePath;
        }
        catch
        {
            return null;
        }
    }
}
