namespace InternHub.Tests.AI.Evaluation.Metrics.Core;

/// <summary>Divides with an explicit value for a zero denominator, so every empty-set convention is visible.</summary>
public static class Ratio
{
    public static double Divide(double numerator, double denominator, double whenDenominatorIsZero) =>
        denominator == 0 ? whenDenominatorIsZero : numerator / denominator;
}
