using UserManagement.Application.Abstractions;
using UserManagement.Application.DTOs;
using UserManagement.Application.Exceptions;
using UserManagement.Application.Requests;
using UserManagement.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.Application.Handlers;

public sealed class RegisterUserHandler(IUserManagementDbContext db, IPasswordHasher<User> hasher, IRequestContext requestContext, TimeProvider clock)
    : IRequestHandler<RegisterUserCommand, UserDto>
{
    public async Task<UserDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (requestContext.Role != nameof(UserRole.Admin)) throw new ForbiddenAccessException();
        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(user => user.Username.ToLower() == username.ToLower() || user.Email == email, cancellationToken))
            throw new ConflictException("Username or email is already registered.");

        var now = clock.GetUtcNow().UtcDateTime;
        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = string.Empty,
            Role = request.Role,
            EmployeeId = request.EmployeeId,
            CreatedAt = now,
            UpdatedAt = now
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return new UserDto(user.Id, user.Username, user.Email, user.Role, user.IsActive, user.EmployeeId);
    }
}
