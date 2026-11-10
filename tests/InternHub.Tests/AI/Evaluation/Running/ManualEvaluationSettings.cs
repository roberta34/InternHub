using System.Globalization;

namespace InternHub.Tests.AI.Evaluation.Running;

public sealed record ManualEvaluationSettings(
    string? OllamaEndpoint,
    string? OllamaModel,
    int RunsPerCase,
    string Temperature,
    string PromptVersion,
    string Commit,
    string RunDate)
{
    public const int DefaultRunsPerCase = 5;
    public const string NotSet = "not set";

    public static ManualEvaluationSettings FromEnvironment() =>
        FromEnvironment(Environment.GetEnvironmentVariable);

    public static ManualEvaluationSettings FromEnvironment(Func<string, string?> read)
    {
        ArgumentNullException.ThrowIfNull(read);

        return new ManualEvaluationSettings(
            read("INTERNHUB_AI_EVAL_OLLAMA_ENDPOINT"),
            read("INTERNHUB_AI_EVAL_OLLAMA_MODEL"),
            ParseRuns(read("INTERNHUB_AI_EVAL_RUNS")),
            ContextValue(read("INTERNHUB_AI_EVAL_TEMPERATURE")),
            ContextValue(read("INTERNHUB_AI_EVAL_PROMPT_VERSION")),
            ContextValue(read("INTERNHUB_AI_EVAL_COMMIT")),
            ContextValue(read("INTERNHUB_AI_EVAL_DATE")));
    }

    private static int ParseRuns(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DefaultRunsPerCase;

        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var runs) && runs >= 1
            ? runs
            : throw new EvaluationSetupException(
                $"INTERNHUB_AI_EVAL_RUNS must be a whole number of at least 1, but was '{value}'.");
    }

    private static string ContextValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? NotSet : value.Trim();
}
