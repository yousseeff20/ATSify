using Microsoft.EntityFrameworkCore;
using ATS.Domain.Aggregates.Users;

namespace ATS.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> DomainUsers { get; }
    DbSet<Role> DomainRoles { get; }
    DbSet<UserRole> DomainUserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    
    DbSet<ATS.Domain.Aggregates.Companies.Company> Companies { get; }
    DbSet<ATS.Domain.Aggregates.Departments.Department> Departments { get; }
    DbSet<ATS.Domain.Aggregates.Invitations.Invitation> Invitations { get; }
    DbSet<ATS.Domain.Aggregates.Jobs.Job> Jobs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
