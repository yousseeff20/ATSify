using ATS.Application.Features.Companies.Commands.CreateCompany;
using ATS.Domain.Aggregates.Companies;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ATS.Application.UnitTests.Features.Users.Commands;

namespace ATS.Application.UnitTests.Features.Companies.Commands;

public class CreateCompanyCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateCompany()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<FakeApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var context = new FakeApplicationDbContext(options);
        var handler = new CreateCompanyCommandHandler(context);
        var command = new CreateCompanyCommand("Test Company", null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var company = await context.Companies.FindAsync(result.Value);
        company.Should().NotBeNull();
        company!.Name.Should().Be("Test Company");
        company.Slug.Should().Be("test-company");
    }

    [Fact]
    public async Task Handle_WithDuplicateName_ShouldReturnFailure()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<FakeApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var context = new FakeApplicationDbContext(options);
        context.Companies.Add(new Company(Guid.NewGuid(), "Duplicate Company", "duplicate-company"));
        await context.SaveChangesAsync();

        var handler = new CreateCompanyCommandHandler(context);
        var command = new CreateCompanyCommand("Duplicate Company", null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already exists");
    }
}
