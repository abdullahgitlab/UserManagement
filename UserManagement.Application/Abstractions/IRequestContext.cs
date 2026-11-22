namespace UserManagement.Application.Abstractions;

public interface IRequestContext
{
    Guid? UserId { get; }
    string? Role { get; }
    Guid? SessionId { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}
