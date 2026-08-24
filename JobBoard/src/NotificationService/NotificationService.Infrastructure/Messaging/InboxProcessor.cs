using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationService.Domain;

namespace NotificationService.Infrastructure.Messaging;

internal static class InboxProcessor
{
    internal static async Task ProcessAsync<T>(
        Persistence.NotificationDbContext db,
        Guid messageId,
        string consumerName,
        Func<T, IReadOnlyList<Notification>> map,
        T message,
        ILogger logger,
        CancellationToken cancellationToken) where T : class
    {
        if (await db.InboxMessages.AnyAsync(m => m.MessageId == messageId, cancellationToken))
        {
            logger.LogWarning(
                "Duplicate message {MessageId} for consumer {Consumer} skipped",
                messageId, consumerName);
            return;
        }

        var notifications = map(message);
        db.Notifications.AddRange(notifications);
        await db.InboxMessages.AddAsync(new InboxMessage(messageId, consumerName, DateTime.UtcNow), cancellationToken);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Persisted {Count} notification(s) for message {MessageId} ({Consumer})",
                notifications.Count, messageId, consumerName);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            logger.LogWarning(
                "Concurrent duplicate for message {MessageId} ({Consumer}); treated as already processed",
                messageId, consumerName);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        for (var current = (Exception?)ex; current is not null; current = current.InnerException)
        {
            if (current is SqlException sqlException && sqlException.Number is 2601 or 2627)
            {
                return true;
            }

            var type = current.GetType();
            if (type.Name == "SqliteException" &&
                type.GetProperty("SqlErrorCode")?.GetValue(current) is int code && code == 19)
            {
                return true;
            }
        }

        return false;
    }
}
