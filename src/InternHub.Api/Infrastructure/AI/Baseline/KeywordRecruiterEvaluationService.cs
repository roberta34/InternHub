using System.Text.RegularExpressions;
using InternHub.Api.Features.RecruiterEvaluation;

namespace InternHub.Api.Infrastructure.AI.Baseline;

public sealed class KeywordRecruiterEvaluationService : IRecruiterEvaluationService
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private static readonly IReadOnlyList<SkillDefinition> Vocabulary =
    [
        new("Python", ["Python", "Python3", "Python 3"]),
        new("Java", ["Java"]),
        new("JavaScript", ["JavaScript", "JS", "Java Script", "ECMAScript"]),
        new("SQL", ["SQL"]),
        new("C#", ["C#", "CSharp", "C Sharp", "C-Sharp"]),
        new("C++", ["C++", "CPP"]),
        new(".NET", [".NET", "DotNet", "Dot Net", ".NET Core"]),
        new("React", ["React", "ReactJS", "React.js"]),
        new("Docker", ["Docker"]),
        new("Git", ["Git"]),
        new("SQL Server", ["SQL Server", "SQLServer", "MS SQL Server", "Microsoft SQL Server", "MS SQL", "MSSQL"]),
        new("TypeScript", ["TypeScript", "TS", "Type Script"]),
        new("PostgreSQL", ["PostgreSQL", "Postgres", "Postgre SQL"]),
        new("MySQL", ["MySQL"]),
        new("SQLite", ["SQLite"]),
        new("NoSQL", ["NoSQL", "No SQL"]),
        new("MongoDB", ["MongoDB", "Mongo"]),
        new("ASP.NET Core", ["ASP.NET Core", "ASP.NET", "ASPNET Core"]),
        new("Entity Framework Core", ["Entity Framework Core", "EF Core", "Entity Framework"]),
        new("Node.js", ["Node.js", "NodeJS", "Node JS"]),
        new("Angular", ["Angular", "AngularJS"]),
        new("Vue.js", ["Vue.js", "VueJS", "Vue"]),
        new("HTML", ["HTML", "HTML5"]),
        new("CSS", ["CSS", "CSS3"]),
        new("REST API", ["REST API", "RESTful API", "REST APIs", "RESTful APIs", "RESTful"]),
        new("GraphQL", ["GraphQL"]),
        new("Unit Testing", ["Unit Testing", "Unit Tests", "Unit Test", "Unit-Testing"]),
        new("Linux", ["Linux"]),
        new("Kubernetes", ["Kubernetes", "K8s", "Kube"]),
        new("Azure", ["Azure", "Microsoft Azure"]),
        new("AWS", ["AWS", "Amazon Web Services"]),
        new("CI/CD", ["CI/CD", "CICD"]),
        new("Docker Compose", ["Docker Compose", "Docker-Compose"])
    ];

    private static readonly IReadOnlyList<CompiledTermMatcher> TermMatchers = BuildTermMatchers();

    public Task<RecruiterEvaluationResult> AnalyzeAsync(
        string cvContent,
        string offerRequirements,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cvContent);
        ArgumentNullException.ThrowIfNull(offerRequirements);
        cancellationToken.ThrowIfCancellationRequested();

        var requiredSkills = ExtractSkills(offerRequirements, cancellationToken);
        var cvSkills = requiredSkills.Count == 0
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : ExtractSkills(cvContent, cancellationToken)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var matchedSkills = requiredSkills
            .Where(cvSkills.Contains)
            .ToArray();
        var missingSkills = requiredSkills
            .Where(skill => !cvSkills.Contains(skill))
            .ToArray();
        var suggestions = missingSkills
            .Select(skill =>
                $"Highlight or build hands-on experience with {skill} to better align with the internship requirements.")
            .ToArray();

        var summary = CreateSummary(requiredSkills, matchedSkills, missingSkills);
        var result = new RecruiterEvaluationResult(
            matchedSkills,
            missingSkills,
            suggestions,
            summary);

        return Task.FromResult(result);
    }

    private static IReadOnlyList<string> ExtractSkills(
        string content,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        var claimedSpans = new List<(int Start, int End)>();
        var matchedSkillIndices = new HashSet<int>();

        foreach (var matcher in TermMatchers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (Match match in matcher.Pattern.Matches(content))
            {
                if (!match.Success)
                {
                    continue;
                }

                var start = match.Index;
                var end = match.Index + match.Length;

                if (OverlapsClaimedSpan(claimedSpans, start, end))
                {
                    continue;
                }

                claimedSpans.Add((start, end));
                matchedSkillIndices.Add(matcher.VocabularyIndex);
            }
        }

        if (matchedSkillIndices.Count == 0)
        {
            return [];
        }

        var orderedSkills = new List<string>(matchedSkillIndices.Count);
        for (var index = 0; index < Vocabulary.Count; index++)
        {
            if (matchedSkillIndices.Contains(index))
            {
                orderedSkills.Add(Vocabulary[index].CanonicalName);
            }
        }

        return orderedSkills;
    }

    private static bool OverlapsClaimedSpan(
        List<(int Start, int End)> claimedSpans,
        int start,
        int end)
    {
        foreach (var (claimedStart, claimedEnd) in claimedSpans)
        {
            if (start < claimedEnd && claimedStart < end)
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<CompiledTermMatcher> BuildTermMatchers() =>
        Vocabulary
            .SelectMany((definition, vocabularyIndex) => definition.Aliases
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(alias => new CompiledTermMatcher(
                    vocabularyIndex,
                    alias,
                    BuildTermRegex(alias))))
            .OrderByDescending(matcher => matcher.Term.Length)
            .ThenBy(matcher => matcher.VocabularyIndex)
            .ThenBy(matcher => matcher.Term, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static Regex BuildTermRegex(string term)
    {
        var tokens = term.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var escapedTerm = string.Join(@"\s+", tokens.Select(Regex.Escape));
        var pattern = $@"(?<![\p{{L}}\p{{N}}_#+.]){escapedTerm}(?![\p{{L}}\p{{N}}_#+]|\.(?=[\p{{L}}\p{{N}}_]))";

        return new Regex(
            pattern,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
            RegexTimeout);
    }

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

    private sealed record SkillDefinition(
        string CanonicalName,
        IReadOnlyList<string> Aliases);

    private sealed record CompiledTermMatcher(
        int VocabularyIndex,
        string Term,
        Regex Pattern);
}
