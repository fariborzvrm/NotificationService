namespace JobBoard.Contracts;

public record ApplicationSubmittedEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid ApplicationId,
    Guid JobPostId,
    Guid EmployerUserId,
    Guid ApplicantUserId,
    string ApplicantName,
    string JobTitle);
