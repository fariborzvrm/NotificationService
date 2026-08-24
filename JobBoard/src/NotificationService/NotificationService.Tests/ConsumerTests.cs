using JobBoard.Contracts;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Domain;
using NotificationService.Infrastructure.Messaging.Consumers;
using NotificationService.Tests.Testing;
using Xunit;

namespace NotificationService.Tests;

public sealed class ConsumerTests
{
    [Fact]
    public async Task ApplicationSubmitted_CreatesOneNotificationForEmployerAndInboxRow()
    {
        var factory = await NotificationTestFactory.CreateAsync();
        await using var _ = factory;

        var evt = new ApplicationSubmittedEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(),
            EmployerUserId: Guid.NewGuid(), ApplicantUserId: Guid.NewGuid(),
            ApplicantName: "Dana Developer", JobTitle: "Backend Engineer");

        await factory.Bus.Publish(evt);

        Assert.True(await factory.Harness.Consumed.Any<ApplicationSubmittedEvent>());

        using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.NotificationDbContext>();

        var notifications = await db.Notifications.AsNoTracking().ToListAsync();
        var inbox = await db.InboxMessages.AsNoTracking().ToListAsync();

        var notification = Assert.Single(notifications);
        Assert.Equal(evt.EmployerUserId, notification.RecipientUserId);
        Assert.Equal(RecipientRole.Employer, notification.RecipientRole);
        Assert.Equal(NotificationType.ApplicationSubmitted, notification.Type);

        var (expectedTitle, expectedBody) = Application.Mapping.NotificationTexts.ForApplicationSubmitted(
            evt.ApplicantName, evt.JobTitle);
        Assert.Equal(expectedTitle, notification.Title);
        Assert.Equal(expectedBody, notification.Body);
        Assert.False(notification.IsRead);

        var message = Assert.Single(inbox);
        Assert.Equal(evt.EventId, message.MessageId);
        Assert.Equal(nameof(ApplicationSubmittedConsumer), message.Consumer);
    }

    [Fact]
    public async Task JobPosted_FansOutOneNotificationPerRecipient()
    {
        var factory = await NotificationTestFactory.CreateAsync();
        await using var _ = factory;

        var recipients = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var evt = new JobPostedEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(),
            "Acme Corp", "Senior .NET Engineer", recipients);

        await factory.Bus.Publish(evt);

        Assert.True(await factory.Harness.Consumed.Any<JobPostedEvent>());

        using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.NotificationDbContext>();

        var notifications = await db.Notifications.AsNoTracking().ToListAsync();
        Assert.Equal(3, notifications.Count);
        Assert.All(notifications, n =>
        {
            Assert.Equal(RecipientRole.Employee, n.RecipientRole);
            Assert.Equal(NotificationType.JobPosted, n.Type);
            Assert.Contains(recipients, r => r == n.RecipientUserId);
        });
        Assert.Single(await db.InboxMessages.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task JobPosted_NoRecipients_PersistsInboxOnly()
    {
        var factory = await NotificationTestFactory.CreateAsync();
        await using var _ = factory;

        var evt = new JobPostedEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(),
            "Acme Corp", "Senior .NET Engineer", Array.Empty<Guid>());

        await factory.Bus.Publish(evt);

        Assert.True(await factory.Harness.Consumed.Any<JobPostedEvent>());

        using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.NotificationDbContext>();

        Assert.Empty(await db.Notifications.AsNoTracking().ToListAsync());
        Assert.Single(await db.InboxMessages.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task RedeliveredEventId_IsConsumedTwiceButPersistsOnce()
    {
        var factory = await NotificationTestFactory.CreateAsync();
        await using var _ = factory;

        var evt = new ApplicationStatusChangedEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(),
            CandidateUserId: Guid.NewGuid(), EmployerUserId: Guid.NewGuid(),
            JobTitle: "Backend Engineer", NewStatus: ApplicationStatus.Seen);

        await factory.Bus.Publish(evt);
        await factory.Bus.Publish(evt);

        Assert.True(await factory.Harness.Consumed.Any<ApplicationStatusChangedEvent>());

        var deadline = DateTime.UtcNow.AddSeconds(5);
        var consumedCount = 0;
        while (DateTime.UtcNow < deadline)
        {
            consumedCount = 0;
            await foreach (var _message in factory.Harness.Consumed.SelectAsync<ApplicationStatusChangedEvent>())
            {
                consumedCount++;
            }

            if (consumedCount >= 2)
            {
                break;
            }

            await Task.Delay(50);
        }

        Assert.Equal(2, consumedCount);

        using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.NotificationDbContext>();

        Assert.Single(await db.Notifications.AsNoTracking().ToListAsync());
        Assert.Single(await db.InboxMessages.AsNoTracking().ToListAsync());
    }

    [Theory]
    [InlineData(ApplicationStatus.Seen)]
    [InlineData(ApplicationStatus.ResumeAccepted)]
    [InlineData(ApplicationStatus.ResumeRejected)]
    [InlineData(ApplicationStatus.InterviewScheduled)]
    [InlineData(ApplicationStatus.Hired)]
    public async Task StatusChanged_EachStatusProducesItsOwnText(ApplicationStatus status)
    {
        var factory = await NotificationTestFactory.CreateAsync();
        await using var _ = factory;

        var candidateId = Guid.NewGuid();
        var evt = new ApplicationStatusChangedEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(),
            candidateId, Guid.NewGuid(), "Backend Engineer", status);

        await factory.Bus.Publish(evt);

        Assert.True(await factory.Harness.Consumed.Any<ApplicationStatusChangedEvent>());

        using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.NotificationDbContext>();

        var notifications = await db.Notifications.AsNoTracking().ToListAsync();
        var notification = Assert.Single(notifications);

        Assert.Equal(candidateId, notification.RecipientUserId);
        Assert.Equal(RecipientRole.Employee, notification.RecipientRole);
        Assert.Equal(NotificationType.ApplicationStatusChanged, notification.Type);

        var (expectedTitle, expectedBody) = Application.Mapping.NotificationTexts.ForStatusChanged(status, "Backend Engineer");
        Assert.Equal(expectedTitle, notification.Title);
        Assert.Equal(expectedBody, notification.Body);
    }
}
