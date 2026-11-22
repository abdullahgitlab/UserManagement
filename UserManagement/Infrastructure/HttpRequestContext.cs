using System.Security.Claims;
using UserManagement.Application.Abstractions;

namespace UserManagement.API.Infrastructure;

public sealed class HttpRequestContext(IHttpContextAccessor httpContextAccessor) : IRequestContext
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;
    public Guid? UserId => Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public string? Role => Principal?.FindFirstValue(ClaimTypes.Role);
    public Guid? SessionId => Guid.TryParse(Principal?.FindFirstValue("sid"), out var id) ? id : null;
    public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public string? UserAgent => httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
}
