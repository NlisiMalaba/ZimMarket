using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Common.Interfaces;

public interface IPaymentGatewayFactory
{
    /// <summary>Resolves the payment gateway registered for <paramref name="method"/> (keyed services).</summary>
    IPaymentGateway Create(PaymentMethod method);
}
