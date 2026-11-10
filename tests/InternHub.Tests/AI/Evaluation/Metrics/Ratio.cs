namespace InternHub.Tests.AI.Evaluation.Metrics;

public static class Ratio
{
    public static double Divide(double numerator, double denominator, double whenDenominatorIsZero) =>
        denominator == 0 ? whenDenominatorIsZero : numerator / denominator;
}
