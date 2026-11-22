using UserManagement.Domain.Entities;

namespace UserManagement.Application.DTOs;

public sealed record UserDto(Guid Id, string Username, string Email, UserRole Role, bool IsActive, Guid? EmployeeId);
public sealed record AuthResponseDto(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresUtc, DateTime RefreshTokenExpiresUtc, UserDto User);
public sealed record SessionDto(Guid Id, Guid UserId, DateTime LoginTimeUtc, DateTime? LogoutTimeUtc, LogoutReason? LogoutReason, string? IpAddress, string? UserAgent, bool IsActive, TimeSpan? SessionDuration);
public sealed record LoginAttemptDto(Guid Id, string UsernameAttempted, Guid? UserId, DateTime AttemptTimeUtc, bool IsSuccessful, string? FailureReason, string? IpAddress, string? UserAgent);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
