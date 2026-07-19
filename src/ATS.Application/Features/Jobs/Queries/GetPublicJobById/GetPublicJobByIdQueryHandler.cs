using ATS.Application.Common.Interfaces;
using ATS.Domain.Common;
using ATS.Domain.Aggregates.Jobs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Jobs.Queries.GetPublicJobById;

public class GetPublicJobByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetPublicJobByIdQuery, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(GetPublicJobByIdQuery request, CancellationToken cancellationToken)
    {
        var job = await context.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == request.JobId && j.Status == JobStatus.Published, cancellationToken);

        if (job == null)
            return Result<JobDto>.Failure("Job not found or not published.");

        var dto = new JobDto(
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
            job.UpdatedAt);

        return Result<JobDto>.Success(dto);
    }
}
