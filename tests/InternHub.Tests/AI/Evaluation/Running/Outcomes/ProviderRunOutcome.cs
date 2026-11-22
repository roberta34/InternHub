using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Dataset;

namespace InternHub.Tests.AI.Evaluation.Running.Outcomes;

public sealed record ProviderRunOutcome(
    string CaseId,
    EvaluationCategory Category,
    int Run,
    OutcomeKind Kind,
    RecruiterEvaluationResult? Result);
