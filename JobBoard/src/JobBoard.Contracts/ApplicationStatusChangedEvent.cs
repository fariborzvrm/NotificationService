namespace JobBoard.Contracts;

public record ApplicationStatusChangedEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid ApplicationId,
    Guid JobPostId,
    Guid CandidateUserId,
    Guid EmployerUserId,
    string JobTitle,
    ApplicationStatus NewStatus);
