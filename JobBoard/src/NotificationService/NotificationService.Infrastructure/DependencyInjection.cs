using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Abstractions;
using NotificationService.Infrastructure.Messaging.Consumers;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultDb")));

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationService, Application.Services.NotificationAppService>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<ApplicationSubmittedConsumer>();
            x.AddConsumer<JobPostedConsumer>();
            x.AddConsumer<ApplicationStatusChangedConsumer>();

            x.AddConfigureEndpointsCallback((_, cfg) =>
                cfg.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15))));

            x.UsingRabbitMq((context, cfg) =>
            {
                var section = configuration.GetSection("RabbitMQ");
                cfg.Host(section["Host"] ?? "localhost", "/", h =>
                {
                    h.Username(section["Username"] ?? "guest");
                    h.Password(section["Password"] ?? "guest");
                });

                cfg.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("notification", false));
            });
        });

        return services;
    }
}
