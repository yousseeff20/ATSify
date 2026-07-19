using ATS.Application.Common.Interfaces;
using ATS.Application.Common.Models;
using ATS.Domain.Common;
using ATS.Domain.Aggregates.Jobs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Jobs.Queries.GetCompanyJobs;

public class GetCompanyJobsQueryHandler(IApplicationDbContext context) : IRequestHandler<GetCompanyJobsQuery, Result<PaginatedList<JobDto>>>
{
    public async Task<Result<PaginatedList<JobDto>>> Handle(GetCompanyJobsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Jobs.AsNoTracking().AsQueryable();

        if (request.CompanyId.HasValue)
            query = query.Where(j => j.CompanyId == request.CompanyId.Value);

        if (request.DepartmentId.HasValue)
            query = query.Where(j => j.DepartmentId == request.DepartmentId.Value);

        if (request.Status.HasValue)
            query = query.Where(j => j.Status == request.Status.Value);

        if (request.EmploymentType.HasValue)
            query = query.Where(j => j.EmploymentType == request.EmploymentType.Value);

        if (request.WorkplaceType.HasValue)
            query = query.Where(j => j.WorkplaceType == request.WorkplaceType.Value);

        if (request.ExperienceLevel.HasValue)
            query = query.Where(j => j.ExperienceLevel == request.ExperienceLevel.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(j => j.Title.ToLower().Contains(search) || j.Location.ToLower().Contains(search));
        }

        query = request.SortBy.ToLower() switch
        {
            "title" => request.SortDescending ? query.OrderByDescending(j => j.Title) : query.OrderBy(j => j.Title),
            "publishedat" => request.SortDescending ? query.OrderByDescending(j => j.PublishedAt) : query.OrderBy(j => j.PublishedAt),
            "salary" => request.SortDescending ? query.OrderByDescending(j => j.SalaryRange.Max) : query.OrderBy(j => j.SalaryRange.Min),
            _ => request.SortDescending ? query.OrderByDescending(j => j.CreatedAt) : query.OrderBy(j => j.CreatedAt)
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

