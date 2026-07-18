using ATS.Application.Features.Users.Commands.CreateUser;
using ATS.Application.Features.Users.Commands.AssignUserRole;
using ATS.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATS.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/users")]
public class UsersController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Permissions.Users.Create)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { Error = result.ErrorMessage });
    }

    [HttpPost("{userId}/roles")]
    [Authorize(Policy = Permissions.Users.Update)]
    public async Task<IActionResult> AssignRole(Guid userId, [FromBody] Guid roleId, CancellationToken cancellationToken)
    {
        var command = new AssignUserRoleCommand(userId, roleId);
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok() : BadRequest(new { Error = result.ErrorMessage });
    }
}
