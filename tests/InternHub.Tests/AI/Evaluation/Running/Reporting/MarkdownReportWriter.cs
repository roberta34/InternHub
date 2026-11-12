using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using InternHub.Tests.AI.Evaluation.Metrics.Aggregation;
using InternHub.Tests.AI.Evaluation.Metrics.Strategies;
using InternHub.Tests.AI.Evaluation.Running.Outcomes;

namespace InternHub.Tests.AI.Evaluation.Running.Reporting;

/// <summary>Writes a run report as Markdown with only case ids, categories, context values and numbers; never CV or provider-generated text.</summary>
public static class MarkdownReportWriter
{
    private static readonly string[] MicroMacroMetricNames =
    [
        "Matched precision",
        "Matched recall",
        "Matched F1",
        "Missing precision",
        "Missing recall",
        "Missing F1"
    ];

    private static readonly IReadOnlyDictionary<string, Func<AggregateScores, double>> MicroValues =
        new Dictionary<string, Func<AggregateScores, double>>
        {
            ["Matched precision"] = scores => scores.Micro["Matched"].Precision,
            ["Matched recall"] = scores => scores.Micro["Matched"].Recall,
            ["Matched F1"] = scores => scores.Micro["Matched"].F1,
            ["Missing precision"] = scores => scores.Micro["Missing"].Precision,
            ["Missing recall"] = scores => scores.Micro["Missing"].Recall,
            ["Missing F1"] = scores => scores.Micro["Missing"].F1
        };

    public static string Write(EvaluationRunReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var markdown = new StringBuilder();
        WriteContext(markdown, report.Context);
        WriteOutcomes(markdown, report);
        WriteAggregate(markdown, report.Aggregate);
        WriteStability(markdown, report);
        WriteCategories(markdown, report);
        WriteNonSuccessCalls(markdown, report);

        return markdown.ToString();
    }

    public static string FileName(EvaluationRunContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return $"ai-evaluation-{Sanitize(context.Provider)}-{Sanitize(context.Model)}.md";
    }

    private static void WriteContext(StringBuilder markdown, EvaluationRunContext context)
    {
        markdown.AppendLine($"# AI evaluation report: {context.Provider} / {context.Model}");
        markdown.AppendLine();
        markdown.AppendLine("## Run context");
        markdown.AppendLine();
        markdown.AppendLine("| Field | Value |");
        markdown.AppendLine("|---|---|");
        markdown.AppendLine($"| Provider | {context.Provider} |");
        markdown.AppendLine($"| Model | {context.Model} |");
        markdown.AppendLine($"| Temperature | {context.Temperature} |");
        markdown.AppendLine($"| Prompt version | {context.PromptVersion} |");
        markdown.AppendLine($"| Runs per case (K) | {context.RunsPerCase.ToString(CultureInfo.InvariantCulture)} |");
        markdown.AppendLine($"| Date | {context.RunDate} |");
        markdown.AppendLine($"| Commit | {context.Commit} |");
        markdown.AppendLine();
    }

    private static void WriteOutcomes(StringBuilder markdown, EvaluationRunReport report)
    {
        markdown.AppendLine("## Outcomes");
        markdown.AppendLine();
        markdown.AppendLine("| Outcome | Calls |");
        markdown.AppendLine("|---|---|");

        foreach (var kind in Enum.GetValues<OutcomeKind>())
            markdown.AppendLine($"| {kind} | {report.OutcomeCounts[kind].ToString(CultureInfo.InvariantCulture)} |");

        markdown.AppendLine($"| Total | {report.TotalCalls.ToString(CultureInfo.InvariantCulture)} |");
        markdown.AppendLine();
        markdown.AppendLine($"- Valid-response rate (calls that reached the model): {Format(report.ValidResponseRate)}");
        markdown.AppendLine($"- Unavailability rate (all calls): {Format(report.UnavailabilityRate)}");
        markdown.AppendLine("- Provider-internal retries are invisible to the evaluator: all rates are measured after them.");

        if (report.StoppedAfterFirstUnavailableCall)
        {
            markdown.AppendLine();
            markdown.AppendLine(
                "> **Run stopped:** the first call ended in InfrastructureUnavailable, so no further calls were made. " +
                "Check that the service is reachable and run again.");
        }

        markdown.AppendLine();
    }

