namespace InternHub.Api.Domain.Offers;

using InternHub.Api.Domain.InternshipCandidacies;

public class Offer
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string CompanyName { get; private set; }
    public string Requirements { get; private set; }
    public DateTime Deadline { get; private set; }
    public int MaximumAvailableSlots { get; private set; }
    public bool IsClosed { get; private set; }
    public int Version { get; private set; }

	private readonly List<InternshipCandidacy> _internshipCandidacies = new();

    public IReadOnlyCollection<InternshipCandidacy> InternshipCandidacies =>
        _internshipCandidacies.AsReadOnly();

    public Offer(
        string title,
        string companyName,
        string requirements,
        DateTime deadline,
        int maximumAvailableSlots)
    {
        if(string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        if(string.IsNullOrWhiteSpace(companyName))
            throw new ArgumentException("Company name is required.", nameof(companyName));
        if (string.IsNullOrWhiteSpace(requirements))
            throw new ArgumentException("Requirements are required.", nameof(requirements));
        Id = Guid.NewGuid();
        Title = title;
        CompanyName = companyName;
        Requirements = requirements;
        Deadline = deadline;
        MaximumAvailableSlots = maximumAvailableSlots;
        IsClosed = false;
        Version = 0;
    }

    public void Update(
        string title,
        string companyName,
        string requirements,
        DateTime deadline,
        int maximumAvailableSlots)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        if (string.IsNullOrWhiteSpace(companyName))
            throw new ArgumentException("Company name is required.", nameof(companyName));

        if (string.IsNullOrWhiteSpace(requirements))
            throw new ArgumentException("Requirements are required.", nameof(requirements));

        Title = title;
        CompanyName = companyName;
        Requirements = requirements;
        Deadline = deadline;
        MaximumAvailableSlots = maximumAvailableSlots;

        Version++;
    }

    public void Close()
    {
        if (IsClosed)
            return;

        IsClosed = true;
        Version++;
    }
}