using InternHub.Api.Features.RecruiterReview;

namespace InternHub.Tests.Unit.RecruiterReview.Fakes;

/// <summary>
/// Deterministic keyword-matching review provider for tests. Required skills are the
/// comma/semicolon/newline separated items of the offer requirements; matching uses
/// <see cref="SkillExcerptLocator"/> (whole terms, longest term wins).
/// </summary>
internal sealed class FakeRecruiterCandidacyReviewService : IRecruiterCandidacyReviewService
{
    private static readonly char[] RequirementSeparators = [',', ';', '\r', '\n'];

    private int _callCount;

    public bool SimulateUnavailable { get; set; }

    public int CallCount => Volatile.Read(ref _callCount);

    public string? LastOfferRequirements { get; private set; }

    public Task<RecruiterCandidacyReview> ReviewAsync(
        string offerRequirements,
        string cvText,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _callCount);
        LastOfferRequirements = offerRequirements;
        cancellationToken.ThrowIfCancellationRequested();

        if (SimulateUnavailable)
        {
            return Task.FromException<RecruiterCandidacyReview>(
                new RecruiterCandidacyReviewProviderException(
                    "The fake recruiter review provider is unavailable."));
        }

        var requiredSkills = (offerRequirements ?? string.Empty)
            .Split(RequirementSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var excerpts = SkillExcerptLocator.Locate(cvText, requiredSkills);
        var matched = requiredSkills.Where(excerpts.ContainsKey).ToList();
        var missing = requiredSkills.Where(skill => !excerpts.ContainsKey(skill)).ToList();
        var evidence = matched.Select(skill => new SkillEvidence(skill, excerpts[skill])).ToList();

        return Task.FromResult(new RecruiterCandidacyReview(
            matched,
            missing,
            evidence,
            $"Matched {matched.Count} of {requiredSkills.Count} required skills."));
    }
}
