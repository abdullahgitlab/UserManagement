using UserManagement.Application.Abstractions;
using UserManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace UserManagement.UnitTests;

internal sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options), IUserManagementDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
    public DbSet<UsedRefreshToken> UsedRefreshTokens => Set<UsedRefreshToken>();
}

internal static class TestSupport
{
    public static TestDbContext CreateDb() => new(new DbContextOptionsBuilder<TestDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    public static FakeTimeProvider CreateClock() => new(new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero));

    public static Mock<IRequestContext> RequestContext(Guid? userId = null, string? role = null, Guid? sessionId = null)
    {
        var context = new Mock<IRequestContext>();
        context.SetupGet(item => item.UserId).Returns(userId);
        context.SetupGet(item => item.Role).Returns(role);
        context.SetupGet(item => item.SessionId).Returns(sessionId);
        context.SetupGet(item => item.IpAddress).Returns("127.0.0.1");
        context.SetupGet(item => item.UserAgent).Returns("test");
        return context;
    }

    public static User CreateUser(string password, DateTime now)
    {
        var user = new User
        {
            Username = "person",
            Email = "person@example.com",
            PasswordHash = string.Empty,
            Role = UserRole.Employee,
            CreatedAt = now,
            UpdatedAt = now
        };
        user.PasswordHash = new Microsoft.AspNetCore.Identity.PasswordHasher<User>().HashPassword(user, password);
        return user;
    }
}
