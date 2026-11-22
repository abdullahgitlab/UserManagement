namespace UserManagement.Domain.Entities;

public sealed class UserSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime LoginTimeUtc { get; set; }
    public DateTime? LogoutTimeUtc { get; set; }
    public LogoutReason? LogoutReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public required string RefreshTokenHash { get; set; }
    public DateTime RefreshTokenExpiresUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public TimeSpan? SessionDuration => LogoutTimeUtc - LoginTimeUtc;
}
