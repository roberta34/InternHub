namespace InternHub.Tests.AI.Evaluation.Running;

public sealed record EvaluationRunContext(
    string Provider,
    string Model,
    string Temperature,
    string PromptVersion,
    int RunsPerCase,
    string RunDate,
    string Commit);
