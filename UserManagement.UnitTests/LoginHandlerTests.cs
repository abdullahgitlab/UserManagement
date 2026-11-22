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

public sealed class LoginHandlerTests
{
    private const string Password = "Password1!";

    [Fact]
    public async Task Login_success_creates_session_at_fake_time_and_successful_attempt()
    {
        await using var db = TestSupport.CreateDb();
        var clock = TestSupport.CreateClock();
        var user = TestSupport.CreateUser(Password, clock.GetUtcNow().UtcDateTime);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var tokens = TokenMock();
        var handler = new LoginHandler(db, new PasswordHasher<User>(), tokens.Object, TestSupport.RequestContext().Object, clock);

        var result = await handler.Handle(new LoginCommand(user.Username, Password), CancellationToken.None);

        result.User.Id.Should().Be(user.Id);
        result.RefreshToken.Should().Be("refresh-1");
        var session = await db.UserSessions.SingleAsync();
        session.LoginTimeUtc.Should().Be(clock.GetUtcNow().UtcDateTime);
        session.RefreshTokenHash.Should().Be("hash:refresh-1");
        (await db.LoginAttempts.SingleAsync()).IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task Wrong_password_increments_counter_and_audits_failure()
    {
        await using var db = TestSupport.CreateDb();
        var clock = TestSupport.CreateClock();
        var user = TestSupport.CreateUser(Password, clock.GetUtcNow().UtcDateTime);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var handler = new LoginHandler(db, new PasswordHasher<User>(), TokenMock().Object, TestSupport.RequestContext().Object, clock);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => handler.Handle(new LoginCommand(user.Username, "WrongPass1!"), CancellationToken.None));

        (await db.Users.SingleAsync()).FailedLoginAttempts.Should().Be(1);
        var attempt = await db.LoginAttempts.SingleAsync();
        attempt.IsSuccessful.Should().BeFalse();
        attempt.FailureReason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Fifth_failure_locks_account_and_correct_password_is_rejected_while_locked()
    {
        await using var db = TestSupport.CreateDb();
        var clock = TestSupport.CreateClock();
        var user = TestSupport.CreateUser(Password, clock.GetUtcNow().UtcDateTime);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var handler = new LoginHandler(db, new PasswordHasher<User>(), TokenMock().Object, TestSupport.RequestContext().Object, clock);

        for (var index = 0; index < 5; index++)
            await Assert.ThrowsAsync<AuthenticationFailedException>(() => handler.Handle(new LoginCommand(user.Username, "WrongPass1!"), CancellationToken.None));
        var locked = await db.Users.SingleAsync();
        locked.LockoutEndUtc.Should().Be(clock.GetUtcNow().UtcDateTime.AddMinutes(15));
        await Assert.ThrowsAsync<AuthenticationFailedException>(() => handler.Handle(new LoginCommand(user.Username, Password), CancellationToken.None));
        (await db.LoginAttempts.CountAsync()).Should().Be(6);
    }

    [Fact]
    public async Task Inactive_user_is_rejected_and_attempt_is_recorded()
    {
        await using var db = TestSupport.CreateDb();
        var clock = TestSupport.CreateClock();
        var user = TestSupport.CreateUser(Password, clock.GetUtcNow().UtcDateTime);
        user.IsActive = false;
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var handler = new LoginHandler(db, new PasswordHasher<User>(), TokenMock().Object, TestSupport.RequestContext().Object, clock);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => handler.Handle(new LoginCommand(user.Username, Password), CancellationToken.None));

        (await db.LoginAttempts.SingleAsync()).IsSuccessful.Should().BeFalse();
    }

    private static Mock<ITokenService> TokenMock()
    {
        var mock = new Mock<ITokenService>();
        mock.Setup(item => item.CreateRefreshToken()).Returns("refresh-1");
        mock.Setup(item => item.HashRefreshToken(It.IsAny<string>())).Returns<string>(token => $"hash:{token}");
        mock.Setup(item => item.CreateAccessToken(It.IsAny<User>(), It.IsAny<Guid>(), It.IsAny<DateTime>())).Returns("access");
        return mock;
    }
}
