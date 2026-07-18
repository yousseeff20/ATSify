using ATS.Application.Features.Roles.Commands.CreateRole;
using ATS.Application.Features.Roles.Commands.AssignRolePermissions;
using ATS.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATS.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/roles")]
public class RolesController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Permissions.Roles.Create)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { Error = result.ErrorMessage });
    }

    [HttpPost("{roleId}/permissions")]
    [Authorize(Policy = Permissions.Roles.Update)]
    public async Task<IActionResult> AssignPermissions(Guid roleId, [FromBody] List<string> permissions, CancellationToken cancellationToken)
    {
        var command = new AssignRolePermissionsCommand(roleId, permissions);
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok() : BadRequest(new { Error = result.ErrorMessage });
    }
}
