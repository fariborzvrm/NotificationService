using JobBoard.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Abstractions;
using NotificationService.Domain;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Tests.Testing;
using Xunit;

namespace NotificationService.Tests;

public sealed class NotificationCommandTests
{
    [Fact]
    public async Task UnreadCount_MarkAsRead_AndIdempotentRemark()
    {
        var factory = await NotificationTestFactory.CreateAsync();
        await using var _ = factory;

        var userId = Guid.NewGuid();
        var (id1, _) = await SeedAsync(factory, userId, 2);

        using (var scope = factory.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<INotificationService>();

            Assert.Equal(2, await service.GetUnreadCountAsync(userId, CancellationToken.None));
            Assert.True(await service.MarkAsReadAsync(id1, userId, CancellationToken.None));

            Assert.Equal(1, await service.GetUnreadCountAsync(userId, CancellationToken.None));

            Assert.True(await service.MarkAsReadAsync(id1, userId, CancellationToken.None));
            Assert.Equal(1, await service.GetUnreadCountAsync(userId, CancellationToken.None));
        }
    }

    [Fact]
    public async Task MarkAsRead_OtherUsersNotification_ReturnsFalseAndChangesNothing()
    {
        var factory = await NotificationTestFactory.CreateAsync();
        await using var _ = factory;

        var owner = Guid.NewGuid();
        var stranger = Guid.NewGuid();
        var (id1, _) = await SeedAsync(factory, owner, 1);

        using (var scope = factory.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<INotificationService>();

            Assert.False(await service.MarkAsReadAsync(id1, stranger, CancellationToken.None));

            var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
            var notification = await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == id1);
            Assert.False(notification.IsRead);
            Assert.Equal(1, await service.GetUnreadCountAsync(owner, CancellationToken.None));
        }
    }

    [Fact]
    public async Task MarkAllAsRead_ClearsEveryUnreadForUser()
    {
        var factory = await NotificationTestFactory.CreateAsync();
        await using var _ = factory;

        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await SeedAsync(factory, userA, 3);
        await SeedAsync(factory, userB, 2);

        using (var scope = factory.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var updated = await service.MarkAllAsReadAsync(userA, CancellationToken.None);
            Assert.Equal(3, updated);

            Assert.Equal(0, await service.GetUnreadCountAsync(userA, CancellationToken.None));
            Assert.Equal(2, await service.GetUnreadCountAsync(userB, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Search_FiltersByTypeAndIsRead_WithPaging()
    {
        var factory = await NotificationTestFactory.CreateAsync();
        await using var _ = factory;

        var userId = Guid.NewGuid();

        using (var seedScope = factory.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<NotificationDbContext>();

            var readJobPost = new Notification(
                userId, RecipientRole.Employee, NotificationType.JobPosted,
                "job old read", "body", "{}", DateTime.UtcNow.AddMinutes(-20));
            readJobPost.MarkAsRead(DateTime.UtcNow);

            var toSeed = new List<Notification>
            {
                new(userId, RecipientRole.Employee, NotificationType.JobPosted,
                    "job", "body", "{}", DateTime.UtcNow.AddMinutes(-30)),
                readJobPost,
                new(userId, RecipientRole.Employee, NotificationType.ApplicationSubmitted,
                    "application unread", "body", "{}", DateTime.UtcNow.AddMinutes(-10)),
                new(Guid.NewGuid(), RecipientRole.Employer, NotificationType.JobPosted,
                    "someone else", "body", "{}", DateTime.UtcNow.AddMinutes(-5))
            };

            db.Notifications.AddRange(toSeed);
            await db.SaveChangesAsync();
        }

        using (var scope = factory.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var all = await service.SearchAsync(userId, null, null, 1, 20, CancellationToken.None);
            Assert.Equal(3, all.Total);

            var jobPostedUnread = await service.SearchAsync(
                userId, NotificationType.JobPosted, false, 1, 20, CancellationToken.None);
            Assert.Equal(1, jobPostedUnread.Total);
            Assert.Equal("job", jobPostedUnread.Items.Single().Title);

            var firstPage = await service.SearchAsync(userId, null, null, 1, 2, CancellationToken.None);
            Assert.Equal(3, firstPage.Total);
            Assert.Equal(2, firstPage.Items.Count);
            Assert.Equal("application unread", firstPage.Items.First().Title);

            var secondPage = await service.SearchAsync(userId, null, null, 2, 2, CancellationToken.None);
            Assert.Single(secondPage.Items);
        }
    }

    private static async Task<(Guid FirstId, IReadOnlyList<Guid> AllIds)> SeedAsync(
        NotificationTestFactory factory,
        Guid userId,
        int count)
    {
        using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var ids = new List<Guid>();
        for (var i = 0; i < count; i++)
        {
            var notification = new Notification(
                userId,
                RecipientRole.Employee,
                NotificationType.ApplicationStatusChanged,
                $"t{i}",
                $"b{i}",
                "{}",
                DateTime.UtcNow.AddMinutes(-i));
            db.Notifications.Add(notification);
            ids.Add(notification.Id);
        }

        await db.SaveChangesAsync();
        return (ids[0], ids);
    }
}
