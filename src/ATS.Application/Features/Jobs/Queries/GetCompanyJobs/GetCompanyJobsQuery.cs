using ATS.Application.Common.Models;
using ATS.Domain.Common;
using ATS.Domain.Aggregates.Jobs;
using MediatR;

namespace ATS.Application.Features.Jobs.Queries.GetCompanyJobs;

public record GetCompanyJobsQuery(
    Guid? CompanyId = null,
    Guid? DepartmentId = null,
    string? SearchTerm = null,
    JobStatus? Status = null,
    EmploymentType? EmploymentType = null,
    WorkplaceType? WorkplaceType = null,
    ExperienceLevel? ExperienceLevel = null,
    int PageNumber = 1,
    int PageSize = 10,
    string SortBy = "CreatedAt",
    bool SortDescending = true) : IRequest<Result<PaginatedList<JobDto>>>;

