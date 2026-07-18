using Microsoft.EntityFrameworkCore;
using ATS.Domain.Aggregates.Users;

namespace ATS.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> DomainUsers { get; }
    DbSet<Role> DomainRoles { get; }
    DbSet<UserRole> DomainUserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
