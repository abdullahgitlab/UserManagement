using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using UserManagement.Application.Abstractions;
using UserManagement.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace UserManagement.Infrastructure.Security;

public sealed class JwtTokenService(IConfiguration configuration) : ITokenService
{
    public string CreateAccessToken(User user, Guid sessionId, DateTime issuedAtUtc)
    {
        var key = GetSigningKey();
        var issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is required.");
        var audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is required.");
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("sid", sessionId.ToString())
        };
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, issuedAtUtc, issuedAtUtc.AddMinutes(15), credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string CreateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public string HashRefreshToken(string token) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public SymmetricSecurityKey GetSigningKey()
    {
        var secret = configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey must be supplied through user-secrets or a secure configuration provider.");
        var bytes = Encoding.UTF8.GetBytes(secret);
        if (bytes.Length < 32) throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 bytes.");
        return new SymmetricSecurityKey(bytes);
    }
}
