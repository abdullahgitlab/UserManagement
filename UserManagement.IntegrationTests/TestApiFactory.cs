using System.Data.Common;
using UserManagement.Application.Abstractions;
using UserManagement.Domain.Entities;
using UserManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Microsoft.AspNetCore.Identity;
using UserManagement.Infrastructure.Security;
using UserManagement.Infrastructure.Services;
using UserManagement.Application.Handlers;

namespace UserManagement.IntegrationTests;

internal sealed class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero));

    public TestApiFactory() => _connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = new string('x', 64),
            ["SeedAdmin:Username"] = "admin",
            ["SeedAdmin:Email"] = "admin@example.test",
            ["SeedAdmin:Password"] = "AdminPass1!",
            ["RateLimiting:LoginPermitLimit"] = "100"
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<UserManagementDbContext>>();
            services.RemoveAll<UserManagementDbContext>();
            services.RemoveAll<IUserManagementDbContext>();
            services.AddDbContext<UserManagementDbContext>(options => options.UseSqlite(_connection));
            services.AddScoped<IUserManagementDbContext>(provider => provider.GetRequiredService<UserManagementDbContext>());
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
            services.AddSingleton<ITokenService, JwtTokenService>();
            services.AddScoped<ExpiredSessionsHandler>();
            services.AddScoped<AdminSeedService>();
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
