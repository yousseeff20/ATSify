using ATS.Application.Common.Models;
using ATS.Domain.Aggregates.Jobs;
using MediatR;

namespace ATS.Application.Features.Jobs.Commands.CreateJob;

public record CreateJobCommand(
    Guid CompanyId,
    string Title,
    string Description,
    EmploymentType EmploymentType,
    WorkplaceType WorkplaceType,
    ExperienceLevel ExperienceLevel,
    string Location,
    decimal SalaryMin,
    decimal SalaryMax,
    string SalaryCurrency,
    Guid? DepartmentId = null) : IRequest<Result<Guid>>;
