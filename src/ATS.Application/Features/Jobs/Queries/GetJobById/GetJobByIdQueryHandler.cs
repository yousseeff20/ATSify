using ATS.Application.Common.Interfaces;
using ATS.Application.Common.Models;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Jobs.Queries.GetJobById;

public class GetJobByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetJobByIdQuery, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(GetJobByIdQuery request, CancellationToken cancellationToken)
    {
        var job = await context.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);

        if (job == null)
            return Result<JobDto>.Failure("Job not found.");

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
