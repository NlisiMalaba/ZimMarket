using MediatR;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Auth;

public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest<Result>;
