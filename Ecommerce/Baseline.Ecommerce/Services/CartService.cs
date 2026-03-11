using Baseline.Ecommerce.Models;
using CMS.Commerce;
using CMS.ContentEngine;
using CMS.DataEngine;
using Ecommerce.Extensions;
using Ecommerce.Models;
using Ecommerce.Services;
using Kentico.Commerce.Web.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Ecommerce;

/// <summary>
/// Default implementation of ICartService using Kentico Commerce APIs.
/// Integrates with ICurrentShoppingCartRetriever for session-based cart management.
/// </summary>
public class CartService(
    ICurrentShoppingCartRetriever currentShoppingCartRetriever,
    ICurrentShoppingCartCreator currentShoppingCartCreator,
    ICurrentShoppingCartDiscardHandler currentShoppingCartDiscardHandler,
    IInfoProvider<ShoppingCartInfo> shoppingCartInfoProvider,
    IProductRepository productRepository,
    ICouponService couponService,
    IGiftCardService giftCardService,
    IPricingService pricingService,
    IOptions<BaselineEcommerceOptions> ecommerceOptions,
    ILogger<CartService> logger) : ICartService
{
    // Applied discount codes are stored in the cart's Discounts collection
    private const string DiscountSessionKey = "Cart_AppliedDiscounts";

    /// <inheritdoc/>
    public async Task<Cart> GetCartAsync()
    {
        logger.LogDebug("Getting cart from Kentico Commerce");

        var shoppingCart = await currentShoppingCartRetriever.Get();
        if (shoppingCart == null)
        {
            return CreateEmptyCart();
        }

        return await MapToCartAsync(shoppingCart);
    }

    /// <inheritdoc/>
    public async Task<ShoppingCartInfo?> GetNativeCartAsync()
    {
        logger.LogDebug("Getting native ShoppingCartInfo from Kentico Commerce");
        return await currentShoppingCartRetriever.Get();
    }

    /// <inheritdoc/>
    public async Task<bool> HasItemsAsync()
    {
        var shoppingCart = await currentShoppingCartRetriever.Get();
        if (shoppingCart == null)
        {
            return false;
        }
        var cartData = shoppingCart.GetShoppingCartDataModel();
        return cartData.Items.Count > 0;
    }

    /// <inheritdoc/>
    public async Task<CartResult> AddItemAsync(AddToCartRequest request)
    {
        logger.LogDebug("Adding item to cart: ProductId={ProductId}, Quantity={Quantity}", request.ProductId, request.Quantity);

        try
        {
            var shoppingCart = await currentShoppingCartRetriever.Get()
                              ?? await currentShoppingCartCreator.Create();

            var cartData = shoppingCart.GetShoppingCartDataModel();

            // Parse variant from options if provided
            int? variantId = null;
            if (request.Options?.TryGetValue("VariantId", out var variantStr) == true && int.TryParse(variantStr, out var parsedVariant))
            {
                variantId = parsedVariant;
            }

            // Check if this is a gift card (special handling - don't merge, each is unique)
            var isGiftCard = request.Options?.ContainsKey("IsGiftCard") == true;

            if (!isGiftCard)
            {
                var existingItem = cartData.Items.FirstOrDefault(x => x.ContentItemId == request.ProductId && x.VariantId == variantId);

                if (existingItem != null)
                {
                    existingItem.Quantity += request.Quantity;
                }
                else
                {
                    cartData.Items.Add(new ShoppingCartDataItem
                    {
                        ContentItemId = request.ProductId,
                        Quantity = request.Quantity,
                        VariantId = variantId,
                        Options = request.Options
                    });
                }
            }
            else
            {
                // Gift cards are always added as separate items (each is unique)
                cartData.Items.Add(new ShoppingCartDataItem
                {
                    ContentItemId = request.ProductId,
                    Quantity = request.Quantity,
                    VariantId = null,
                    Options = request.Options
                });
            }

            shoppingCart.StoreShoppingCartDataModel(cartData);
            await shoppingCartInfoProvider.SetAsync(shoppingCart);

            var cart = await MapToCartAsync(shoppingCart);
            return CartResult.Succeeded(cart);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add item to cart: ProductId={ProductId}", request.ProductId);
            return CartResult.Failed($"Failed to add item to cart: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<CartResult> UpdateQuantityAsync(Guid cartItemId, int quantity)
    {
        logger.LogDebug("Updating cart item quantity: CartItemId={CartItemId}, Quantity={Quantity}", cartItemId, quantity);

        try
        {
            var shoppingCart = await currentShoppingCartRetriever.Get();
            if (shoppingCart == null)
            {
                return CartResult.Failed("Cart not found");
            }

            var cartData = shoppingCart.GetShoppingCartDataModel();

            // Find item by matching the generated cart item ID (ContentItemId + VariantId hash)
            var itemToUpdate = FindItemByCartItemId(cartData, cartItemId);

            if (itemToUpdate == null)
            {
                return CartResult.Failed("Cart item not found");
            }

            if (quantity <= 0)
            {
                cartData.Items.Remove(itemToUpdate);
            }
            else
            {
                itemToUpdate.Quantity = quantity;
            }

            shoppingCart.StoreShoppingCartDataModel(cartData);
            await shoppingCartInfoProvider.SetAsync(shoppingCart);

            var cart = await MapToCartAsync(shoppingCart);
            return CartResult.Succeeded(cart);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update cart item: CartItemId={CartItemId}", cartItemId);
            return CartResult.Failed($"Failed to update cart item: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<CartResult> RemoveItemAsync(Guid cartItemId)
    {
        logger.LogDebug("Removing cart item: CartItemId={CartItemId}", cartItemId);

        try
        {
            var shoppingCart = await currentShoppingCartRetriever.Get();
            if (shoppingCart == null)
            {
                return CartResult.Failed("Cart not found");
            }

            var cartData = shoppingCart.GetShoppingCartDataModel();
            var itemToRemove = FindItemByCartItemId(cartData, cartItemId);

            if (itemToRemove == null)
            {
                return CartResult.Failed("Cart item not found");
            }

            cartData.Items.Remove(itemToRemove);

            // Clear discounts/coupons when cart becomes empty
            if (cartData.Items.Count == 0)
            {
                cartData.AppliedDiscounts.Clear();
                cartData.CouponCodes.Clear();
            }

            shoppingCart.StoreShoppingCartDataModel(cartData);
            await shoppingCartInfoProvider.SetAsync(shoppingCart);

            var cart = await MapToCartAsync(shoppingCart);
            return CartResult.Succeeded(cart);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove cart item: CartItemId={CartItemId}", cartItemId);
            return CartResult.Failed($"Failed to remove cart item: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<CartResult> ClearCartAsync()
    {
        logger.LogDebug("Clearing cart");

        try
        {
            await currentShoppingCartDiscardHandler.Discard();
            return CartResult.Succeeded(CreateEmptyCart());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to clear cart");
            return CartResult.Failed($"Failed to clear cart: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<DiscountResult> ApplyDiscountAsync(string code)
    {

        if (string.IsNullOrWhiteSpace(code))
        {
            return DiscountResult.Failed("Please enter a coupon or gift card code.");
        }

        try
        {
            var normalizedCode = code.Trim().ToUpperInvariant();

            // Get current cart to calculate discount
            var cart = await GetCartAsync();
            if (cart.IsEmpty)
            {
                return DiscountResult.Failed("Your cart is empty.");
            }

            // Check if already applied
            if (cart.Discounts.Any(d => string.Equals(d.Code, normalizedCode, StringComparison.OrdinalIgnoreCase)))
            {
                return DiscountResult.Failed("This code has already been applied.");
            }

            // First, try to validate as a coupon
            var couponValidation = await couponService.ValidateAsync(normalizedCode);
            DiscountResult result;

            if (couponValidation.IsValid)
            {
                // It's a valid coupon - apply coupon discount
                result = await ApplyCouponDiscountAsync(normalizedCode, couponValidation, cart);
            }
            else
            {
                // If not a valid coupon, try as a gift card
                var giftCardValidation = await giftCardService.ValidateCodeAsync(normalizedCode);

                if (giftCardValidation.IsValid && giftCardValidation.GiftCard is not null)
                {
                    // It's a valid gift card - apply gift card as discount
                    result = await ApplyGiftCardDiscountAsync(normalizedCode, giftCardValidation, cart);
                }
                else
                {
                    // Try as a Kentico DC coupon code (order discount from admin)
                    var kenticoDcResult = await TryApplyKenticoDcCouponAsync(normalizedCode, cart);

                    if (kenticoDcResult is not null && kenticoDcResult.Success)
                    {
                        result = kenticoDcResult;
                    }
                    else
                    {
                        // Use Kentico DC error if it recognized the code but conditions weren't met
                        var errorMessage = kenticoDcResult?.ErrorMessage
                            ?? couponValidation.ErrorMessage
                            ?? giftCardValidation.ErrorMessage
                            ?? "Invalid code.";
                        return DiscountResult.Failed(errorMessage);
                    }
                }
            }

            // Persist the discount to the native cart
            if (result.Success)
            {
                await PersistDiscountsAsync(cart);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply discount code: {Code}", code);
            return DiscountResult.Failed($"Failed to apply discount: {ex.Message}");
        }
    }

    /// <summary>
    /// Applies a validated coupon as a discount to the cart.
    /// </summary>
    private Task<DiscountResult> ApplyCouponDiscountAsync(string normalizedCode, PromotionCouponValidationResult validationResult, Cart cart)
    {
        decimal discountAmount = 0m;
        string description = string.Empty;
        DiscountType discountType = DiscountType.FixedAmount;

        if (validationResult.OrderPromotion is not null)
        {
            var promo = validationResult.OrderPromotion;
            discountAmount = promo.DiscountType switch
            {
                PromotionDiscountType.Percentage => cart.Totals.Subtotal.Amount * (promo.DiscountValue / 100m),
                PromotionDiscountType.FixedAmount => Math.Min(promo.DiscountValue, cart.Totals.Subtotal.Amount),
                _ => 0m
            };
            description = promo.PromotionDisplayName ?? promo.PromotionName;
            discountType = promo.DiscountType == PromotionDiscountType.Percentage
                ? DiscountType.Percentage
                : DiscountType.FixedAmount;
        }
        else if (validationResult.CatalogPromotion is not null)
        {
            var promo = validationResult.CatalogPromotion;
            discountAmount = promo.DiscountType switch
            {
                PromotionDiscountType.Percentage => cart.Totals.Subtotal.Amount * (promo.DiscountValue / 100m),
                PromotionDiscountType.FixedAmount => Math.Min(promo.DiscountValue, cart.Totals.Subtotal.Amount),
                _ => 0m
            };
            description = promo.PromotionDisplayName ?? promo.PromotionName;
            discountType = promo.DiscountType == PromotionDiscountType.Percentage
                ? DiscountType.Percentage
                : DiscountType.FixedAmount;
        }

        var appliedDiscount = new AppliedDiscount
        {
            Code = normalizedCode,
            Description = description,
            Amount = new Money { Amount = discountAmount, Currency = cart.Totals.Subtotal.Currency },
            Type = discountType
        };

        // Add discount to cart
        cart.Discounts.Add(appliedDiscount);

        // Recalculate totals (sum all discounts)
        var totalDiscount = cart.Discounts.Sum(d => d.Amount.Amount);
        cart.Totals.Discount = new Money { Amount = totalDiscount, Currency = cart.Totals.Subtotal.Currency };
        cart.Totals.Total = cart.Totals.Subtotal - cart.Totals.Discount;

        return Task.FromResult(DiscountResult.Succeeded(appliedDiscount, cart));
    }

    /// <summary>
    /// Applies a validated gift card as a discount to the cart.
    /// </summary>
    private Task<DiscountResult> ApplyGiftCardDiscountAsync(string normalizedCode, GiftCardValidationResult validationResult, Cart cart)
    {
        var giftCard = validationResult.GiftCard!;

        // Gift card balance is applied as a fixed amount discount (up to cart total)
        decimal discountAmount = Math.Min(giftCard.GiftCardRemainingBalance, cart.Totals.Subtotal.Amount);
        string description = $"Gift Card ({normalizedCode})";

        var appliedDiscount = new AppliedDiscount
        {
            Code = normalizedCode,
            Description = description,
            Amount = new Money { Amount = discountAmount, Currency = cart.Totals.Subtotal.Currency },
            Type = DiscountType.GiftCard
        };

        // Add discount to cart
        cart.Discounts.Add(appliedDiscount);

        // Recalculate totals (sum all discounts)
        var totalDiscount = cart.Discounts.Sum(d => d.Amount.Amount);
        cart.Totals.Discount = new Money { Amount = totalDiscount, Currency = cart.Totals.Subtotal.Currency };
        cart.Totals.Total = cart.Totals.Subtotal - cart.Totals.Discount;

        return Task.FromResult(DiscountResult.Succeeded(appliedDiscount, cart));
    }

    /// <summary>
    /// Validates a code against the Kentico DC Commerce_PromotionCoupon table,
    /// then runs a trial pricing calculation to verify the promotion actually
    /// applies to the current cart. Rejects the code if no discount results.
    /// </summary>
    private async Task<DiscountResult?> TryApplyKenticoDcCouponAsync(string normalizedCode, Cart cart)
    {
        try
        {
            // Step 1: Verify code exists and promotion is active
            var query = new DataQuery()
                .From(new QuerySource("Commerce_PromotionCoupon")
                    .InnerJoin("Commerce_Promotion", "PromotionCouponPromotionID", "PromotionID"))
                .WhereEquals("PromotionCouponCode", normalizedCode)
                .Where("PromotionActiveFromWhen", QueryOperator.LessOrEquals, DateTime.UtcNow)
                .Where(w => w
                    .WhereNull("PromotionActiveToWhen")
                    .Or()
                    .Where("PromotionActiveToWhen", QueryOperator.GreaterOrEquals, DateTime.UtcNow))
                .TopN(1)
                .Columns("PromotionDisplayName", "PromotionCouponPromotionID");

            var ds = query.Result;
            if (ds?.Tables.Count == 0 || ds!.Tables[0].Rows.Count == 0)
            {
                return null; // Code not found or promotion inactive
            }

            var row = ds.Tables[0].Rows[0];
            var promotionName = row["PromotionDisplayName"]?.ToString() ?? "Order Discount";

            // Step 2: Trial pricing — run with and without the coupon to check if it produces a discount
            var existingCouponCodes = cart.Discounts
                .Where(d => d.Type is DiscountType.KenticoDcCoupon
                    or DiscountType.Percentage
                    or DiscountType.FixedAmount)
                .Select(d => d.Code)
                .ToList();

            var items = cart.Items.Select(item => new PriceCalculationRequestItem
            {
                ProductIdentifier = item.ProductId.ToString(),
                Quantity = item.Quantity,
                OverridePrice = item.UnitPrice.Amount
            }).ToList();

            // Calculate without the new code
            var baseResult = await pricingService.CalculateAsync(new PriceCalculationRequest
            {
                Mode = PriceCalculationMode.ShoppingCart,
                CouponCodes = existingCouponCodes,
                Items = items
            });

            // Calculate with the new code
            var withCodeCoupons = new List<string>(existingCouponCodes) { normalizedCode };
            var trialResult = await pricingService.CalculateAsync(new PriceCalculationRequest
            {
                Mode = PriceCalculationMode.ShoppingCart,
                CouponCodes = withCodeCoupons,
                Items = items
            });

            var baseDiscount = baseResult.TotalOrderDiscount + baseResult.TotalCatalogDiscount;
            var trialDiscount = trialResult.TotalOrderDiscount + trialResult.TotalCatalogDiscount;
            var additionalDiscount = trialDiscount - baseDiscount;

            if (additionalDiscount <= 0)
            {
                logger.LogDebug(
                    "Kentico DC coupon {Code} is valid but promotion '{Name}' does not apply to current cart",
                    normalizedCode, promotionName);
                return DiscountResult.Failed(
                    $"This promotion does not apply to your current cart.");
            }

            logger.LogDebug(
                "Kentico DC coupon {Code} validated — promotion '{Name}' adds {Amount:C} discount",
                normalizedCode, promotionName, additionalDiscount);

            var appliedDiscount = new AppliedDiscount
            {
                Code = normalizedCode,
                Description = promotionName,
                Amount = new Money { Amount = additionalDiscount, Currency = cart.Totals.Subtotal.Currency },
                Type = DiscountType.KenticoDcCoupon
            };

            cart.Discounts.Add(appliedDiscount);
            return DiscountResult.Succeeded(appliedDiscount, cart);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to validate code {Code} against Kentico DC promotions", normalizedCode);
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<CartResult> RemoveDiscountAsync(string code)
    {

        try
        {
            var cart = await GetCartAsync();

            var discountToRemove = cart.Discounts.FirstOrDefault(d =>
                string.Equals(d.Code, code, StringComparison.OrdinalIgnoreCase));

            if (discountToRemove != null)
            {
                cart.Discounts.Remove(discountToRemove);

                // Recalculate totals
                var totalDiscount = cart.Discounts.Sum(d => d.Amount.Amount);
                cart.Totals.Discount = new Money { Amount = totalDiscount, Currency = cart.Totals.Subtotal.Currency };
                cart.Totals.Total = cart.Totals.Subtotal - cart.Totals.Discount;

                // Persist the updated discounts
                await PersistDiscountsAsync(cart);
            }

            return CartResult.Succeeded(cart);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove discount code: {Code}", code);
            return CartResult.Failed($"Failed to remove discount: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<int> GetItemCountAsync()
    {
        logger.LogDebug("Getting cart item count");

        var shoppingCart = await currentShoppingCartRetriever.Get();
        if (shoppingCart == null)
        {
            return 0;
        }

        var cartData = shoppingCart.GetShoppingCartDataModel();
        return cartData.GetItemCount();
    }

    /// <inheritdoc/>
    public async Task<CartValidationResult> ValidateCartAsync()
    {
        logger.LogDebug("Validating cart");

        var errors = new List<CartValidationError>();
        var shoppingCart = await currentShoppingCartRetriever.Get();

        if (shoppingCart == null)
        {
            return CartValidationResult.Valid();
        }

        var cartData = shoppingCart.GetShoppingCartDataModel();

        if (cartData.Items.Count == 0)
        {
            return CartValidationResult.Valid();
        }

        // Validate all products exist
        var productIds = cartData.Items.Select(i => i.ContentItemId).Distinct();
        var products = await productRepository.GetProductsByIdsAsync(productIds);
        var existingProductIds = products.OfType<object>().Select(GetContentItemId).ToHashSet();

        foreach (var item in cartData.Items)
        {
            if (!existingProductIds.Contains(item.ContentItemId))
            {
                var cartItemId = GenerateCartItemId(item);
                errors.Add(new CartValidationError(cartItemId, "PRODUCT_NOT_FOUND", $"Product {item.ContentItemId} no longer exists"));
            }

            if (item.Quantity <= 0)
            {
                var cartItemId = GenerateCartItemId(item);
                errors.Add(new CartValidationError(cartItemId, "INVALID_QUANTITY", "Quantity must be greater than zero"));
            }
        }

        return errors.Count > 0
            ? CartValidationResult.Invalid(errors)
            : CartValidationResult.Valid();
    }

    #region Private Helpers

    /// <summary>
    /// Persists the applied discounts from the Cart to the ShoppingCartInfo data model.
    /// Also syncs coupon codes (baseline + Kentico DC) to CouponCodes for pricing pipeline.
    /// </summary>
    private async Task PersistDiscountsAsync(Cart cart)
    {
        var shoppingCart = await currentShoppingCartRetriever.Get();
        if (shoppingCart == null)
        {
            return;
        }

        var cartData = shoppingCart.GetShoppingCartDataModel();

        // Update the applied discounts in the cart data
        cartData.AppliedDiscounts.Clear();
        foreach (var discount in cart.Discounts)
        {
            cartData.AppliedDiscounts.Add(new AppliedDiscountData
            {
                Code = discount.Code,
                Description = discount.Description,
                Amount = discount.Amount.Amount,
                DiscountType = discount.Type.ToString()
            });
        }

        // Sync coupon codes — Kentico DC pipeline needs these in CouponCodes to activate
        // coupon-gated promotions during price calculation
        cartData.CouponCodes.Clear();
        foreach (var discount in cart.Discounts)
        {
            if (discount.Type is DiscountType.KenticoDcCoupon
                or DiscountType.Percentage
                or DiscountType.FixedAmount)
            {
                cartData.CouponCodes.Add(discount.Code);
            }
        }

        // Save back to the ShoppingCartInfo
        shoppingCart.StoreShoppingCartDataModel(cartData);
        shoppingCart.Update();
    }

    private static Cart CreateEmptyCart() => new()
    {
        Id = Guid.Empty,
        Items = [],
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private async Task<Cart> MapToCartAsync(ShoppingCartInfo shoppingCart)
    {
        var cartData = shoppingCart.GetShoppingCartDataModel();
        var currency = ecommerceOptions.Value.Pricing.DefaultCurrency;

        if (cartData.Items.Count == 0)
        {
            return CreateEmptyCart();
        }

        // Fetch product data for all items (exclude gift cards which have ContentItemId = -1)
        var productIds = cartData.Items
            .Where(i => i.ContentItemId > 0)
            .Select(i => i.ContentItemId)
            .Distinct();
        var products = await productRepository.GetProductsByIdsAsync(productIds);
        var productLookup = BuildProductLookup(products);

        var cartItems = new List<CartItem>();
        decimal subtotal = 0m;
        int giftCardIndex = 0;

        foreach (var item in cartData.Items)
        {
            // Handle gift card items (ContentItemId = -1)
            if (item.ContentItemId == -1 && item.Options?.ContainsKey("IsGiftCard") == true)
            {
                var amount = decimal.TryParse(item.Options.GetValueOrDefault("Amount", "0"), out var a) ? a : 0m;
                var recipientName = item.Options.GetValueOrDefault("RecipientName", "");
                var isGift = item.Options.GetValueOrDefault("IsGift", "false") == "true";
                var lineTotal = amount * item.Quantity;
                subtotal += lineTotal;

                var displayName = isGift && !string.IsNullOrWhiteSpace(recipientName)
                    ? $"Gift Card for {recipientName}"
                    : "Gift Card";

                cartItems.Add(new CartItem
                {
                    Id = GenerateGiftCardCartItemId(item, giftCardIndex++),
                    ProductId = -1,
                    ProductName = displayName,
                    Sku = "GIFT-CARD",
                    ImageUrl = null,
                    Quantity = item.Quantity,
                    UnitPrice = new Money { Amount = amount, Currency = currency },
                    LineTotal = new Money { Amount = lineTotal, Currency = currency },
                    Options = item.Options ?? []
                });
                continue;
            }

            if (!productLookup.TryGetValue(item.ContentItemId, out var productData))
            {
                continue;
            }

            var unitPrice = productData.Price;
            var lineTotal2 = unitPrice * item.Quantity;
            subtotal += lineTotal2;

            cartItems.Add(new CartItem
            {
                Id = GenerateCartItemId(item),
                ProductId = item.ContentItemId,
                ProductName = productData.Name,
                Sku = productData.Sku,
                ImageUrl = productData.ImageUrl,
                Quantity = item.Quantity,
                UnitPrice = new Money { Amount = unitPrice, Currency = currency },
                LineTotal = new Money { Amount = lineTotal2, Currency = currency },
                Options = item.VariantId.HasValue
                    ? new Dictionary<string, string> { ["VariantId"] = item.VariantId.Value.ToString() }
                    : []
            });
        }

        // Load applied discounts from persisted data
        var appliedDiscounts = new List<AppliedDiscount>();
        decimal totalDiscount = 0m;
        foreach (var discountData in cartData.AppliedDiscounts)
        {
            appliedDiscounts.Add(new AppliedDiscount
            {
                Code = discountData.Code,
                Description = discountData.Description,
                Amount = new Money { Amount = discountData.Amount, Currency = currency },
                Type = Enum.TryParse<DiscountType>(discountData.DiscountType, out var dt) ? dt : DiscountType.FixedAmount
            });
            totalDiscount += discountData.Amount;
        }

        return new Cart
        {
            Id = shoppingCart.ShoppingCartGUID,
            Items = cartItems,
            Discounts = appliedDiscounts,
            Totals = new CartTotals
            {
                Subtotal = new Money { Amount = subtotal, Currency = currency },
                Discount = new Money { Amount = totalDiscount, Currency = currency },
                Total = new Money { Amount = subtotal - totalDiscount, Currency = currency }
            },
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static Guid GenerateCartItemId(ShoppingCartDataItem item)
    {
        // Create a deterministic GUID from ContentItemId and VariantId
        var bytes = new byte[16];
        BitConverter.GetBytes(item.ContentItemId).CopyTo(bytes, 0);
        BitConverter.GetBytes(item.VariantId ?? 0).CopyTo(bytes, 4);
        return new Guid(bytes);
    }

    private static Guid GenerateGiftCardCartItemId(ShoppingCartDataItem item, int index)
    {
        // Create a unique GUID for gift cards using amount and index
        var amount = item.Options?.GetValueOrDefault("Amount", "0") ?? "0";
        var recipient = item.Options?.GetValueOrDefault("RecipientEmail", "") ?? "";
        var hash = (amount + recipient + index).GetHashCode();
        var bytes = new byte[16];
        BitConverter.GetBytes(-1).CopyTo(bytes, 0);
        BitConverter.GetBytes(hash).CopyTo(bytes, 4);
        BitConverter.GetBytes(index).CopyTo(bytes, 8);
        return new Guid(bytes);
    }

    private static ShoppingCartDataItem? FindItemByCartItemId(ShoppingCartDataModel cartData, Guid cartItemId)
    {
        foreach (var item in cartData.Items)
        {
            if (GenerateCartItemId(item) == cartItemId)
            {
                return item;
            }
        }
        return null;
    }

    private static Dictionary<int, ProductData> BuildProductLookup(IEnumerable<object> products)
    {
        var lookup = new Dictionary<int, ProductData>();

        foreach (var product in products)
        {
            // Prefer IContentItemFieldsSource + IProductFields interfaces over reflection
            if (product is IContentItemFieldsSource contentItem && product is IProductFields productFields)
            {
                var id = contentItem.SystemFields.ContentItemID;
                if (id <= 0) continue;

                lookup[id] = new ProductData
                {
                    Name = productFields.ProductFieldName,
                    Price = productFields.ProductFieldPrice,
                    Sku = GetProductSku(product),
                    ImageUrl = GetProductImageUrl(product)
                };
                continue;
            }

            // Fallback: reflection for non-standard product types
            var contentItemId = GetContentItemId(product);
            if (contentItemId <= 0) continue;

            lookup[contentItemId] = new ProductData
            {
                Name = GetProductName(product),
                Price = GetProductPrice(product),
                Sku = GetProductSku(product),
                ImageUrl = GetProductImageUrl(product)
            };
        }

        return lookup;
    }

    private static int GetContentItemId(object product)
    {
        if (product is IContentItemFieldsSource source)
        {
            return source.SystemFields.ContentItemID;
        }
        return 0;
    }

    private static string GetProductName(object product)
    {
        if (product is IProductFields pf)
            return pf.ProductFieldName;

        // Reflection fallback — product types should implement IProductFields
        var prop = product.GetType().GetProperty("ProductFieldName") ?? product.GetType().GetProperty("Name");
        return prop?.GetValue(product)?.ToString() ?? "Unknown Product";
    }

    private static decimal GetProductPrice(object product)
    {
        if (product is IProductFields pf)
            return pf.ProductFieldPrice;

        // Reflection fallback — product types should implement IProductFields
        var prop = product.GetType().GetProperty("ProductFieldPrice") ?? product.GetType().GetProperty("Price");
        return prop?.GetValue(product) is decimal price ? price : 0m;
    }

    private static string? GetProductSku(object product)
    {
        // Reflection fallback — product types should implement a reusable schema
        // with ProductSKUCode (e.g. Generic.IProductSKU generated from ProductSKU schema)
        var prop = product.GetType().GetProperty("ProductSKUCode") ?? product.GetType().GetProperty("Sku");
        return prop?.GetValue(product)?.ToString();
    }

    private static string? GetProductImageUrl(object product)
    {
        // Try ProductFieldImage property
        var prop = product.GetType().GetProperty("ProductFieldImage");
        if (prop?.GetValue(product) is IEnumerable<object> images)
        {
            var firstImage = images.FirstOrDefault();
            if (firstImage != null)
            {
                var assetProp = firstImage.GetType().GetProperty("ImageAsset");
                var asset = assetProp?.GetValue(firstImage);
                var urlProp = asset?.GetType().GetProperty("Url");
                return urlProp?.GetValue(asset)?.ToString();
            }
        }
        return null;
    }

    private sealed class ProductData
    {
        public required string Name { get; init; }
        public decimal Price { get; init; }
        public string? Sku { get; init; }
        public string? ImageUrl { get; init; }
    }

    #endregion
}
