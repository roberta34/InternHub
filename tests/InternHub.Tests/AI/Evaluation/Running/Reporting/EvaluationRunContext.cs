namespace InternHub.Tests.AI.Evaluation.Running.Reporting;

/// <summary>Value Object: the run context recorded in a report (provider, model, temperature, prompt version, K, date, commit).</summary>
public sealed record EvaluationRunContext(
    string Provider,
    string Model,
    string Temperature,
    string PromptVersion,
    int RunsPerCase,
    string RunDate,
    string Commit);
