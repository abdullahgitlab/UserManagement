using UserManagement.Application.Abstractions;
using UserManagement.Application.DTOs;
using UserManagement.Application.Exceptions;
using UserManagement.Application.Requests;
using UserManagement.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.Application.Handlers;

public sealed class RefreshTokenHandler(IUserManagementDbContext db, ITokenService tokens, TimeProvider clock) : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var session = await db.UserSessions.Include(item => item.User).SingleOrDefaultAsync(item => item.RefreshTokenHash == hash, cancellationToken);
        var now = clock.GetUtcNow().UtcDateTime;
        if (session is null)
        {
            var previouslyUsed = await db.UsedRefreshTokens.SingleOrDefaultAsync(item => item.TokenHash == hash, cancellationToken);
            if (previouslyUsed is not null)
            {
                var reusedSession = await db.UserSessions.SingleAsync(item => item.Id == previouslyUsed.UserSessionId, cancellationToken);
                Close(reusedSession, now);
                await db.SaveChangesAsync(cancellationToken);
            }
            throw new AuthenticationFailedException();
        }

        if (!session.IsActive || session.RefreshTokenExpiresUtc <= now || !session.User.IsActive)
        {
            if (session.IsActive) Close(session, session.RefreshTokenExpiresUtc <= now ? session.RefreshTokenExpiresUtc : now);
            await db.SaveChangesAsync(cancellationToken);
            throw new AuthenticationFailedException();
        }

        db.UsedRefreshTokens.Add(new UsedRefreshToken { UserSessionId = session.Id, TokenHash = hash, UsedAtUtc = now });
        var rawToken = tokens.CreateRefreshToken();
        session.RefreshTokenHash = tokens.HashRefreshToken(rawToken);
        session.RefreshTokenExpiresUtc = now.AddDays(7);
        await db.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(tokens.CreateAccessToken(session.User, session.Id, now), rawToken, now.AddMinutes(15), session.RefreshTokenExpiresUtc,
            new UserDto(session.User.Id, session.User.Username, session.User.Email, session.User.Role, session.User.IsActive, session.User.EmployeeId));
    }

    private static void Close(UserSession session, DateTime logoutTime)
    {
        session.IsActive = false;
        session.LogoutTimeUtc = logoutTime;
        session.LogoutReason = LogoutReason.UserLogout;
        session.RefreshTokenHash = string.Empty;
    }
}
