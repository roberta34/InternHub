namespace InternHub.Tests.AI.Evaluation.Running;

public sealed class ManualEvaluationFactAttribute : FactAttribute
{
    public const string EnableVariable = "INTERNHUB_AI_EVAL";

    public ManualEvaluationFactAttribute()
    {
        var reason = SkipReasonFor(Environment.GetEnvironmentVariable(EnableVariable));

        if (reason is not null)
            Skip = reason;
    }

    public static string? SkipReasonFor(string? value) =>
        value == "1"
            ? null
            : $"Manual AI evaluation: set {EnableVariable}=1 to run it.";
}
