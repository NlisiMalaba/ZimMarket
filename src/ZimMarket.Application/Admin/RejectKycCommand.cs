using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Admin;

public sealed record RejectKycCommand(Guid UserId, UserRole Role, string Reason) : ICommand;
