namespace InternHub.Api.Domain.Applications;

public enum ApplicationStatus
{
    Submitted,
    Shortlisted,
    Accepted,
    Rejected,
    Waitlisted,
    Withdrawn
}

public sealed class Application
{
    public Guid Id { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid OfferId { get; private set; }
    public DateTime ApplicationDate { get; private set; }
    public ApplicationStatus Status { get; private set; }

    public Application(
        Guid id,
        Guid studentId,
        Guid offerId,
        DateTime applicationDate)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id must not be empty.", nameof(id));

        if (studentId == Guid.Empty)
            throw new ArgumentException("StudentId must not be empty.", nameof(studentId));

        if (offerId == Guid.Empty)
            throw new ArgumentException("OfferId must not be empty.", nameof(offerId));

        Id = id;
        StudentId = studentId;
        OfferId = offerId;
        ApplicationDate = applicationDate;
        Status = ApplicationStatus.Submitted;
    }
}