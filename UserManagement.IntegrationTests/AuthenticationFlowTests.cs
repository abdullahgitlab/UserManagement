using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using UserManagement.Domain.Entities;
using UserManagement.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace UserManagement.IntegrationTests;

public sealed class AuthenticationFlowTests
{
    [Fact]
    public async Task Register_login_protected_request_logout_and_reject_closed_session()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var adminToken = await Login(client, "admin", "AdminPass1!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var registration = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "employee1",
            email = "employee1@example.test",
            password = "Employee1!",
            role = nameof(UserRole.Employee),
            employeeId = (Guid?)null
        });
        registration.StatusCode.Should().Be(HttpStatusCode.Created);

        client.DefaultRequestHeaders.Authorization = null;
        var employeeToken = await Login(client, "employee1", "Employee1!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
        (await client.GetAsync("/api/users/me")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsync("/api/auth/logout", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync("/api/users/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
        var employee = await db.Users.SingleAsync(user => user.Username == "employee1");
        var session = await db.UserSessions.SingleAsync(item => item.UserId == employee.Id);
        session.LogoutTimeUtc.Should().NotBeNull();
        session.LogoutReason.Should().Be(LogoutReason.UserLogout);
    }

    [Fact]
    public async Task Wrong_password_returns_401_and_persists_failed_attempt()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        (await Login(client, "admin", "BadPass1!", expectSuccess: false)).Should().BeEmpty();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
        var attempt = await db.LoginAttempts.SingleAsync();
        attempt.IsSuccessful.Should().BeFalse();
        attempt.FailureReason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Five_failed_logins_lock_account_until_fake_clock_passes_lockout()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var adminToken = await Login(client, "admin", "AdminPass1!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        (await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "lockme", email = "lockme@example.test", password = "Password1!", role = nameof(UserRole.Employee), employeeId = (Guid?)null
        })).StatusCode.Should().Be(HttpStatusCode.Created);
        client.DefaultRequestHeaders.Authorization = null;

        for (var index = 0; index < 5; index++)
            (await client.PostAsJsonAsync("/api/auth/login", new { username = "lockme", password = "WrongPass1!" })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/api/auth/login", new { username = "lockme", password = "Password1!" })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        factory.Clock.Advance(TimeSpan.FromMinutes(15));
        (await client.PostAsJsonAsync("/api/auth/login", new { username = "lockme", password = "Password1!" })).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Protected_endpoint_requires_auth_and_admin_route_forbids_employee_role()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        (await client.GetAsync("/api/users/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var adminToken = await Login(client, "admin", "AdminPass1!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "ordinary", email = "ordinary@example.test", password = "Password1!", role = nameof(UserRole.Employee), employeeId = (Guid?)null
        });
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        client.DefaultRequestHeaders.Authorization = null;
        var employeeToken = await Login(client, "ordinary", "Password1!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
        (await client.PostAsync($"/api/users/{id}/force-logout", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_force_logout_ends_another_users_session()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var adminToken = await Login(client, "admin", "AdminPass1!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var registration = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "target", email = "target@example.test", password = "Password1!", role = nameof(UserRole.Employee), employeeId = (Guid?)null
        });
        var userId = (await registration.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        client.DefaultRequestHeaders.Authorization = null;
        var userToken = await Login(client, "target", "Password1!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        (await client.PostAsync($"/api/users/{userId}/force-logout", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        (await client.GetAsync("/api/users/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<string> Login(HttpClient client, string username, string password, bool expectSuccess = true)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        if (!expectSuccess)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            return string.Empty;
        }
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }
}
