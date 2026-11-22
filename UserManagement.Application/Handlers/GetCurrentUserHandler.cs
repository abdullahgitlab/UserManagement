using UserManagement.Application.Abstractions;
using UserManagement.Application.DTOs;
using UserManagement.Application.Exceptions;
using UserManagement.Application.Requests;
using UserManagement.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.Application.Handlers;

public sealed class GetCurrentUserHandler(IUserManagementDbContext db, IRequestContext requestContext) : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (requestContext.UserId is not Guid userId) throw new ForbiddenAccessException();
        var user = await db.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");
        return new UserDto(user.Id, user.Username, user.Email, user.Role, user.IsActive, user.EmployeeId);
    }
}
