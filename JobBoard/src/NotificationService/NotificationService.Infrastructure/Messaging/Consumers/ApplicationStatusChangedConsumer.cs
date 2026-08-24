using JobBoard.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Mapping;

namespace NotificationService.Infrastructure.Messaging.Consumers;

public sealed class ApplicationStatusChangedConsumer(
    Persistence.NotificationDbContext dbContext,
    ILogger<ApplicationStatusChangedConsumer> logger) : IConsumer<ApplicationStatusChangedEvent>
{
    public Task Consume(ConsumeContext<ApplicationStatusChangedEvent> context)
    {
        return InboxProcessor.ProcessAsync(
            dbContext,
            context.Message.EventId,
            nameof(ApplicationStatusChangedConsumer),
            evt => NotificationFactory.FromStatusChanged(evt, DateTime.UtcNow),
            context.Message,
            logger,
            context.CancellationToken);
    }
}
