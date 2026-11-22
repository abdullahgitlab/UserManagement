using UserManagement.Application.Abstractions;
using UserManagement.Application.Exceptions;
using UserManagement.Application.Handlers;
using UserManagement.Application.Requests;
using UserManagement.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace UserManagement.UnitTests;

public sealed class SessionHandlerTests
{
    [Fact]
    public async Task Logout_closes_session_and_second_call_keeps_original_logout_time()
    {
        await using var db = TestSupport.CreateDb();
        var clock = TestSupport.CreateClock();
        var session = CreateSession(clock.GetUtcNow().UtcDateTime);
        db.UserSessions.Add(session);
        await db.SaveChangesAsync();
        var handler = new LogoutHandler(db, TestSupport.RequestContext(sessionId: session.Id).Object, clock);

        (await handler.Handle(new LogoutCommand(), CancellationToken.None)).Should().BeTrue();
        var originalLogoutTime = session.LogoutTimeUtc;
        clock.Advance(TimeSpan.FromMinutes(2));
        (await handler.Handle(new LogoutCommand(), CancellationToken.None)).Should().BeFalse();

        session.LogoutTimeUtc.Should().Be(originalLogoutTime);
        session.LogoutReason.Should().Be(LogoutReason.UserLogout);
        session.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Logout_returns_false_for_unknown_session()
    {
        await using var db = TestSupport.CreateDb();
        var handler = new LogoutHandler(db, TestSupport.RequestContext(sessionId: Guid.NewGuid()).Object, TestSupport.CreateClock());

        (await handler.Handle(new LogoutCommand(), CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task Refresh_rotates_token_and_reuse_closes_session()
    {
        await using var db = TestSupport.CreateDb();
        var clock = TestSupport.CreateClock();
        var user = TestSupport.CreateUser("Password1!", clock.GetUtcNow().UtcDateTime);
        var session = CreateSession(clock.GetUtcNow().UtcDateTime, user);
        session.RefreshTokenHash = "hash:old-token";
        db.Users.Add(user);
        db.UserSessions.Add(session);
        await db.SaveChangesAsync();
        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(item => item.HashRefreshToken(It.IsAny<string>())).Returns<string>(token => $"hash:{token}");
        tokenService.Setup(item => item.CreateRefreshToken()).Returns("new-token");
        tokenService.Setup(item => item.CreateAccessToken(It.IsAny<User>(), It.IsAny<Guid>(), It.IsAny<DateTime>())).Returns("access");
        var handler = new RefreshTokenHandler(db, tokenService.Object, clock);

        var response = await handler.Handle(new RefreshTokenCommand("old-token"), CancellationToken.None);
        response.RefreshToken.Should().Be("new-token");
        session.RefreshTokenHash.Should().Be("hash:new-token");
        await Assert.ThrowsAsync<AuthenticationFailedException>(() => handler.Handle(new RefreshTokenCommand("old-token"), CancellationToken.None));
        session.IsActive.Should().BeFalse();
        session.LogoutTimeUtc.Should().Be(clock.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public async Task Force_logout_closes_every_active_session_for_user()
    {
        await using var db = TestSupport.CreateDb();
        var clock = TestSupport.CreateClock();
        var user = TestSupport.CreateUser("Password1!", clock.GetUtcNow().UtcDateTime);
        var active = CreateSession(clock.GetUtcNow().UtcDateTime, user);
        var inactive = CreateSession(clock.GetUtcNow().UtcDateTime, user);
        inactive.IsActive = false;
        db.Users.Add(user);
        db.UserSessions.AddRange(active, inactive);
        await db.SaveChangesAsync();
        var handler = new ForceLogoutUserHandler(db, TestSupport.RequestContext(role: nameof(UserRole.Admin)).Object, clock);

        var count = await handler.Handle(new ForceLogoutUserCommand(user.Id), CancellationToken.None);

        count.Should().Be(1);
        active.LogoutReason.Should().Be(LogoutReason.ForcedByAdmin);
        active.IsActive.Should().BeFalse();
        inactive.LogoutTimeUtc.Should().BeNull();
    }

    [Fact]
    public async Task Expiration_closes_only_sessions_expired_by_now_using_expiry_timestamp()
    {
        await using var db = TestSupport.CreateDb();
        var clock = TestSupport.CreateClock();
        var expired = CreateSession(clock.GetUtcNow().UtcDateTime);
        expired.RefreshTokenExpiresUtc = clock.GetUtcNow().UtcDateTime.AddMinutes(-1);
        var current = CreateSession(clock.GetUtcNow().UtcDateTime);
        current.RefreshTokenExpiresUtc = clock.GetUtcNow().UtcDateTime.AddMinutes(1);
        db.UserSessions.AddRange(expired, current);
        await db.SaveChangesAsync();

        var count = await new ExpiredSessionsHandler(db).CloseExpiredAsync(clock.GetUtcNow().UtcDateTime, CancellationToken.None);

        count.Should().Be(1);
        expired.LogoutTimeUtc.Should().Be(expired.RefreshTokenExpiresUtc);
        expired.LogoutReason.Should().Be(LogoutReason.TokenExpired);
        current.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task User_session_history_applies_date_range_pagination_and_duration()
    {
        await using var db = TestSupport.CreateDb();
        var clock = TestSupport.CreateClock();
        var user = TestSupport.CreateUser("Password1!", clock.GetUtcNow().UtcDateTime);
        var first = CreateSession(clock.GetUtcNow().UtcDateTime.AddDays(-2), user);
        first.LogoutTimeUtc = first.LoginTimeUtc.AddHours(1);
        var second = CreateSession(clock.GetUtcNow().UtcDateTime.AddDays(-1), user);
        second.LogoutTimeUtc = second.LoginTimeUtc.AddMinutes(20);
        db.Users.Add(user);
        db.UserSessions.AddRange(first, second);
        await db.SaveChangesAsync();
        var handler = new GetUserSessionsHandler(db, TestSupport.RequestContext(user.Id).Object);

        var result = await handler.Handle(new GetUserSessionsQuery(user.Id, clock.GetUtcNow().UtcDateTime.AddDays(-1), null, 1, 1), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].SessionDuration.Should().Be(TimeSpan.FromMinutes(20));
    }

    [Fact]
    public async Task Non_admin_cannot_read_another_users_sessions()
    {
        await using var db = TestSupport.CreateDb();
        var handler = new GetUserSessionsHandler(db, TestSupport.RequestContext(Guid.NewGuid(), nameof(UserRole.Employee)).Object);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(new GetUserSessionsQuery(Guid.NewGuid(), null, null), CancellationToken.None));
    }

    private static UserSession CreateSession(DateTime now, User? user = null) => new()
    {
        UserId = user?.Id ?? Guid.NewGuid(),
        User = user!,
        LoginTimeUtc = now,
        RefreshTokenHash = Guid.NewGuid().ToString("N"),
        RefreshTokenExpiresUtc = now.AddDays(7)
    };
}
