namespace ZimMarket.Domain.Enums;

/// <summary>Identifies which payment integration verified and parsed a webhook payload.</summary>
public enum PaymentGatewayType
{
    Paynow = 0,
    Ecocash = 1
}
