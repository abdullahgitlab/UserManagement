using UserManagement.Application.DTOs;
using UserManagement.Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UserManagement.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetCurrentUserQuery(), cancellationToken));

    [HttpGet("{id:guid}/sessions")]
    [ProducesResponseType<PagedResult<SessionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SessionDto>>> Sessions(
        Guid id, [FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new GetUserSessionsQuery(id, fromUtc, toUtc, page, pageSize), cancellationToken));

    [HttpPost("{id:guid}/force-logout")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ForceLogout(Guid id, CancellationToken cancellationToken)
    {
        var count = await sender.Send(new ForceLogoutUserCommand(id), cancellationToken);
        return Ok(new { closedSessions = count });
    }
}
