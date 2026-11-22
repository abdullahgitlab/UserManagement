using UserManagement.Application.DTOs;
using UserManagement.Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace UserManagement.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<UserDto>> Register(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var user = await sender.Send(command, cancellationToken);
        return Created($"/api/users/{user.Id}", user);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginCommand command, CancellationToken cancellationToken) =>
        Ok(await sender.Send(command, cancellationToken));

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await sender.Send(new LogoutCommand(), cancellationToken);
        return NoContent();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Refresh(RefreshTokenCommand command, CancellationToken cancellationToken) =>
        Ok(await sender.Send(command, cancellationToken));

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }
}
