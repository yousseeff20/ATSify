using ATS.Application.Common.Models;
using ATS.Application.Features.Jobs.Commands.ArchiveJob;
using ATS.Application.Features.Jobs.Commands.CloseJob;
using ATS.Application.Features.Jobs.Commands.CreateJob;
using ATS.Application.Features.Jobs.Commands.PublishJob;
using ATS.Application.Features.Jobs.Commands.UpdateJob;
using ATS.Application.Features.Jobs.Queries;
using ATS.Application.Features.Jobs.Queries.GetJobById;
using ATS.Application.Features.Jobs.Queries.GetJobs;
using ATS.Domain.Aggregates.Jobs;
using ATS.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace ATS.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/companies/{companyId}/jobs")]
public class JobsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PaginatedList<JobDto>>> GetJobs(
        [FromRoute] Guid companyId,
        [FromQuery] Guid? departmentId,
        [FromQuery] string? searchTerm,
        [FromQuery] JobStatus? status,
        [FromQuery] EmploymentType? employmentType,
        [FromQuery] WorkplaceType? workplaceType,
        [FromQuery] ExperienceLevel? experienceLevel,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] bool sortDescending = true)
    {
        var query = new GetJobsQuery(
            companyId,
            departmentId,
            searchTerm,
            status,
            employmentType,
            workplaceType,
            experienceLevel,
            pageNumber,
            pageSize,
            sortBy,
            sortDescending);

        var result = await mediator.Send(query);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
    }

    [HttpGet("{jobId}")]
    [AllowAnonymous]
    public async Task<ActionResult<JobDto>> GetJobById([FromRoute] Guid companyId, [FromRoute] Guid jobId)
    {
        var query = new GetJobByIdQuery(jobId);
        var result = await mediator.Send(query);

        if (!result.IsSuccess)
            return NotFound(result.ErrorMessage);

        if (result.Value != null && result.Value.CompanyId != companyId)
            return BadRequest("Job does not belong to the specified company.");

        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Jobs.Create)]
    public async Task<ActionResult<Guid>> CreateJob(
        [FromRoute] Guid companyId, 
        [FromBody] CreateJobRequest request)
    {
        var command = new CreateJobCommand(
            companyId,
            request.Title,
            request.Description,
            request.EmploymentType,
            request.WorkplaceType,
            request.ExperienceLevel,
            request.Location,
            request.SalaryMin,
            request.SalaryMax,
            request.SalaryCurrency,
            request.DepartmentId);

        var result = await mediator.Send(command);

        return result.IsSuccess 
            ? CreatedAtAction(nameof(GetJobById), new { version = "1.0", companyId, jobId = result.Value }, result.Value) 
            : BadRequest(result.ErrorMessage);
    }

    [HttpPut("{jobId}")]
    [Authorize(Policy = Permissions.Jobs.Update)]
    public async Task<ActionResult> UpdateJob(
        [FromRoute] Guid companyId,
        [FromRoute] Guid jobId,
        [FromBody] UpdateJobRequest request)
    {
        var command = new UpdateJobCommand(
            jobId,
            request.Title,
            request.Description,
            request.EmploymentType,
            request.WorkplaceType,
            request.ExperienceLevel,
            request.Location,
            request.SalaryMin,
            request.SalaryMax,
            request.SalaryCurrency,
            request.DepartmentId);

        var result = await mediator.Send(command);

        return result.IsSuccess ? NoContent() : BadRequest(result.ErrorMessage);
    }

    [HttpPost("{jobId}/publish")]
    [Authorize(Policy = Permissions.Jobs.Publish)]
    public async Task<ActionResult> PublishJob(
        [FromRoute] Guid companyId,
        [FromRoute] Guid jobId)
    {
        var command = new PublishJobCommand(jobId);
        var result = await mediator.Send(command);

        return result.IsSuccess ? NoContent() : BadRequest(result.ErrorMessage);
    }

    [HttpPost("{jobId}/close")]
    [Authorize(Policy = Permissions.Jobs.Close)]
    public async Task<ActionResult> CloseJob(
        [FromRoute] Guid companyId,
        [FromRoute] Guid jobId)
    {
        var command = new CloseJobCommand(jobId);
        var result = await mediator.Send(command);

        return result.IsSuccess ? NoContent() : BadRequest(result.ErrorMessage);
    }

    [HttpPost("{jobId}/archive")]
    [Authorize(Policy = Permissions.Jobs.Archive)]
    public async Task<ActionResult> ArchiveJob(
        [FromRoute] Guid companyId,
        [FromRoute] Guid jobId)
    {
        var command = new ArchiveJobCommand(jobId);
        var result = await mediator.Send(command);

        return result.IsSuccess ? NoContent() : BadRequest(result.ErrorMessage);
    }
}

public record CreateJobRequest(
    string Title,
    string Description,
    EmploymentType EmploymentType,
    WorkplaceType WorkplaceType,
    ExperienceLevel ExperienceLevel,
    string Location,
    decimal SalaryMin,
    decimal SalaryMax,
    string SalaryCurrency,
    Guid? DepartmentId = null);

public record UpdateJobRequest(
    string Title,
    string Description,
    EmploymentType EmploymentType,
    WorkplaceType WorkplaceType,
    ExperienceLevel ExperienceLevel,
    string Location,
    decimal SalaryMin,
    decimal SalaryMax,
    string SalaryCurrency,
    Guid? DepartmentId = null);
