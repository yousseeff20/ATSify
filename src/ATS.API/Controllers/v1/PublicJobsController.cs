using ATS.Application.Common.Models;
using ATS.Domain.Aggregates.Jobs;
using ATS.Application.Features.Jobs.Queries.SearchJobs;
using ATS.Application.Features.Jobs.Queries.GetPublicJobById;
using ATS.Application.Features.Jobs.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace ATS.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/public/jobs")]
[AllowAnonymous]
public class PublicJobsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedList<JobDto>>> SearchJobs(
        [FromQuery] string? keyword,
        [FromQuery] Guid? departmentId,
        [FromQuery] EmploymentType? employmentType,
        [FromQuery] WorkplaceType? workplaceType,
        [FromQuery] ExperienceLevel? experienceLevel,
        [FromQuery] string? location,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "PublishedAt",
        [FromQuery] bool sortDescending = true)
    {
        var query = new SearchJobsQuery(
            keyword,
            departmentId,
            employmentType,
            workplaceType,
            experienceLevel,
            location,
            pageNumber,
            pageSize,
            sortBy ?? "PublishedAt",
            sortDescending);

        var result = await mediator.Send(query);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<JobDto>> GetJobById([FromRoute] Guid id)
    {
        var query = new GetPublicJobByIdQuery(id);
        var result = await mediator.Send(query);

        if (!result.IsSuccess)
            return NotFound(result.ErrorMessage);

        return Ok(result.Value);
    }
}
