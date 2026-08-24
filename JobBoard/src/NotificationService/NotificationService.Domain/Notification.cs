namespace NotificationService.Domain;

public sealed class Notification
{
    public Guid Id { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public RecipientRole RecipientRole { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string Metadata { get; private set; } = "{}";
    public bool IsRead { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Notification()
    {
    }

    public Notification(
        Guid recipientUserId,
        RecipientRole recipientRole,
        NotificationType type,
        string title,
        string body,
        string metadata,
        DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        RecipientUserId = recipientUserId;
        RecipientRole = recipientRole;
        Type = type;
        Title = title;
        Body = body;
        Metadata = metadata;
        CreatedAtUtc = createdAtUtc;
    }

    public void MarkAsRead(DateTime readAtUtc)
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAtUtc = readAtUtc;
    }
}
