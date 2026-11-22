
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Domain.Offers;

public class Offer
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string CompanyName { get; private set; } = string.Empty;
    public string Requirements { get; private set; } = string.Empty;
    public DateTime Deadline { get; private set; }
    public int MaximumAvailableSlots { get; private set; }
    public bool IsClosed { get; private set; }
    public int Version { get; private set; }
    public Guid? RecruiterId { get; private set; }

    private readonly List<InternshipCandidacy> _internshipCandidacies = new();

    public IReadOnlyCollection<InternshipCandidacy> InternshipCandidacies =>
        _internshipCandidacies.AsReadOnly();

    // EF Core
    private Offer()
    {
    }

    public Offer(
        string title,
        string companyName,
        string requirements,
        DateTime deadline,
        int maximumAvailableSlots)
    {
        var error = Validate(title, companyName, requirements, maximumAvailableSlots);
        if (error is not null)
            throw new ArgumentException(error);

        Id = Guid.NewGuid();
        Title = title;
        CompanyName = companyName;
        Requirements = requirements;
        Deadline = deadline;
        MaximumAvailableSlots = maximumAvailableSlots;
        IsClosed = false;
        Version = 0;
        RecruiterId = null;
    }

    public Offer(
        string title,
        string companyName,
        string requirements,
        DateTime deadline,
        int maximumAvailableSlots,
        Guid? recruiterId)
    {
        if (!recruiterId.HasValue || recruiterId == Guid.Empty)
            throw new ArgumentException(
                "A valid authenticated Recruiter is required.",
                nameof(recruiterId));

        var error = Validate(title, companyName, requirements, maximumAvailableSlots);
        if (error is not null)
            throw new ArgumentException(error);

        Id = Guid.NewGuid();
        Title = title;
        CompanyName = companyName;
        Requirements = requirements;
        Deadline = deadline;
        MaximumAvailableSlots = maximumAvailableSlots;
        IsClosed = false;
        Version = 0;
        RecruiterId = recruiterId;
    }

    public static Result<Offer> Create(
        string title,
        string companyName,
        string requirements,
        DateTime deadline,
        int maximumAvailableSlots,
        Guid? recruiterId = null)
    {
        if (!recruiterId.HasValue || recruiterId == Guid.Empty)
            return Result<Offer>.Unauthorized(
                "A valid authenticated Recruiter is required.");

        var error = Validate(
            title,
            companyName,
            requirements,
            maximumAvailableSlots);

        if (error is not null)
            return Result<Offer>.Validation(error);

        var offer = new Offer(
            title,
            companyName,
            requirements,
            deadline,
            maximumAvailableSlots)
        {
            RecruiterId = recruiterId
        };

        return Result<Offer>.Success(offer);
    }

    public void Update(
        string title,
        string companyName,
        string requirements,
        DateTime deadline,
        int maximumAvailableSlots)
    {
        var error = Validate(
            title,
            companyName,
            requirements,
            maximumAvailableSlots);

        if (error is not null)
            throw new ArgumentException(error);

        Title = title;
        CompanyName = companyName;
        Requirements = requirements;
        Deadline = deadline;
        MaximumAvailableSlots = maximumAvailableSlots;

        Version++;

    }

    public Result<Offer> Update(
        string title,
        string companyName,
        string requirements,
        DateTime deadline,
        int maximumAvailableSlots,
        int acceptedCount)
    {
        var error = Validate(
            title,
            companyName,
            requirements,
            maximumAvailableSlots);

        if (error is not null)
            return Result<Offer>.Validation(error);

        if (maximumAvailableSlots < acceptedCount)
            return Result<Offer>.Conflict(
                $"Maximum available slots cannot be lower than the accepted candidacy count ({acceptedCount}).");

        Update(
            title,
            companyName,
            requirements,
            deadline,
            maximumAvailableSlots);

        return Result<Offer>.Success(this);
    }

    private static string? Validate(
        string title,
        string companyName,
        string requirements,
        int maximumAvailableSlots)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "Title is required.";

        if (string.IsNullOrWhiteSpace(companyName))
            return "Company name is required.";

        if (string.IsNullOrWhiteSpace(requirements))
            return "Requirements are required.";

        if (maximumAvailableSlots <= 0)
            return "Maximum available slots must be greater than 0.";

        return null;
    }

    public void Close()
    {
        if (IsClosed)
            return;

        IsClosed = true;
        Version++;
    }

    public void RegisterAcceptedSlot()
    {
        Version++;
    }
}
