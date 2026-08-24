using System.Text.Json;
using JobBoard.Contracts;
using NotificationService.Domain;

namespace NotificationService.Application.Mapping;

public static class NotificationFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<Notification> FromApplicationSubmitted(ApplicationSubmittedEvent evt, DateTime utcNow)
    {
        var (title, body) = NotificationTexts.ForApplicationSubmitted(evt.ApplicantName, evt.JobTitle);
        return
        [
            new Notification(
                evt.EmployerUserId,
                RecipientRole.Employer,
                NotificationType.ApplicationSubmitted,
                title,
                body,
                ToJson(new { evt.ApplicationId, evt.JobPostId, evt.ApplicantUserId }),
                utcNow)
        ];
    }

    public static IReadOnlyList<Notification> FromJobPosted(JobPostedEvent evt, DateTime utcNow)
    {
        var (title, body) = NotificationTexts.ForJobPosted(evt.CompanyName, evt.JobTitle);
        var metadata = ToJson(new { evt.JobPostId, evt.EmployerUserId });
        return evt.RecipientUserIds
            .Distinct()
            .Select(recipientId => new Notification(
                recipientId,
                RecipientRole.Employee,
                NotificationType.JobPosted,
                title,
                body,
                metadata,
                utcNow))
            .ToList();
    }

    public static IReadOnlyList<Notification> FromStatusChanged(ApplicationStatusChangedEvent evt, DateTime utcNow)
    {
        var (title, body) = NotificationTexts.ForStatusChanged(evt.NewStatus, evt.JobTitle);
        return
        [
            new Notification(
                evt.CandidateUserId,
                RecipientRole.Employee,
                NotificationType.ApplicationStatusChanged,
                title,
                body,
                ToJson(new { evt.ApplicationId, evt.JobPostId, NewStatus = (int)evt.NewStatus }),
                utcNow)
        ];
    }

    public static string ToJson<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);
}
