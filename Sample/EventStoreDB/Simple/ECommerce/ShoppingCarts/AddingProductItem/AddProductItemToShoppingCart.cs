using ECommerce.Pricing.ProductPricing;
using ECommerce.ShoppingCarts.ProductItems;

namespace ECommerce.ShoppingCarts.AddingProductItem;

public record AddProductItemToShoppingCart(
    Guid ShoppingCartId,
    ProductItem ProductItem
)
{
    public static AddProductItemToShoppingCart From(Guid? cartId, ProductItem? productItem)
    {
        if (cartId == null || cartId == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(cartId));
        if (productItem == null)
            throw new ArgumentOutOfRangeException(nameof(productItem));

        return new AddProductItemToShoppingCart(cartId.Value, productItem);
    }

    public static ProductItemAddedToShoppingCart Handle(
        IProductPriceCalculator productPriceCalculator,
        AddProductItemToShoppingCart command,
        ShoppingCart shoppingCart
    )
    {
        var (cartId, productItem) = command;

        if (shoppingCart.IsClosed)
            throw new InvalidOperationException($"Adding product item for cart in '{shoppingCart.Status}' status is not allowed.");

        var pricedProductItem = productPriceCalculator.Calculate(productItem);

        shoppingCart.ProductItems.Add(pricedProductItem);

        return new ProductItemAddedToShoppingCart(
            cartId,
            pricedProductItem
        );
    }
}
