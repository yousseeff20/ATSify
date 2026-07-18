using ATS.Application.Features.Invitations.Commands.AcceptInvitation;
using ATS.Application.Features.Invitations.Commands.CreateInvitation;
using ATS.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace ATS.API.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[ApiVersion("1.0")]
public class InvitationsController(ISender mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Permissions.Invitations.Send)]
    public async Task<IActionResult> Create(CreateInvitationCommand command)
    {
        var result = await mediator.Send(command);
        if (!result.IsSuccess) return BadRequest(result.ErrorMessage);
        return Ok(result.Value);
    }

    [HttpPost("accept")]
    public async Task<IActionResult> Accept(AcceptInvitationCommand command)
    {
        var result = await mediator.Send(command);
        if (!result.IsSuccess) return BadRequest(result.ErrorMessage);
        return Ok();
    }
}
