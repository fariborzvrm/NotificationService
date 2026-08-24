using JobBoard.Contracts;
using NotificationService.Application.Mapping;
using NotificationService.Domain;
using Xunit;

namespace NotificationService.Tests;

public sealed class MappingTests
{
    [Fact]
    public void StatusTexts_AreDistinctPerStatus()
    {
        var statuses = Enum.GetValues<ApplicationStatus>();

        var texts = statuses
            .Select(s => NotificationTexts.ForStatusChanged(s, "Backend Engineer"))
            .ToList();

        Assert.Equal(statuses.Length, texts.Count);
        Assert.Equal(texts.Count, texts.Select(t => t.Title).Distinct().Count());
        Assert.Equal(texts.Count, texts.Select(t => t.Body).Distinct().Count());
    }

    [Fact]
    public void FromJobPosted_CreatesOneNotificationPerDistinctRecipient()
    {
        var recipientA = Guid.NewGuid();
        var evt = new JobPostedEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(),
            "Acme Corp", "Senior .NET Engineer", [recipientA, recipientA, Guid.NewGuid()]);

        var notifications = NotificationFactory.FromJobPosted(evt, DateTime.UtcNow);

        Assert.Equal(2, notifications.Count);
        Assert.All(notifications, n =>
        {
            Assert.Equal(RecipientRole.Employee, n.RecipientRole);
            Assert.Equal(NotificationType.JobPosted, n.Type);
            Assert.False(n.IsRead);
            Assert.Contains("Acme Corp", n.Body);
        });
    }

    [Fact]
    public void FromApplicationSubmitted_TargetsEmployerWithMetadata()
    {
        var evt = new ApplicationSubmittedEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(),
            EmployerUserId: Guid.NewGuid(), ApplicantUserId: Guid.NewGuid(),
            "Dana Developer", "Backend Engineer");

        var notification = NotificationFactory.FromApplicationSubmitted(evt, DateTime.UtcNow).Single();

        Assert.Equal(evt.EmployerUserId, notification.RecipientUserId);
        Assert.Equal(RecipientRole.Employer, notification.RecipientRole);
        Assert.Contains(evt.ApplicationId.ToString(), notification.Metadata);
        Assert.Contains(evt.JobPostId.ToString(), notification.Metadata);
    }

    [Fact]
    public void FromStatusChanged_MetadataCarriesNumericStatus()
    {
        var evt = new ApplicationStatusChangedEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), "Backend Engineer", ApplicationStatus.Hired);

        var notification = NotificationFactory.FromStatusChanged(evt, DateTime.UtcNow).Single();

        Assert.Contains("\"newStatus\":5", notification.Metadata);
    }
}
