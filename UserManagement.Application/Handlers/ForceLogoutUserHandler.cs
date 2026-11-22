using UserManagement.Application.Abstractions;
using UserManagement.Application.Exceptions;
using UserManagement.Application.Requests;
using UserManagement.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.Application.Handlers;

public sealed class ForceLogoutUserHandler(IUserManagementDbContext db, IRequestContext requestContext, TimeProvider clock) : IRequestHandler<ForceLogoutUserCommand, int>
{
    public async Task<int> Handle(ForceLogoutUserCommand request, CancellationToken cancellationToken)
    {
        if (requestContext.Role != nameof(UserRole.Admin)) throw new ForbiddenAccessException();
        var sessions = await db.UserSessions.Where(item => item.UserId == request.UserId && item.IsActive).ToListAsync(cancellationToken);
        var now = clock.GetUtcNow().UtcDateTime;
        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.LogoutTimeUtc = now;
            session.LogoutReason = LogoutReason.ForcedByAdmin;
            session.RefreshTokenHash = string.Empty;
        }
        await db.SaveChangesAsync(cancellationToken);
        return sessions.Count;
    }
}
