using MediatR;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Admin;

public sealed record ApproveAdminCommand(Guid AdminUserId) : IRequest<Result>;
