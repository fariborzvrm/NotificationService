using NotificationService.Application.Abstractions;
using NotificationService.Application.Models;
using NotificationService.Domain;

namespace NotificationService.Application.Services;

public sealed class NotificationAppService(INotificationRepository repository) : INotificationService
{
    public async Task<PagedResult<NotificationDto>> SearchAsync(
        Guid userId,
        NotificationType? type,
        bool? isRead,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, total) = await repository.SearchAsync(userId, type, isRead, page, pageSize, cancellationToken);

        return new PagedResult<NotificationDto>(
            items.Select(Map).ToList(),
            total,
            page,
            pageSize);
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken)
        => repository.CountUnreadAsync(userId, cancellationToken);

    public async Task<bool> MarkAsReadAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        if (!await repository.ExistsForUserAsync(id, userId, cancellationToken))
        {
            return false;
        }

        await repository.MarkAsReadAsync(id, userId, DateTime.UtcNow, cancellationToken);
        return true;
    }

    public Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken)
        => repository.MarkAllAsReadAsync(userId, DateTime.UtcNow, cancellationToken);

    private static NotificationDto Map(Notification notification)
        => new(
            notification.Id,
            notification.RecipientUserId,
            notification.RecipientRole.ToString(),
            notification.Type.ToString(),
            notification.Title,
            notification.Body,
            notification.Metadata,
            notification.IsRead,
            notification.ReadAtUtc,
            notification.CreatedAtUtc);
}
