using MediatR;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Admin;

/// <summary>Creates a platform <see cref="ZimMarket.Domain.Entities.Users.AdminUser"/> (not transactional with outbound email).</summary>
public sealed record CreateAdminCommand(string Email, string Password, string FullName) : IRequest<Result<Guid>>;
