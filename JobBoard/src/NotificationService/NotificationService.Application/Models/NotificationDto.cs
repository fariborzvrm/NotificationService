namespace NotificationService.Application.Models;

public sealed record NotificationDto(
    Guid Id,
    Guid RecipientUserId,
    string RecipientRole,
    string Type,
    string Title,
    string Body,
    string Metadata,
    bool IsRead,
    DateTime? ReadAtUtc,
    DateTime CreatedAtUtc);
