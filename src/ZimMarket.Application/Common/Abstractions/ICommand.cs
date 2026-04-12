using MediatR;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Common.Abstractions;

public interface ICommand : IRequest<Result>, ICommandMarker;

public interface ICommand<T> : IRequest<Result<T>>, ICommandMarker;
