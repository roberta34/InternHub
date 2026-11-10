namespace InternHub.Tests.AI.Evaluation.Metrics;

public sealed record MicroScores(ConfusionCounts Counts, double Precision, double Recall, double F1)
{
    public static MicroScores From(ConfusionCounts counts)
    {
        ArgumentNullException.ThrowIfNull(counts);

        return new MicroScores(counts, counts.Precision, counts.Recall, counts.F1);
    }
}
