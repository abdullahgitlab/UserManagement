using UserManagement.Application.Abstractions;
using UserManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace UserManagement.Infrastructure.Services;

public sealed class AdminSeedService(IUserManagementDbContext db, IPasswordHasher<User> hasher, IConfiguration configuration, TimeProvider clock)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var username = configuration["SeedAdmin:Username"];
        var email = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("SeedAdmin:Username, SeedAdmin:Email, and SeedAdmin:Password must be configured using secure settings.");

        if (await db.Users.AnyAsync(user => user.Role == UserRole.Admin, cancellationToken)) return;
        var now = clock.GetUtcNow().UtcDateTime;
        var admin = new User
        {
            Username = username.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = string.Empty,
            Role = UserRole.Admin,
            CreatedAt = now,
            UpdatedAt = now
        };
        admin.PasswordHash = hasher.HashPassword(admin, password);
        db.Users.Add(admin);
        await db.SaveChangesAsync(cancellationToken);
    }
}
