using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ATS.Application.Common.Interfaces;

namespace ATS.Infrastructure.Persistence;

public class AtsDbContext(DbContextOptions<AtsDbContext> options) : IdentityDbContext<IdentityUser>(options), IUnitOfWork
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AtsDbContext).Assembly);
    }
}
