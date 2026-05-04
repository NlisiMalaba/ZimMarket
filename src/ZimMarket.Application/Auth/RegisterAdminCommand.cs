using MediatR;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Auth;

public sealed record RegisterAdminCommand(string Email, string Password, string FullName, string PhoneNumber) : IRequest<Result>;
