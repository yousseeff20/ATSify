using ATS.Application.Features.Companies.Commands.CreateCompany;
using ATS.Application.Features.Companies.Commands.DeleteCompany;
using ATS.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace ATS.API.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class CompaniesController(ISender mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Permissions.Companies.Create)]
    public async Task<IActionResult> Create(CreateCompanyCommand command)
    {
        var result = await mediator.Send(command);
        if (!result.IsSuccess) return BadRequest(result.ErrorMessage);
        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.Companies.Delete)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await mediator.Send(new DeleteCompanyCommand(id));
        if (!result.IsSuccess) return BadRequest(result.ErrorMessage);
        return NoContent();
    }
}
