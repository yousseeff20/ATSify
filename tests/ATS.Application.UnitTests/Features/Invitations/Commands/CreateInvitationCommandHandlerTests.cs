using ATS.Application.Features.Invitations.Commands.CreateInvitation;
using ATS.Domain.Aggregates.Companies;
using ATS.Domain.Aggregates.Users;
using ATS.Domain.Aggregates.Invitations;
using ATS.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using ATS.Application.Common.Interfaces;
using ATS.Application.UnitTests.Features.Users.Commands;

namespace ATS.Application.UnitTests.Features.Invitations.Commands;

public class CreateInvitationCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateInvitation()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<FakeApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var context = new FakeApplicationDbContext(options);
        var timeProviderMock = new Mock<ITimeProvider>();
        timeProviderMock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

        var company = new Company(Guid.NewGuid(), "Company 1", "c1");
        var role = new Role(Guid.NewGuid(), "Admin", "Admin Role", company.Id);
        context.Companies.Add(company);
        context.DomainRoles.Add(role);
        await context.SaveChangesAsync();

        var handler = new CreateInvitationCommandHandler(context, timeProviderMock.Object);
        var command = new CreateInvitationCommand("test@example.com", company.Id, null, role.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var invitation = await context.Invitations.FindAsync(result.Value);
        invitation.Should().NotBeNull();
        invitation!.Email.Should().Be("test@example.com");
        invitation.Status.Should().Be(InvitationStatus.Pending);
        invitation.SecureToken.Should().NotBeNullOrEmpty();
    }
}
