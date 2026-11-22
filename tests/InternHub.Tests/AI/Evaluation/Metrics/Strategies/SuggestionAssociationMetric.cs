using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Dataset;
using InternHub.Tests.AI.Evaluation.Metrics.Core;

namespace InternHub.Tests.AI.Evaluation.Metrics.Strategies;

/// <summary>Strategy with two Factory Methods (structural): whether suggestions name the reported missing skills, found by whole-term text match.</summary>
public sealed class SuggestionAssociationMetric : ICaseMetric
{
    private const int MaximumTermLength = 40;

    private readonly Func<IReadOnlySet<string>, IReadOnlyList<HashSet<string>>, double> _score;

    private SuggestionAssociationMetric(
        string name,
        Func<IReadOnlySet<string>, IReadOnlyList<HashSet<string>>, double> score)
    {
        Name = name;
        _score = score;
    }

    public string Name { get; }

    public MetricKind Kind => MetricKind.Structural;

    /// <summary>Reported missing skills mentioned in at least one suggestion / reported missing skills; 1 when nothing is missing.</summary>
    public static SuggestionAssociationMetric Coverage() =>
        new(
            "Suggestion coverage",
            (missing, mentions) => Ratio.Divide(
                missing.Count(skill => mentions.Any(mentioned => mentioned.Contains(skill))),
                missing.Count,
                1));

    /// <summary>Suggestions that mention no reported missing skill / suggestions; 0 when there are no suggestions.</summary>
    public static SuggestionAssociationMetric Mismatch() =>
        new(
            "Suggestion mismatch",
            (_, mentions) => Ratio.Divide(
                mentions.Count(mentioned => mentioned.Count == 0),
                mentions.Count,
                0));

    public double Compute(EvaluationCase evaluationCase, RecruiterEvaluationResult result, SkillNormalizer normalizer)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(normalizer);

        var missing = normalizer.ToSet(result.MissingSkills);
        var mentions = result.Suggestions
            .Select(suggestion => MentionedSkills(suggestion ?? string.Empty, missing, normalizer))
            .ToList();

        return _score(missing, mentions);
    }

    // A term is a span with a boundary on both sides: not next to a letter, digit, '+' or '#', and a '.'
    // only counts as a boundary when it is not followed by a letter or digit (".NET" yes, "Node.js" inner "js" no).
    private static HashSet<string> MentionedSkills(string text, IReadOnlySet<string> skills, SkillNormalizer normalizer)
    {
        var mentioned = new HashSet<string>(StringComparer.Ordinal);

        for (var start = 0; start < text.Length; start++)
        {
            if (!IsTermStart(text, start))
                continue;

            var lastEnd = Math.Min(text.Length, start + MaximumTermLength);
            for (var end = start + 1; end <= lastEnd; end++)
            {
                if (!IsTermEnd(text, end))
                    continue;

                var normalized = normalizer.Normalize(text[start..end]);
                if (skills.Contains(normalized))
                    mentioned.Add(normalized);
            }
        }

        return mentioned;
    }

    private static bool IsTermStart(string text, int start) =>
        !char.IsWhiteSpace(text[start]) &&
        (start == 0 || (!IsTermCharacter(text[start - 1]) &&
                        !(text[start - 1] == '.' && char.IsLetterOrDigit(text[start]))));

    private static bool IsTermEnd(string text, int end) =>
        !char.IsWhiteSpace(text[end - 1]) &&
        (end == text.Length || (!IsTermCharacter(text[end]) &&
                                !(text[end] == '.' && end + 1 < text.Length && char.IsLetterOrDigit(text[end + 1]))));

    private static bool IsTermCharacter(char character) =>
        char.IsLetterOrDigit(character) || character is '+' or '#';
}
