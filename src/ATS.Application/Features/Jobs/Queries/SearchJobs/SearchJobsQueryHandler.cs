using ATS.Application.Common.Interfaces;
using ATS.Application.Common.Models;
using ATS.Domain.Common;
using ATS.Domain.Aggregates.Jobs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Jobs.Queries.SearchJobs;

public class SearchJobsQueryHandler(IApplicationDbContext context) : IRequestHandler<SearchJobsQuery, Result<PaginatedList<JobDto>>>
{
    public async Task<Result<PaginatedList<JobDto>>> Handle(SearchJobsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Jobs.AsNoTracking().Where(j => j.Status == JobStatus.Published);

        if (request.DepartmentId.HasValue)
            query = query.Where(j => j.DepartmentId == request.DepartmentId.Value);

        if (request.EmploymentType.HasValue)
            query = query.Where(j => j.EmploymentType == request.EmploymentType.Value);

        if (request.WorkplaceType.HasValue)
            query = query.Where(j => j.WorkplaceType == request.WorkplaceType.Value);

        if (request.ExperienceLevel.HasValue)
            query = query.Where(j => j.ExperienceLevel == request.ExperienceLevel.Value);

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            var location = request.Location.ToLower();
            query = query.Where(j => j.Location.ToLower().Contains(location));
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.ToLower();
            query = query.Where(j => j.Title.ToLower().Contains(keyword) || j.Description.ToLower().Contains(keyword));
        }

        query = request.SortBy.ToLower() switch
        {
            "title" => request.SortDescending ? query.OrderByDescending(j => j.Title) : query.OrderBy(j => j.Title),
            "salary" => request.SortDescending ? query.OrderByDescending(j => j.SalaryRange.Max) : query.OrderBy(j => j.SalaryRange.Min),
            _ => request.SortDescending ? query.OrderByDescending(j => j.PublishedAt) : query.OrderBy(j => j.PublishedAt)
        };

        var count = await query.CountAsync(cancellationToken);
        
        var jobs = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(job => new JobDto(
                job.Id,
                job.CompanyId,
                job.DepartmentId,
                job.Title,
                job.Description,
                job.EmploymentType,
                job.WorkplaceType,
                job.ExperienceLevel,
                job.Location,
                job.SalaryRange.Min,
                job.SalaryRange.Max,
                job.SalaryRange.Currency,
                job.Status,
                job.PublishedAt,
                job.ClosedAt,
                job.CreatedAt,
                job.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result<PaginatedList<JobDto>>.Success(new PaginatedList<JobDto>(jobs, count, request.PageNumber, request.PageSize));
    }
}
