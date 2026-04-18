using Microsoft.Extensions.DependencyInjection;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Infrastructure.Payments;

public sealed class PaymentGatewayFactory(IServiceProvider serviceProvider) : IPaymentGatewayFactory
{
    /// <inheritdoc />
    public IPaymentGateway Create(PaymentMethod method) => method switch
    {
        PaymentMethod.Paynow => serviceProvider.GetRequiredKeyedService<IPaymentGateway>(PaymentGatewayKeys.Paynow),
        PaymentMethod.Ecocash => serviceProvider.GetRequiredKeyedService<IPaymentGateway>(PaymentGatewayKeys.Ecocash),
        _ => throw new NotSupportedException($"Payment method {method} is not supported.")
    };
}
