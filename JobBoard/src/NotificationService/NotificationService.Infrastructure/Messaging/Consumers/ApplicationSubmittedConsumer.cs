using JobBoard.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Mapping;

namespace NotificationService.Infrastructure.Messaging.Consumers;

public sealed class ApplicationSubmittedConsumer(
    Persistence.NotificationDbContext dbContext,
    ILogger<ApplicationSubmittedConsumer> logger) : IConsumer<ApplicationSubmittedEvent>
{
    public Task Consume(ConsumeContext<ApplicationSubmittedEvent> context)
    {
        return InboxProcessor.ProcessAsync(
            dbContext,
            context.Message.EventId,
            nameof(ApplicationSubmittedConsumer),
            evt => NotificationFactory.FromApplicationSubmitted(evt, DateTime.UtcNow),
            context.Message,
            logger,
            context.CancellationToken);
    }
}
