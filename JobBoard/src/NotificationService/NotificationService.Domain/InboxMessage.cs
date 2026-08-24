namespace NotificationService.Domain;

public sealed class InboxMessage
{
    public Guid MessageId { get; private set; }
    public string Consumer { get; private set; } = string.Empty;
    public DateTime ProcessedOnUtc { get; private set; }

    private InboxMessage()
    {
    }

    public InboxMessage(Guid messageId, string consumer, DateTime processedOnUtc)
    {
        MessageId = messageId;
        Consumer = consumer;
        ProcessedOnUtc = processedOnUtc;
    }
}
