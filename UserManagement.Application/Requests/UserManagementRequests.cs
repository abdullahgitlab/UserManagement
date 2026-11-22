using UserManagement.Application.DTOs;
using UserManagement.Domain.Entities;
using MediatR;

namespace UserManagement.Application.Requests;

public sealed record RegisterUserCommand(string Username, string Email, string Password, UserRole Role, Guid? EmployeeId) : IRequest<UserDto>;
public sealed record LoginCommand(string Username, string Password) : IRequest<AuthResponseDto>;
public sealed record LogoutCommand : IRequest<bool>;
public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponseDto>;
public sealed record ForceLogoutUserCommand(Guid UserId) : IRequest<int>;
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<bool>;
public sealed record GetCurrentUserQuery : IRequest<UserDto>;
public sealed record GetUserSessionsQuery(Guid UserId, DateTime? FromUtc, DateTime? ToUtc, int Page = 1, int PageSize = 20) : IRequest<PagedResult<SessionDto>>;
public sealed record GetActiveSessionsQuery : IRequest<IReadOnlyList<SessionDto>>;
public sealed record GetLoginAttemptsQuery(string? Username, bool? IsSuccessful, DateTime? FromUtc, DateTime? ToUtc, int Page = 1, int PageSize = 50) : IRequest<PagedResult<LoginAttemptDto>>;
