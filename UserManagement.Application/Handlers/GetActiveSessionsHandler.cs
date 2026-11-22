using UserManagement.Application.Abstractions;
using UserManagement.Application.DTOs;
using UserManagement.Application.Exceptions;
using UserManagement.Application.Requests;
using UserManagement.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.Application.Handlers;

public sealed class GetActiveSessionsHandler(IUserManagementDbContext db, IRequestContext requestContext) : IRequestHandler<GetActiveSessionsQuery, IReadOnlyList<SessionDto>>
{
    public async Task<IReadOnlyList<SessionDto>> Handle(GetActiveSessionsQuery request, CancellationToken cancellationToken)
    {
        if (requestContext.Role != nameof(UserRole.Admin)) throw new ForbiddenAccessException();
        return await db.UserSessions.AsNoTracking().Where(item => item.IsActive)
            .OrderByDescending(item => item.LoginTimeUtc)
            .Select(item => new SessionDto(item.Id, item.UserId, item.LoginTimeUtc, item.LogoutTimeUtc, item.LogoutReason, item.IpAddress, item.UserAgent, item.IsActive, null))
            .ToListAsync(cancellationToken);
    }
}
