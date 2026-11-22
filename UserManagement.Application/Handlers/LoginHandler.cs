using UserManagement.Application.Abstractions;
using UserManagement.Application.DTOs;
using UserManagement.Application.Exceptions;
using UserManagement.Application.Requests;
using UserManagement.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.Application.Handlers;

public sealed class LoginHandler(IUserManagementDbContext db, IPasswordHasher<User> hasher, ITokenService tokens, IRequestContext requestContext, TimeProvider clock)
    : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim().ToLower();
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Username.ToLower() == username, cancellationToken);
        var now = clock.GetUtcNow().UtcDateTime;
        var attempt = new LoginAttempt
        {
            UsernameAttempted = request.Username.Trim(),
            UserId = user?.Id,
            AttemptTimeUtc = now,
            IsSuccessful = false,
            FailureReason = "Invalid credentials",
            IpAddress = requestContext.IpAddress,
            UserAgent = requestContext.UserAgent
        };
        db.LoginAttempts.Add(attempt);

        if (user is null || !user.IsActive || (user.LockoutEndUtc is not null && user.LockoutEndUtc > now))
        {
            await db.SaveChangesAsync(cancellationToken);
            throw new AuthenticationFailedException();
        }

        if (user.LockoutEndUtc is not null)
        {
            user.LockoutEndUtc = null;
            user.FailedLoginAttempts = 0;
        }

        var verification = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5) user.LockoutEndUtc = now.Add(LockoutDuration);
            user.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
            throw new AuthenticationFailedException();
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = hasher.HashPassword(user, request.Password);

        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;
        user.UpdatedAt = now;
        attempt.IsSuccessful = true;
        attempt.FailureReason = null;

        var session = new UserSession
        {
            UserId = user.Id,
            User = user,
            LoginTimeUtc = now,
            RefreshTokenHash = string.Empty,
            RefreshTokenExpiresUtc = now.AddDays(7),
            IpAddress = requestContext.IpAddress,
            UserAgent = requestContext.UserAgent
        };
        var rawRefreshToken = tokens.CreateRefreshToken();
        session.RefreshTokenHash = tokens.HashRefreshToken(rawRefreshToken);
        db.UserSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(tokens.CreateAccessToken(user, session.Id, now), rawRefreshToken, now.AddMinutes(15), session.RefreshTokenExpiresUtc,
            new UserDto(user.Id, user.Username, user.Email, user.Role, user.IsActive, user.EmployeeId));
    }
}
