using JobBoard.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Mapping;

namespace NotificationService.Infrastructure.Messaging.Consumers;

public sealed class JobPostedConsumer(
    Persistence.NotificationDbContext dbContext,
    ILogger<JobPostedConsumer> logger) : IConsumer<JobPostedEvent>
{
    public Task Consume(ConsumeContext<JobPostedEvent> context)
    {
        return InboxProcessor.ProcessAsync(
            dbContext,
            context.Message.EventId,
            nameof(JobPostedConsumer),
            evt => NotificationFactory.FromJobPosted(evt, DateTime.UtcNow),
            context.Message,
            logger,
            context.CancellationToken);
    }
}
