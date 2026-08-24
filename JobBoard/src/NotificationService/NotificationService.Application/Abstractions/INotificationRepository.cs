using NotificationService.Domain;

namespace NotificationService.Application.Abstractions;

public interface INotificationRepository
{
    Task<(IReadOnlyList<Notification> Items, int Total)> SearchAsync(
        Guid userId,
        NotificationType? type,
        bool? isRead,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> ExistsForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    Task<int> MarkAsReadAsync(Guid id, Guid userId, DateTime readAtUtc, CancellationToken cancellationToken);

    Task<int> MarkAllAsReadAsync(Guid userId, DateTime readAtUtc, CancellationToken cancellationToken);
}
