namespace InternHub.Tests.AI.Evaluation.Dataset;

/// <summary>Value Object: the synthetic dataset with its alias map and cases.</summary>
public sealed record EvaluationDataset(
    string DatasetVersion,
    bool Synthetic,
    IReadOnlyDictionary<string, string> Aliases,
    IReadOnlyList<EvaluationCase> Cases);
