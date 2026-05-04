using MediatR;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Auth;

public sealed record VerifyAdminEmailCommand(string Token) : IRequest<Result>;
