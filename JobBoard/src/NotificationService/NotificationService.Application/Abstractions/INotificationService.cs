using NotificationService.Application.Models;
using NotificationService.Domain;

namespace NotificationService.Application.Abstractions;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> SearchAsync(
        Guid userId,
        NotificationType? type,
        bool? isRead,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> MarkAsReadAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken);
}
