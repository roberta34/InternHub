using System.Text.RegularExpressions;

namespace InternHub.Api.Features.RecruiterReview;

/// <summary>
/// Finds where skills occur in a CV, deterministically. A skill matches only as a whole term
/// (Java is not found in "JavaScript", C is not found in "C#") and the longest term wins,
/// so SQL is not found inside "SQL Server".
/// </summary>
public static class SkillExcerptLocator
{
    public const int ExcerptRadius = 30;

    private static readonly string[] CompoundTerms =
        ["SQL Server", "ASP.NET Core", "Entity Framework", "Spring Boot"];

    /// <summary>Returns one excerpt per skill found in the CV, keyed case-insensitively by skill.</summary>
    public static IReadOnlyDictionary<string, string> Locate(
        string? cvText,
        IEnumerable<string> skills)
    {
        ArgumentNullException.ThrowIfNull(skills);

        var text = cvText ?? string.Empty;
        var wanted = skills
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Select(skill => skill.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var excerpts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var claimed = new bool[text.Length];
        var vocabulary = wanted
            .Concat(CompoundTerms)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(term => term.Length)
            .ThenBy(term => term, StringComparer.OrdinalIgnoreCase);

        foreach (var term in vocabulary)
        {
            foreach (Match match in TermPattern(term).Matches(text))
            {
                if (claimed.AsSpan(match.Index, match.Length).Contains(true))
                    continue;

                claimed.AsSpan(match.Index, match.Length).Fill(true);

                if (wanted.Contains(term))
                    excerpts.TryAdd(term, Excerpt(text, match));
            }
        }

        return excerpts;
    }

    private static Regex TermPattern(string term) =>
        new(
            $@"(?<![A-Za-z0-9+#]|[A-Za-z0-9]\.){Regex.Escape(term)}(?![A-Za-z0-9+#]|\.[A-Za-z0-9])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));

    private static string Excerpt(string text, Match match)
    {
        var start = Math.Max(0, match.Index - ExcerptRadius);
        var end = Math.Min(text.Length, match.Index + match.Length + ExcerptRadius);
        return text[start..end].Trim();
    }
}
