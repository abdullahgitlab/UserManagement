using UserManagement.Application.Abstractions;
using UserManagement.Application.Requests;
using UserManagement.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.Application.Handlers;

public sealed class LogoutHandler(IUserManagementDbContext db, IRequestContext requestContext, TimeProvider clock) : IRequestHandler<LogoutCommand, bool>
{
    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (requestContext.SessionId is not Guid sessionId) return false;
        var session = await db.UserSessions.SingleOrDefaultAsync(item => item.Id == sessionId, cancellationToken);
        if (session is null || !session.IsActive) return false;
        session.IsActive = false;
        session.LogoutTimeUtc = clock.GetUtcNow().UtcDateTime;
        session.LogoutReason = LogoutReason.UserLogout;
        session.RefreshTokenHash = string.Empty;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
