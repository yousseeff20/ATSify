using ATS.Domain.Aggregates.Jobs;
using ATS.Domain.Aggregates.Jobs.ValueObjects;
using ATS.Domain.Aggregates.Jobs.Events;
using FluentAssertions;

namespace ATS.Domain.UnitTests.Aggregates.Jobs;

public class JobTests
{
    private readonly Guid _companyId = Guid.NewGuid();
    private readonly SalaryRange _validSalaryRange = new(50000, 100000, "USD");

    [Fact]
    public void Create_WithValidData_ShouldCreateJobAndRaiseEvent()
    {
        // Act
        var job = new Job(
            Guid.NewGuid(),
            _companyId,
            "Software Engineer",
            "Job Description",
            EmploymentType.FullTime,
            WorkplaceType.Remote,
            ExperienceLevel.MidLevel,
            "Remote",
            _validSalaryRange);

        // Assert
        job.Should().NotBeNull();
        job.CompanyId.Should().Be(_companyId);
        job.Title.Should().Be("Software Engineer");
        job.Status.Should().Be(JobStatus.Draft);

        var domainEvent = job.DomainEvents.FirstOrDefault() as JobCreatedEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.JobId.Should().Be(job.Id);
    }

    [Fact]
    public void Publish_WhenInDraftStatus_ShouldUpdateStatusAndRaiseEvent()
    {
        // Arrange
        var job = new Job(
            Guid.NewGuid(),
            _companyId,
            "Software Engineer",
            "Job Description",
            EmploymentType.FullTime,
            WorkplaceType.Remote,
            ExperienceLevel.MidLevel,
            "Remote",
            _validSalaryRange,
            Guid.NewGuid()); // DepartmentId is required for publish

        var publishDate = DateTimeOffset.UtcNow;
        job.ClearDomainEvents();

        // Act
        job.Publish(publishDate);

        // Assert
        job.Status.Should().Be(JobStatus.Published);
        job.PublishedAt.Should().Be(publishDate);

        var domainEvent = job.DomainEvents.FirstOrDefault() as JobPublishedEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.JobId.Should().Be(job.Id);
    }

    [Fact]
    public void Publish_WithoutDepartment_ShouldThrowException()
    {
        // Arrange
        var job = new Job(
            Guid.NewGuid(),
            _companyId,
            "Software Engineer",
            "Job Description",
            EmploymentType.FullTime,
            WorkplaceType.Remote,
            ExperienceLevel.MidLevel,
            "Remote",
            _validSalaryRange); // No DepartmentId

        // Act
        Action action = () => job.Publish(DateTimeOffset.UtcNow);

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("DepartmentId is required to publish a job.");
    }
}
