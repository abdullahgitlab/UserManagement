namespace UserManagement.Domain.Entities;

public sealed class LoginAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string UsernameAttempted { get; set; }
    public Guid? UserId { get; set; }
    public DateTime AttemptTimeUtc { get; set; }
    public bool IsSuccessful { get; set; }
    public string? FailureReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
