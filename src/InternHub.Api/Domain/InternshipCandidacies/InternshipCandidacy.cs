namespace InternHub.Api.Domain.InternshipCandidacies;

public sealed class InternshipCandidacy
{
    public Guid Id { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid OfferId { get; private set; }
    public DateTime ApplicationDate { get; private set; }
    public InternshipCandidacyStatus Status { get; private set; }
    public bool IsActive => IsActiveStatus(Status);

    public InternshipCandidacy(
        Guid id,
        Guid studentId,
        Guid offerId,
        DateTime applicationDate,
        InternshipCandidacyStatus status)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id must not be empty.", nameof(id));

        if (studentId == Guid.Empty)
            throw new ArgumentException("StudentId must not be empty.", nameof(studentId));

        if (offerId == Guid.Empty)
            throw new ArgumentException("OfferId must not be empty.", nameof(offerId));

        if (!Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Status must be a defined InternshipCandidacyStatus value.");

        Id = id;
        StudentId = studentId;
        OfferId = offerId;
        ApplicationDate = applicationDate;
        Status = status;
    }

    public static bool IsActiveStatus(InternshipCandidacyStatus status) =>
        status is InternshipCandidacyStatus.Submitted
            or InternshipCandidacyStatus.Shortlisted
            or InternshipCandidacyStatus.Accepted
            or InternshipCandidacyStatus.Waitlisted;

    public bool Shortlist()
    {
        if (Status != InternshipCandidacyStatus.Submitted)
            return false;

        Status = InternshipCandidacyStatus.Shortlisted;
        return true;
    }

    public bool Accept(bool hasAvailableSlot)
    {
        if (!IsOpenForTransition())
            return false;

        Status = hasAvailableSlot
            ? InternshipCandidacyStatus.Accepted
            : InternshipCandidacyStatus.Waitlisted;
        return true;
    }

    public bool Reject()
    {
        if (!IsOpenForTransition())
            return false;

        Status = InternshipCandidacyStatus.Rejected;
        return true;
    }

    public bool Withdraw()
    {
        if (!IsOpenForTransition())
            return false;

        Status = InternshipCandidacyStatus.Withdrawn;
        return true;
    }

    private bool IsOpenForTransition() =>
        Status is InternshipCandidacyStatus.Submitted or InternshipCandidacyStatus.Shortlisted;
}