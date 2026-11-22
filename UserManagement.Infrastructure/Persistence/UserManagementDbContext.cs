using UserManagement.Application.Abstractions;
using UserManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.Infrastructure.Persistence;

public sealed class UserManagementDbContext(DbContextOptions<UserManagementDbContext> options) : DbContext(options), IUserManagementDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
    public DbSet<UsedRefreshToken> UsedRefreshTokens => Set<UsedRefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Username).HasMaxLength(100).IsRequired();
            entity.Property(item => item.Email).HasMaxLength(256).IsRequired();
            entity.Property(item => item.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(item => item.Role).HasConversion<string>().HasMaxLength(20);
            entity.Property(item => item.CreatedAt).HasColumnType("datetime2");
            entity.Property(item => item.UpdatedAt).HasColumnType("datetime2");
            entity.Property(item => item.LockoutEndUtc).HasColumnType("datetime2");
            entity.HasIndex(item => item.Username).IsUnique();
            entity.HasIndex(item => item.Email).IsUnique();
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.LoginTimeUtc).HasColumnType("datetime2");
            entity.Property(item => item.LogoutTimeUtc).HasColumnType("datetime2");
            entity.Property(item => item.RefreshTokenExpiresUtc).HasColumnType("datetime2");
            entity.Property(item => item.RefreshTokenHash).HasMaxLength(128).IsRequired();
            entity.Property(item => item.IpAddress).HasMaxLength(45);
            entity.Property(item => item.UserAgent).HasMaxLength(512);
            entity.Property(item => item.LogoutReason).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(item => new { item.UserId, item.LoginTimeUtc });
            entity.HasIndex(item => item.RefreshTokenHash);
            entity.HasOne(item => item.User).WithMany(item => item.Sessions).HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LoginAttempt>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.UsernameAttempted).HasMaxLength(100).IsRequired();
            entity.Property(item => item.AttemptTimeUtc).HasColumnType("datetime2");
            entity.Property(item => item.FailureReason).HasMaxLength(128);
            entity.Property(item => item.IpAddress).HasMaxLength(45);
            entity.Property(item => item.UserAgent).HasMaxLength(512);
            entity.HasIndex(item => item.AttemptTimeUtc);
        });

        modelBuilder.Entity<UsedRefreshToken>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(item => item.UsedAtUtc).HasColumnType("datetime2");
            entity.HasIndex(item => item.TokenHash).IsUnique();
            entity.HasIndex(item => item.UserSessionId);
        });
    }
}
