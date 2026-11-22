using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Dataset;

namespace InternHub.Tests.Unit.AI.Evaluation.TestDoubles;

/// <summary>
/// Scripted provider for the offline run: returns each case's own labels, one suggestion per missing skill
/// and a fixed summary, except three documented perturbations:
/// (1) negation-03: the negated skill is moved from missing to matched and its suggestion removed;
/// (2) suggestion-mismatch-01: a suggestion naming no skill is appended;
/// (3) minimal-01: the summary is blank, so the call is an InvalidResponse.
/// </summary>
internal sealed class ScriptedEvaluationProvider : IRecruiterEvaluationService
{
    public const string Summary = "Scripted evaluation summary.";
    public const string UnrelatedSuggestion = "Improve the layout of your CV.";

    public const string NegatedSkillMovedCaseId = "negation-03";
    public const string UnrelatedSuggestionCaseId = "suggestion-mismatch-01";
    public const string BlankSummaryCaseId = "minimal-01";

    private static readonly IReadOnlyDictionary<string, Func<EvaluationCase, RecruiterEvaluationResult>> Perturbations =
        new Dictionary<string, Func<EvaluationCase, RecruiterEvaluationResult>>
        {
            [NegatedSkillMovedCaseId] = evaluationCase => new RecruiterEvaluationResult(
                [.. evaluationCase.ExpectedMatched, .. evaluationCase.ExpectedMissing],
                [],
                [],
                Summary),
            [UnrelatedSuggestionCaseId] = evaluationCase => Labels(evaluationCase) with
            {
                Suggestions = [.. SuggestionsFor(evaluationCase), UnrelatedSuggestion]
            },
            [BlankSummaryCaseId] = evaluationCase => Labels(evaluationCase) with { Summary = " " }
        };

    private readonly IReadOnlyDictionary<string, EvaluationCase> _casesByCvText;

    public ScriptedEvaluationProvider(EvaluationDataset dataset)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        if (dataset.Cases.Select(evaluationCase => evaluationCase.CvText).Distinct(StringComparer.Ordinal).Count()
            != dataset.Cases.Count)
        {
            throw new InvalidOperationException("Every dataset case needs a unique CV text for the scripted provider.");
        }

        _casesByCvText = dataset.Cases.ToDictionary(evaluationCase => evaluationCase.CvText, StringComparer.Ordinal);
    }

    public static string SuggestionFor(string skill) => $"Practice {skill} in a small project.";

    public Task<RecruiterEvaluationResult> AnalyzeAsync(
        string cvContent,
        string offerRequirements,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_casesByCvText.TryGetValue(cvContent, out var evaluationCase))
            throw new ArgumentException("No dataset case has this CV text.", nameof(cvContent));

        var script = Perturbations.TryGetValue(evaluationCase.Id, out var perturbation) ? perturbation : Labels;
        return Task.FromResult(script(evaluationCase));
    }

    private static RecruiterEvaluationResult Labels(EvaluationCase evaluationCase) =>
        new(evaluationCase.ExpectedMatched, evaluationCase.ExpectedMissing, SuggestionsFor(evaluationCase), Summary);

    private static IReadOnlyList<string> SuggestionsFor(EvaluationCase evaluationCase) =>
        evaluationCase.ExpectedMissing.Select(SuggestionFor).ToList();
}
