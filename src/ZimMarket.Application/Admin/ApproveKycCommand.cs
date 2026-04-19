using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Admin;

public sealed record ApproveKycCommand(Guid UserId, UserRole Role) : ICommand;
