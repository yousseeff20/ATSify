using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ATS.Application.Common.Interfaces;
using ATS.Domain.Aggregates.Users;
using ATS.Infrastructure.Identity;

namespace ATS.Infrastructure.Persistence;

public class AtsDbContext(DbContextOptions<AtsDbContext> options) : IdentityDbContext<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>(options), IUnitOfWork, IApplicationDbContext
{
    public DbSet<User> DomainUsers => Set<User>();
    public DbSet<Role> DomainRoles => Set<Role>();
    public DbSet<UserRole> DomainUserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<ATS.Domain.Aggregates.Companies.Company> Companies => Set<ATS.Domain.Aggregates.Companies.Company>();
    public DbSet<ATS.Domain.Aggregates.Departments.Department> Departments => Set<ATS.Domain.Aggregates.Departments.Department>();
    public DbSet<ATS.Domain.Aggregates.Invitations.Invitation> Invitations => Set<ATS.Domain.Aggregates.Invitations.Invitation>();
    public DbSet<ATS.Domain.Aggregates.Jobs.Job> Jobs => Set<ATS.Domain.Aggregates.Jobs.Job>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Ignore<ATS.Domain.Common.DomainEvent>();
        builder.ApplyConfigurationsFromAssembly(typeof(AtsDbContext).Assembly);
    }
}
