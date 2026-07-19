using ATS.Domain.Aggregates.Jobs.Events;
using ATS.Domain.Aggregates.Jobs.ValueObjects;
using ATS.Domain.Common;

namespace ATS.Domain.Aggregates.Jobs;

public sealed class Job : AggregateRoot
{
    public Guid CompanyId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    
    public string Title { get; private set; }
    public string Description { get; private set; }
    public EmploymentType EmploymentType { get; private set; }
    public WorkplaceType WorkplaceType { get; private set; }
    public ExperienceLevel ExperienceLevel { get; private set; }
    public string Location { get; private set; }
    public SalaryRange SalaryRange { get; private set; }
    
    public JobStatus Status { get; private set; }
    
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    private Job() 
    { 
        Title = null!;
        Description = null!;
        Location = null!;
        SalaryRange = null!;
    } // EF Core

    public Job(
        Guid id, 
        Guid companyId, 
        string title, 
        string description, 
        EmploymentType employmentType, 
        WorkplaceType workplaceType, 
        ExperienceLevel experienceLevel, 
        string location, 
        SalaryRange salaryRange, 
        Guid? departmentId = null)
    {
        Id = id;
        CompanyId = companyId;
        DepartmentId = departmentId;
        Title = title;
        Description = description;
        EmploymentType = employmentType;
        WorkplaceType = workplaceType;
        ExperienceLevel = experienceLevel;
        Location = location;
        SalaryRange = salaryRange;
        Status = JobStatus.Draft;

        AddDomainEvent(new JobCreatedEvent(Id));
    }

    public Result Update(
        string title,
        string description,
        EmploymentType employmentType,
        WorkplaceType workplaceType,
        ExperienceLevel experienceLevel,
        string location,
        SalaryRange salaryRange,
        Guid? departmentId)
    {
        if (Status == JobStatus.Archived)
            return Result.Failure("Cannot update an archived job.");
            
        Title = title;
        Description = description;
        EmploymentType = employmentType;
        WorkplaceType = workplaceType;
        ExperienceLevel = experienceLevel;
        Location = location;
        SalaryRange = salaryRange;
        DepartmentId = departmentId;

        return Result.Success();
    }

    public Result Publish(DateTimeOffset publishedAt)
    {
        if (Status != JobStatus.Draft)
            return Result.Failure($"Cannot publish a job from status {Status}. Only Draft jobs can be published.");
            
        if (!DepartmentId.HasValue)
            return Result.Failure("DepartmentId is required to publish a job.");

        if (string.IsNullOrWhiteSpace(Title))
            return Result.Failure("Title is required to publish a job.");

        if (string.IsNullOrWhiteSpace(Description))
            return Result.Failure("Description is required to publish a job.");

        Status = JobStatus.Published;
        PublishedAt = publishedAt;

        AddDomainEvent(new JobPublishedEvent(Id));

        return Result.Success();
    }

    public Result Close(DateTimeOffset closedAt)
    {
        if (Status != JobStatus.Published)
            return Result.Failure($"Cannot close a job from status {Status}. Only Published jobs can be closed.");

        Status = JobStatus.Closed;
        ClosedAt = closedAt;

        AddDomainEvent(new JobClosedEvent(Id));

        return Result.Success();
    }

    public void Archive()
    {
        if (Status == JobStatus.Archived)
            return;

        Status = JobStatus.Archived;

        AddDomainEvent(new JobArchivedEvent(Id));
    }
}
