using System.Text.RegularExpressions;
using InternHub.Api.Features.RecruiterEvaluation;

namespace InternHub.Api.Infrastructure.AI.Baseline;

public sealed class KeywordRecruiterEvaluationService : IRecruiterEvaluationService
{
    private static readonly IReadOnlyList<string> SkillVocabulary =
    [
        "Python",
        "Java",
        "JavaScript",
        "SQL",
        "C#",
        "C++",
        ".NET",
        "React",
        "Docker",
        "Git"
    ];

    public Task<RecruiterEvaluationResult> AnalyzeAsync(
        string cvContent,
        string offerRequirements,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cvContent);
        ArgumentNullException.ThrowIfNull(offerRequirements);
        cancellationToken.ThrowIfCancellationRequested();

        var requiredSkills = FindSkills(offerRequirements);
        var matchedSkills = requiredSkills
            .Where(skill => ContainsSkill(cvContent, skill))
            .ToArray();
        var missingSkills = requiredSkills
            .Except(matchedSkills, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var suggestions = missingSkills
            .Select(skill => $"Consider gaining experience with {skill}.")
            .ToArray();

        var summary = CreateSummary(requiredSkills, matchedSkills, missingSkills);
        var result = new RecruiterEvaluationResult(
            matchedSkills,
            missingSkills,
            suggestions,
            summary);

        return Task.FromResult(result);
    }

    private static IReadOnlyList<string> FindSkills(string content) =>
        SkillVocabulary
            .Where(skill => ContainsSkill(content, skill))
            .ToArray();

    private static bool ContainsSkill(string content, string skill) =>
        Regex.IsMatch(
            content,
            $@"(?<![\p{{L}}\p{{N}}_]){Regex.Escape(skill)}(?![\p{{L}}\p{{N}}_])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static string CreateSummary(
        IReadOnlyCollection<string> requiredSkills,
        IReadOnlyCollection<string> matchedSkills,
        IReadOnlyCollection<string> missingSkills)
    {
        if (requiredSkills.Count == 0)
        {
            return "No recognized skills were found in the internship requirements.";
        }

        if (missingSkills.Count == 0)
        {
            return $"Matched all {requiredSkills.Count} recognized required skill(s).";
        }

        return $"Matched {matchedSkills.Count} of {requiredSkills.Count} recognized required skill(s); " +
               $"{missingSkills.Count} skill(s) are missing.";
    }
}
