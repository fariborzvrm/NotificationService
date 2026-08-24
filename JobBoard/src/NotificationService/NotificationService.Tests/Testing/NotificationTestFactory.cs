using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MassTransit;
using MassTransit.Testing;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Services;
using NotificationService.Infrastructure.Messaging.Consumers;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Tests.Testing;

public sealed class NotificationTestFactory : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    private NotificationTestFactory(SqliteConnection connection, ServiceProvider provider)
    {
        _connection = connection;
        _provider = provider;
    }

    public IBus Bus => _provider.GetRequiredService<IBus>();

    public ITestHarness Harness => _provider.GetRequiredService<ITestHarness>();

    public static async Task<NotificationTestFactory> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();

        services.AddDbContext<NotificationDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationService, NotificationAppService>();

        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<ApplicationSubmittedConsumer>();
            x.AddConsumer<JobPostedConsumer>();
            x.AddConsumer<ApplicationStatusChangedConsumer>();

            x.UsingInMemory((context, configurator) => configurator.ConfigureEndpoints(context));
        });

        var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        var factory = new NotificationTestFactory(connection, provider);
        await factory.Harness.Start();
        return factory;
    }

    public IServiceScope CreateScope() => _provider.CreateScope();

    public async Task<int> CountNotificationsAsync()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        return await db.Notifications.CountAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
