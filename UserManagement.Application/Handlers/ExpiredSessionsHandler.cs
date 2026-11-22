using UserManagement.Application.Abstractions;
using UserManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.Application.Handlers;

public sealed class ExpiredSessionsHandler(IUserManagementDbContext db)
{
    public async Task<int> CloseExpiredAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var sessions = await db.UserSessions.Where(item => item.IsActive && item.RefreshTokenExpiresUtc <= utcNow).ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.LogoutTimeUtc = session.RefreshTokenExpiresUtc;
            session.LogoutReason = LogoutReason.TokenExpired;
            session.RefreshTokenHash = string.Empty;
        }
        await db.SaveChangesAsync(cancellationToken);
        return sessions.Count;
    }
}
