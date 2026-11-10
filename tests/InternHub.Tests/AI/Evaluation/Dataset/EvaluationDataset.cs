namespace InternHub.Tests.AI.Evaluation.Dataset;

public sealed record EvaluationDataset(
    string DatasetVersion,
    bool Synthetic,
    IReadOnlyDictionary<string, string> Aliases,
    IReadOnlyList<EvaluationCase> Cases);
