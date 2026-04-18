namespace ZimMarket.Application.Payments;

public static class PaymentErrorCodes
{
    public const string Unauthorized = "Payments.Unauthorized";

    public const string Forbidden = "Payments.Forbidden";

    public const string IdempotencyKeyConflict = "Payments.IdempotencyKeyConflict";

    public const string OrderNotFound = "Payments.OrderNotFound";

    public const string OrderNotPending = "Payments.OrderNotPending";

    public const string PaymentAlreadyInitiated = "Payments.PaymentAlreadyInitiated";

    public const string MethodNotSupported = "Payments.MethodNotSupported";

    public const string GatewayRejected = "Payments.GatewayRejected";

    public const string MissingCheckoutUrl = "Payments.MissingCheckoutUrl";

    public const string InvalidState = "Payments.InvalidState";

    public const string CustomerRoleRequired = "Payments.CustomerRoleRequired";

    public const string WebhookInvalidSignature = "Payments.WebhookInvalidSignature";

    public const string WebhookInvalidPayload = "Payments.WebhookInvalidPayload";

    public const string WebhookGatewayUnavailable = "Payments.WebhookGatewayUnavailable";

    public const string WebhookOrderNotFound = "Payments.WebhookOrderNotFound";

    public const string WebhookInvalidOrderState = "Payments.WebhookInvalidOrderState";

    public const string WebhookMissingProviderReference = "Payments.WebhookMissingProviderReference";
}
