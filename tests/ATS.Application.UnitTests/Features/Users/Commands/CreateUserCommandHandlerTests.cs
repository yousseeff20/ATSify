using ATS.Application.Common.Interfaces;
using ATS.Application.Common.Models;
using ATS.Application.Features.Users.Commands.CreateUser;
using ATS.Domain.Aggregates.Users;
using ATS.Domain.Aggregates.Companies;
using ATS.Domain.Aggregates.Departments;
using ATS.Domain.Aggregates.Invitations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ATS.Application.UnitTests.Features.Users.Commands;

public class CreateUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnUserId()
    {
        // Arrange
        var identityServiceMock = new Mock<IIdentityService>();
        
        var options = new DbContextOptionsBuilder<FakeApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "CreateUserDb")
            .Options;
        var dbContext = new FakeApplicationDbContext(options);

        var userId = Guid.NewGuid();
        identityServiceMock.Setup(x => x.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Result.Success(), userId));

        var handler = new CreateUserCommandHandler(identityServiceMock.Object, dbContext);
        var command = new CreateUserCommand("John", "Doe", "john@doe.com", "Password123!", null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(userId);
        
        await dbContext.SaveChangesAsync();
        var userInDb = await dbContext.DomainUsers.FirstOrDefaultAsync(u => u.Id == userId);
        userInDb.Should().NotBeNull();
        userInDb!.Email.Should().Be("john@doe.com");
    }
}

public class FakeApplicationDbContext(DbContextOptions options) : DbContext(options), IApplicationDbContext
{
    public DbSet<User> DomainUsers => Set<User>();
    public DbSet<Role> DomainRoles => Set<Role>();
    public DbSet<UserRole> DomainUserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<ATS.Domain.Aggregates.Jobs.Job> Jobs => Set<ATS.Domain.Aggregates.Jobs.Job>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<ATS.Domain.Common.DomainEvent>();
        modelBuilder.Entity<UserRole>().HasKey(x => new { x.UserId, x.RoleId });
        modelBuilder.Entity<RolePermission>().HasKey(x => new { x.RoleId, x.PermissionName });
        modelBuilder.Entity<ATS.Domain.Aggregates.Jobs.Job>().OwnsOne(x => x.SalaryRange);
    }
}
