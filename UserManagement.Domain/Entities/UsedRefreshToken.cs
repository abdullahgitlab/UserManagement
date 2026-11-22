namespace UserManagement.Domain.Entities;

public sealed class UsedRefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserSessionId { get; set; }
    public required string TokenHash { get; set; }
    public DateTime UsedAtUtc { get; set; }
}
