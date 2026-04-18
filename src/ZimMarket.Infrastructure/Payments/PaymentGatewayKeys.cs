namespace ZimMarket.Infrastructure.Payments;

/// <summary>Keyed service names for <see cref="IPaymentGateway"/> implementations.</summary>
public static class PaymentGatewayKeys
{
    public const string Paynow = "paynow";
    public const string Ecocash = "ecocash";
}
