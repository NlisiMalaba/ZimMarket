using MediatR;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Common.Abstractions;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
