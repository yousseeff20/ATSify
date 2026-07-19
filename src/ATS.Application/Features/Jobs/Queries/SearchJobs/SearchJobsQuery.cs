using ATS.Application.Common.Models;
using ATS.Domain.Common;
using ATS.Domain.Aggregates.Jobs;
using MediatR;

namespace ATS.Application.Features.Jobs.Queries.SearchJobs;

public record SearchJobsQuery(
    string? Keyword = null,
    Guid? DepartmentId = null,
    EmploymentType? EmploymentType = null,
    WorkplaceType? WorkplaceType = null,
    ExperienceLevel? ExperienceLevel = null,
    string? Location = null,
    int PageNumber = 1,
    int PageSize = 10,
    string SortBy = "PublishedAt",
    bool SortDescending = true) : IRequest<Result<PaginatedList<JobDto>>>;
