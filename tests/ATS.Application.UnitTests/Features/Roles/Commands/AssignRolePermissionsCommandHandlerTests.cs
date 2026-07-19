using ATS.Application.Common.Interfaces;
using ATS.Application.Common.Models;
using ATS.Domain.Common;
using ATS.Application.Features.Roles.Commands.AssignRolePermissions;
using ATS.Application.UnitTests.Features.Users.Commands;
using ATS.Domain.Aggregates.Users;
using ATS.Domain.Constants;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATS.Application.UnitTests.Features.Roles.Commands;

public class AssignRolePermissionsCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidPermissions_ShouldAssignThem()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<FakeApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "AssignPermissionsDb")
            .Options;
        var dbContext = new FakeApplicationDbContext(options);
        
        var role = new Role(Guid.NewGuid(), "Admin", "Admin Role", null);
        dbContext.DomainRoles.Add(role);
        await dbContext.SaveChangesAsync();

        var handler = new AssignRolePermissionsCommandHandler(dbContext);
        var command = new AssignRolePermissionsCommand(role.Id, new List<string> { Permissions.Users.Create });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        var updatedRole = await dbContext.DomainRoles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == role.Id);
            
        updatedRole!.RolePermissions.Should().ContainSingle(rp => rp.PermissionName == Permissions.Users.Create);
    }
}

