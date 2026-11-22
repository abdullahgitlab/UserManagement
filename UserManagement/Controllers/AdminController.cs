using UserManagement.Application.DTOs;
using UserManagement.Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UserManagement.API.Controllers;

[ApiController]
[Route("api")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminController(ISender sender) : ControllerBase
{
    [HttpGet("sessions/active")]
    [ProducesResponseType<IReadOnlyList<SessionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SessionDto>>> ActiveSessions(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetActiveSessionsQuery(), cancellationToken));

    [HttpGet("login-attempts")]
    [ProducesResponseType<PagedResult<LoginAttemptDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LoginAttemptDto>>> LoginAttempts(
        [FromQuery] string? username, [FromQuery] bool? successful, [FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new GetLoginAttemptsQuery(username, successful, fromUtc, toUtc, page, pageSize), cancellationToken));
}
