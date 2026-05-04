using MediatR;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Auth;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;
