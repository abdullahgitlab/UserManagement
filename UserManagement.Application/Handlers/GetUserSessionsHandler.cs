using UserManagement.Application.Abstractions;
using UserManagement.Application.DTOs;
using UserManagement.Application.Exceptions;
using UserManagement.Application.Requests;
using UserManagement.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.Application.Handlers;

public sealed class GetUserSessionsHandler(IUserManagementDbContext db, IRequestContext requestContext) : IRequestHandler<GetUserSessionsQuery, PagedResult<SessionDto>>
{
    public async Task<PagedResult<SessionDto>> Handle(GetUserSessionsQuery request, CancellationToken cancellationToken)
    {
        if (requestContext.Role != nameof(UserRole.Admin) && requestContext.UserId != request.UserId) throw new ForbiddenAccessException();
        var query = db.UserSessions.AsNoTracking().Where(item => item.UserId == request.UserId);
        if (request.FromUtc is not null) query = query.Where(item => item.LoginTimeUtc >= request.FromUtc.Value);
        if (request.ToUtc is not null) query = query.Where(item => item.LoginTimeUtc <= request.ToUtc.Value);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(item => item.LoginTimeUtc).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(item => new SessionDto(item.Id, item.UserId, item.LoginTimeUtc, item.LogoutTimeUtc, item.LogoutReason, item.IpAddress, item.UserAgent, item.IsActive,
                item.LogoutTimeUtc == null ? null : item.LogoutTimeUtc.Value - item.LoginTimeUtc))
            .ToListAsync(cancellationToken);
        return new PagedResult<SessionDto>(items, request.Page, request.PageSize, total);
    }
}
