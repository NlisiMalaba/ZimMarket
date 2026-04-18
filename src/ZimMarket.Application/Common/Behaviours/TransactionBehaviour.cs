using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Common.Behaviours;

public sealed class TransactionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TransactionBehaviour<TRequest, TResponse>> _logger;

    public TransactionBehaviour(
        IServiceProvider serviceProvider,
        ILogger<TransactionBehaviour<TRequest, TResponse>> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICommandMarker)
            return await next();

        var unitOfWork = _serviceProvider.GetService<IUnitOfWork>();
        if (unitOfWork is null)
        {
            _logger.LogWarning(
                "IUnitOfWork is not registered; executing {RequestType} without a transaction",
                typeof(TRequest).Name);

            return await next();
        }

        return await unitOfWork.RunInTransactionAsync(() => next(), cancellationToken).ConfigureAwait(false);
    }
}