    private static void WriteAggregate(StringBuilder markdown, AggregateScores? aggregate)
    {
        markdown.AppendLine("## Aggregate metrics");
        markdown.AppendLine();

        if (aggregate is null)
        {
            markdown.AppendLine("No successful outcomes; metrics were not computed.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine($"Computed over {aggregate.CaseCount.ToString(CultureInfo.InvariantCulture)} successful result(s).");
        markdown.AppendLine();
        markdown.AppendLine("| Metric | Micro | Macro |");
        markdown.AppendLine("|---|---|---|");

        foreach (var name in MicroMacroMetricNames)
            markdown.AppendLine($"| {name} | {Format(MicroValues[name](aggregate))} | {Format(aggregate.Macro[name])} |");

        markdown.AppendLine();
        markdown.AppendLine("### Extended metrics (macro)");
        markdown.AppendLine();
        markdown.AppendLine("| Metric | Macro |");
        markdown.AppendLine("|---|---|");

        foreach (var metric in aggregate.Macro.Where(metric => !MicroValues.ContainsKey(metric.Key)))
            markdown.AppendLine($"| {metric.Key} | {Format(metric.Value)} |");

        markdown.AppendLine();
    }

    private static void WriteStability(StringBuilder markdown, EvaluationRunReport report)
    {
        markdown.AppendLine("## Stability");
        markdown.AppendLine();

        if (report.Stability is null)
        {
            markdown.AppendLine(
                $"Not computed (K = {report.Context.RunsPerCase.ToString(CultureInfo.InvariantCulture)}; " +
                "needs K >= 2 and at least two successful runs of a case).");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine(
            $"Mean pairwise Jaccard similarity over {report.StabilityCaseCount.ToString(CultureInfo.InvariantCulture)} case(s).");
        markdown.AppendLine();
        markdown.AppendLine("| List | Stability |");
        markdown.AppendLine("|---|---|");
        markdown.AppendLine($"| Matched | {Format(report.Stability.Matched)} |");
        markdown.AppendLine($"| Missing | {Format(report.Stability.Missing)} |");
        markdown.AppendLine();
    }

    private static void WriteCategories(StringBuilder markdown, EvaluationRunReport report)
    {
        markdown.AppendLine("## Per category");
        markdown.AppendLine();

        if (report.ByCategory.Count == 0)
        {
            markdown.AppendLine("No successful outcomes.");
            markdown.AppendLine();
            return;
        }

        var metricNames = CaseMetrics.Default.Select(metric => metric.Name).ToList();
        markdown.AppendLine(
            $"| Category | Results | Matched F1 (micro) | Missing F1 (micro) | {string.Join(" | ", metricNames)} |");
        markdown.AppendLine($"|---|---|---|---|{string.Concat(metricNames.Select(_ => "---|"))}");

        foreach (var category in report.ByCategory.OrderBy(entry => entry.Key))
        {
            var scores = category.Value;
            var macroValues = metricNames.Select(name => Format(scores.Macro[name]));
            markdown.AppendLine(
                $"| {category.Key} | {scores.CaseCount.ToString(CultureInfo.InvariantCulture)} | " +
                $"{Format(scores.Micro["Matched"].F1)} | {Format(scores.Micro["Missing"].F1)} | " +
                $"{string.Join(" | ", macroValues)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteNonSuccessCalls(StringBuilder markdown, EvaluationRunReport report)
    {
        markdown.AppendLine("## Non-success calls");
        markdown.AppendLine();

        var failures = report.Outcomes.Where(outcome => outcome.Kind != OutcomeKind.Success).ToList();

        if (failures.Count == 0)
        {
            markdown.AppendLine("None.");
            return;
        }

        markdown.AppendLine("| Case | Category | Run | Outcome |");
        markdown.AppendLine("|---|---|---|---|");

        foreach (var failure in failures)
            markdown.AppendLine(
                $"| {failure.CaseId} | {failure.Category} | {failure.Run.ToString(CultureInfo.InvariantCulture)} | {failure.Kind} |");
    }

    private static string Format(double? value) =>
        value?.ToString("0.0000", CultureInfo.InvariantCulture) ?? "n/a";

    private static string Sanitize(string value) =>
        Regex.Replace(value.Trim().ToLowerInvariant(), "[^a-z0-9.]+", "-").Trim('-');
}
