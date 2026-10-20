using InternHub.Api.Domain.InternshipCandidacies;

namespace InternHub.Domain.Students;

public sealed class Student
{
    public Guid Id { get; private set; }
    public string FullName { get; private set; }
    public string Email { get; private set; }

    private readonly List<InternshipCandidacy> _internshipCandidacies = new();

    public IReadOnlyCollection<InternshipCandidacy> InternshipCandidacies =>
        _internshipCandidacies.AsReadOnly();

    public Student(
        Guid id,
        string? fullName,
        string? email)
    {
        if (id == Guid.Empty)
            throw new ArgumentException(
                "Id must not be empty.",
                nameof(id));

        Id = id;
        FullName = ValidateName(fullName);
        Email = ValidateEmail(email);
    }

    public void Update(
        string? fullName,
        string? email)
    {
        var name = ValidateName(fullName);
        var mail = ValidateEmail(email);

        FullName = name;
        Email = mail;
    }

    private static string ValidateName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                "Full name is required.",
                nameof(value));

        var v = value.Trim();

        if (v.Length > 200)
            throw new ArgumentException(
                "Full name must be at most 200 characters.",
                nameof(value));

        return v;
    }

    private static string ValidateEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                "Email is required.",
                nameof(value));

        var v = value.Trim();

        if (v.Length > 320 ||
            !System.Net.Mail.MailAddress.TryCreate(v, out var addr) ||
            addr.Address != v)
        {
            throw new ArgumentException(
                "Email is not valid.",
                nameof(value));
        }

        return v.ToLowerInvariant();
    }
}