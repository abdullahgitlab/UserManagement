using UserManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.Application.Abstractions;

public interface IUserManagementDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserSession> UserSessions { get; }
    DbSet<LoginAttempt> LoginAttempts { get; }
    DbSet<UsedRefreshToken> UsedRefreshTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
