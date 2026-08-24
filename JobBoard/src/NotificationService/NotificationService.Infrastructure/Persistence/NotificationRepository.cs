using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Abstractions;
using NotificationService.Domain;

namespace NotificationService.Infrastructure.Persistence;

public sealed class NotificationRepository(NotificationDbContext dbContext) : INotificationRepository
{
    public async Task<(IReadOnlyList<Notification> Items, int Total)> SearchAsync(
        Guid userId,
        NotificationType? type,
        bool? isRead,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Notifications.Where(n => n.RecipientUserId == userId);

        if (type.HasValue)
        {
            query = query.Where(n => n.Type == type.Value);
        }

        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken)
        => dbContext.Notifications.CountAsync(
            n => n.RecipientUserId == userId && !n.IsRead, cancellationToken);

    public Task<bool> ExistsForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken)
        => dbContext.Notifications.AnyAsync(
            n => n.Id == id && n.RecipientUserId == userId, cancellationToken);

    public async Task<int> MarkAsReadAsync(Guid id, Guid userId, DateTime readAtUtc, CancellationToken cancellationToken)
        => await dbContext.Notifications
            .Where(n => n.Id == id && n.RecipientUserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAtUtc, (DateTime?)readAtUtc), cancellationToken);

    public Task<int> MarkAllAsReadAsync(Guid userId, DateTime readAtUtc, CancellationToken cancellationToken)
        => dbContext.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAtUtc, (DateTime?)readAtUtc), cancellationToken);
}
