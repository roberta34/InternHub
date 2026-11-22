namespace InternHub.Tests.AI.Evaluation.Metrics.Core;

/// <summary>Normalizes skills (trim, lowercase, alias lookup) so predictions and labels are compared as sets.</summary>
public sealed class SkillNormalizer
{
    private readonly IReadOnlyDictionary<string, string> _aliases;

    public SkillNormalizer(IReadOnlyDictionary<string, string> aliases)
    {
        ArgumentNullException.ThrowIfNull(aliases);

        _aliases = aliases.ToDictionary(
            alias => Clean(alias.Key),
            alias => Clean(alias.Value),
            StringComparer.Ordinal);
    }

    public string Normalize(string skill)
    {
        ArgumentNullException.ThrowIfNull(skill);

        var cleaned = Clean(skill);
        return _aliases.TryGetValue(cleaned, out var canonical) ? canonical : cleaned;
    }

    public HashSet<string> ToSet(IEnumerable<string> skills)
    {
        ArgumentNullException.ThrowIfNull(skills);

        return skills.Select(Normalize).ToHashSet(StringComparer.Ordinal);
    }

    private static string Clean(string value) => value.Trim().ToLowerInvariant();
}
