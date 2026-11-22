using UserManagement.Application.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace UserManagement.Infrastructure.Services;

public sealed class ExpiredSessionCleanupService(IServiceScopeFactory scopeFactory, TimeProvider clock) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1), clock);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<ExpiredSessionsHandler>();
            await handler.CloseExpiredAsync(clock.GetUtcNow().UtcDateTime, stoppingToken);
        }
    }
}
