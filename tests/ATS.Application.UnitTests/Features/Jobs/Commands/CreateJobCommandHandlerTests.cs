using ATS.Application.Features.Jobs.Commands.CreateJob;
using ATS.Domain.Aggregates.Companies;
using ATS.Domain.Aggregates.Jobs;
using ATS.Domain.Aggregates.Departments;
using ATS.Application.UnitTests.Features.Users.Commands;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.UnitTests.Features.Jobs.Commands;

public class CreateJobCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCompanyExists_ShouldCreateJob()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<FakeApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new FakeApplicationDbContext(options);
        var company = new Company(Guid.NewGuid(), "Acme Corp", "acme-corp");
        context.Companies.Add(company);
        await context.SaveChangesAsync();

        var handler = new CreateJobCommandHandler(context);
        var command = new CreateJobCommand(
            company.Id,
            "Software Engineer",
            "Job Description",
            EmploymentType.FullTime,
            WorkplaceType.Remote,
            ExperienceLevel.MidLevel,
            "Remote",
            50000,
            100000,
            "USD");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        var job = await context.Jobs.FindAsync(result.Value);
        job.Should().NotBeNull();
        job!.CompanyId.Should().Be(company.Id);
        job.Title.Should().Be("Software Engineer");
    }

    [Fact]
    public async Task Handle_WhenCompanyDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<FakeApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new FakeApplicationDbContext(options);
        var handler = new CreateJobCommandHandler(context);
        var command = new CreateJobCommand(
            Guid.NewGuid(),
            "Software Engineer",
            "Job Description",
            EmploymentType.FullTime,
            WorkplaceType.Remote,
            ExperienceLevel.MidLevel,
            "Remote",
            50000,
            100000,
            "USD");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Company not found or inactive.");
    }
}
