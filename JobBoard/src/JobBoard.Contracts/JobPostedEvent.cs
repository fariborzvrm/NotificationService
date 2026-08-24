namespace JobBoard.Contracts;

public record JobPostedEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid JobPostId,
    Guid EmployerUserId,
    string CompanyName,
    string JobTitle,
    Guid[] RecipientUserIds);
