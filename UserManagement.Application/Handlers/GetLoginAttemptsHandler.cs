using UserManagement.Application.Abstractions;
using UserManagement.Application.DTOs;
using UserManagement.Application.Exceptions;
using UserManagement.Application.Requests;
using UserManagement.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.Application.Handlers;

public sealed class GetLoginAttemptsHandler(IUserManagementDbContext db, IRequestContext requestContext) : IRequestHandler<GetLoginAttemptsQuery, PagedResult<LoginAttemptDto>>
{
    public async Task<PagedResult<LoginAttemptDto>> Handle(GetLoginAttemptsQuery request, CancellationToken cancellationToken)
    {
        if (requestContext.Role != nameof(UserRole.Admin)) throw new ForbiddenAccessException();
        var query = db.LoginAttempts.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Username)) query = query.Where(item => item.UsernameAttempted.ToLower() == request.Username.Trim().ToLower());
        if (request.IsSuccessful.HasValue) query = query.Where(item => item.IsSuccessful == request.IsSuccessful.Value);
        if (request.FromUtc is not null) query = query.Where(item => item.AttemptTimeUtc >= request.FromUtc.Value);
        if (request.ToUtc is not null) query = query.Where(item => item.AttemptTimeUtc <= request.ToUtc.Value);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(item => item.AttemptTimeUtc).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(item => new LoginAttemptDto(item.Id, item.UsernameAttempted, item.UserId, item.AttemptTimeUtc, item.IsSuccessful, item.FailureReason, item.IpAddress, item.UserAgent))
            .ToListAsync(cancellationToken);
        return new PagedResult<LoginAttemptDto>(items, request.Page, request.PageSize, total);
    }
}
