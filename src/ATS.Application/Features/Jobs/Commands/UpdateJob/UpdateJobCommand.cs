using ATS.Application.Common.Models;
using ATS.Domain.Common;
using ATS.Domain.Aggregates.Jobs;
using MediatR;

namespace ATS.Application.Features.Jobs.Commands.UpdateJob;

public record UpdateJobCommand(
    Guid JobId,
    string Title,
    string Description,
    EmploymentType EmploymentType,
    WorkplaceType WorkplaceType,
    ExperienceLevel ExperienceLevel,
    string Location,
    decimal SalaryMin,
    decimal SalaryMax,
    string SalaryCurrency,
    Guid? DepartmentId = null) : IRequest<Result>;

