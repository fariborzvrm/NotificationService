using JobBoard.Contracts;
using NotificationService.Domain;

namespace NotificationService.Application.Mapping;

public static class NotificationTexts
{
    public static (string Title, string Body) ForApplicationSubmitted(string applicantName, string jobTitle)
        => ("New job application",
            $"{applicantName} applied to your posting '{jobTitle}'.");

    public static (string Title, string Body) ForJobPosted(string companyName, string jobTitle)
        => ("New job posted",
            $"{companyName} is hiring: '{jobTitle}'. Take a look and apply.");

    public static (string Title, string Body) ForStatusChanged(ApplicationStatus status, string jobTitle)
        => status switch
        {
            ApplicationStatus.Seen => ("Your resume was seen",
                $"A recruiter viewed your application for '{jobTitle}'."),
            ApplicationStatus.ResumeAccepted => ("You passed the screening",
                $"Your resume was accepted for '{jobTitle}'."),
            ApplicationStatus.ResumeRejected => ("Application update",
                $"Unfortunately, you were not selected for '{jobTitle}'."),
            ApplicationStatus.InterviewScheduled => ("Interview invitation",
                $"You are invited to interview for '{jobTitle}'."),
            ApplicationStatus.Hired => ("You're hired!",
                $"Congratulations! You have been hired for '{jobTitle}'."),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown application status.")
        };
}
