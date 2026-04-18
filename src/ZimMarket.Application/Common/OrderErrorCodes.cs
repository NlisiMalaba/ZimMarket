namespace ZimMarket.Application.Common;

public static class OrderErrorCodes
{
    public const string OrderForbidden = "ORDER_FORBIDDEN";
    public const string OrderNotFound = "ORDER_NOT_FOUND";
    public const string OrderCannotCancel = "ORDER_CANNOT_CANCEL";
    public const string ProductNotFound = "PRODUCT_NOT_FOUND";
    public const string ProductInactive = "PRODUCT_INACTIVE";
    public const string ProductOutOfStock = "PRODUCT_OUT_OF_STOCK";
    public const string ProductUnsupportedCurrency = "PRODUCT_UNSUPPORTED_CURRENCY";
    public const string OrderInvalidAddress = "ORDER_INVALID_ADDRESS";
    public const string OrderCreateFailed = "ORDER_CREATE_FAILED";
}
