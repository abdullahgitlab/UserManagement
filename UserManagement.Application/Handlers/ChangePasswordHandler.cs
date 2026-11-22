using UserManagement.Application.Abstractions;
using UserManagement.Application.Exceptions;
using UserManagement.Application.Requests;
using UserManagement.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.Application.Handlers;

public sealed class ChangePasswordHandler(IUserManagementDbContext db, IRequestContext requestContext, IPasswordHasher<User> hasher, TimeProvider clock)
    : IRequestHandler<ChangePasswordCommand, bool>
{
    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (requestContext.UserId is not Guid userId) throw new ForbiddenAccessException();
        var user = await db.Users.SingleAsync(item => item.Id == userId, cancellationToken);
        if (hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
            throw new AuthenticationFailedException();

        var now = clock.GetUtcNow().UtcDateTime;
        user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
        user.UpdatedAt = now;
        var otherSessions = await db.UserSessions.Where(item => item.UserId == userId && item.IsActive && item.Id != requestContext.SessionId)
            .ToListAsync(cancellationToken);
        foreach (var session in otherSessions)
        {
            session.IsActive = false;
            session.LogoutTimeUtc = now;
            session.LogoutReason = LogoutReason.UserLogout;
            session.RefreshTokenHash = string.Empty;
        }
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
