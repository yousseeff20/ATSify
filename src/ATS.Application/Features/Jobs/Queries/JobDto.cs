using ATS.Domain.Aggregates.Jobs;

namespace ATS.Application.Features.Jobs.Queries;

public record JobDto(
    Guid Id,
    Guid CompanyId,
    Guid? DepartmentId,
    string Title,
    string Description,
    EmploymentType EmploymentType,
    WorkplaceType WorkplaceType,
    ExperienceLevel ExperienceLevel,
    string Location,
    decimal SalaryMin,
    decimal SalaryMax,
    string SalaryCurrency,
    JobStatus Status,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ClosedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
