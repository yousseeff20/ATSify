using ATS.Application.Features.Departments.Commands.CreateDepartment;
using ATS.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace ATS.API.Controllers;

[Route("api/v{version:apiVersion}/companies/{companyId}/[controller]")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class DepartmentsController(ISender mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Permissions.Departments.Create)]
    public async Task<IActionResult> Create(Guid companyId, CreateDepartmentCommand command)
    {
        if (companyId != command.CompanyId) return BadRequest("Company ID mismatch.");
        
        var result = await mediator.Send(command);
        if (!result.IsSuccess) return BadRequest(result.ErrorMessage);
        return Ok(result.Value);
    }
}
