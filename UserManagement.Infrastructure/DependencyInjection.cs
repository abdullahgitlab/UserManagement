using UserManagement.Application.Abstractions;
using UserManagement.Application.Handlers;
using UserManagement.Domain.Entities;
using UserManagement.Infrastructure.Persistence;
using UserManagement.Infrastructure.Security;
using UserManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace UserManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("UserManagementDB")
            ?? throw new InvalidOperationException("ConnectionStrings:UserManagementDB must be configured.");
        services.AddDbContext<UserManagementDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IUserManagementDbContext>(provider => provider.GetRequiredService<UserManagementDbContext>());
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ExpiredSessionsHandler>();
        services.AddScoped<AdminSeedService>();
        services.AddHostedService<ExpiredSessionCleanupService>();
        return services;
    }
}
