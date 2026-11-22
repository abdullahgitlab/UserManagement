using UserManagement.Domain.Entities;

namespace UserManagement.Application.Abstractions;

public interface ITokenService
{
    string CreateAccessToken(User user, Guid sessionId, DateTime issuedAtUtc);
    string CreateRefreshToken();
    string HashRefreshToken(string token);
}
