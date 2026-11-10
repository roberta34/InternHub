using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Dataset;
using InternHub.Tests.AI.Evaluation.Metrics;

namespace InternHub.Tests.AI.Evaluation.Running;

public sealed class EvaluationRunner
{
    private readonly OutcomeClassifier _classifier;

    public EvaluationRunner(OutcomeClassifier classifier)
    {
        ArgumentNullException.ThrowIfNull(classifier);

        _classifier = classifier;
    }

    public async Task<EvaluationRunReport> RunAsync(
        EvaluationDataset dataset,
        IRecruiterEvaluationService service,
        IReadOnlyDictionary<string, string?> requiredSettings,
        EvaluationRunContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(requiredSettings);
        ArgumentNullException.ThrowIfNull(context);

        EnsureConfigured(requiredSettings);

        if (context.RunsPerCase < 1)
            throw new ArgumentOutOfRangeException(nameof(context), context.RunsPerCase, "RunsPerCase must be at least 1.");

        var outcomes = new List<ProviderRunOutcome>();

        foreach (var evaluationCase in dataset.Cases)
        {
            for (var run = 1; run <= context.RunsPerCase; run++)
            {
                var outcome = await RunOnceAsync(service, evaluationCase, run, cancellationToken);
                outcomes.Add(outcome);

                if (outcomes.Count == 1 && outcome.Kind == OutcomeKind.InfrastructureUnavailable)
                    return BuildReport(dataset, context, outcomes, stoppedAfterFirstUnavailableCall: true);
            }
        }

        return BuildReport(dataset, context, outcomes, stoppedAfterFirstUnavailableCall: false);
    }

    private static void EnsureConfigured(IReadOnlyDictionary<string, string?> requiredSettings)
    {
        var missingKeys = requiredSettings
            .Where(setting => string.IsNullOrWhiteSpace(setting.Value))
            .Select(setting => setting.Key)
            .ToList();

        if (missingKeys.Count > 0)
            throw new EvaluationSetupException(
                $"Evaluation setup is incomplete. Missing configuration value(s): {string.Join(", ", missingKeys)}. No provider call was made.");
    }

    private async Task<ProviderRunOutcome> RunOnceAsync(
        IRecruiterEvaluationService service,
        EvaluationCase evaluationCase,
        int run,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.AnalyzeAsync(
                evaluationCase.CvText,
                evaluationCase.RequirementsText,
                cancellationToken);
            var resultKind = _classifier.ClassifyResult(result);

            return new ProviderRunOutcome(
                evaluationCase.Id,
                evaluationCase.Category,
                run,
                resultKind,
                resultKind == OutcomeKind.Success ? result : null);
        }
        catch (Exception exception) when (_classifier.TryClassify(exception, cancellationToken, out var exceptionKind))
        {
            return new ProviderRunOutcome(evaluationCase.Id, evaluationCase.Category, run, exceptionKind, null);
        }
    }

    private static EvaluationRunReport BuildReport(
        EvaluationDataset dataset,
        EvaluationRunContext context,
        IReadOnlyList<ProviderRunOutcome> outcomes,
        bool stoppedAfterFirstUnavailableCall)
    {
        var counts = Enum.GetValues<OutcomeKind>()
            .ToDictionary(kind => kind, kind => outcomes.Count(outcome => outcome.Kind == kind));

        var reachedModel = counts[OutcomeKind.Success] + counts[OutcomeKind.InvalidResponse];
        double? validResponseRate = reachedModel == 0
            ? null
            : (double)counts[OutcomeKind.Success] / reachedModel;
        var unavailabilityRate = Ratio.Divide(counts[OutcomeKind.InfrastructureUnavailable], outcomes.Count, 0);

        var casesById = dataset.Cases.ToDictionary(evaluationCase => evaluationCase.Id);
        var successes = outcomes
            .Where(outcome => outcome.Kind == OutcomeKind.Success)
            .Select(outcome => (Case: casesById[outcome.CaseId], Result: outcome.Result!))
            .ToList();

        var normalizer = new SkillNormalizer(dataset.Aliases);
        var aggregator = new MetricAggregator(CaseMetrics.Default, [SkillList.Matched, SkillList.Missing], normalizer);
        AggregateScores? aggregate = successes.Count == 0 ? null : aggregator.Aggregate(successes);
        var byCategory = new CategoryBreakdown(aggregator).Aggregate(successes);

        var repeatedCases = successes
            .Where(_ => context.RunsPerCase >= 2)
            .GroupBy(pair => pair.Case.Id)
            .Where(group => group.Count() >= 2)
            .Select(group => (IReadOnlyList<RecruiterEvaluationResult>)group.Select(pair => pair.Result).ToList())
            .ToList();
        StabilityScores? stability = repeatedCases.Count == 0
            ? null
            : new StabilityCalculator(normalizer).MeanOverCases(repeatedCases);

        return new EvaluationRunReport(
            context,
            outcomes.Count,
            counts,
            validResponseRate,
            unavailabilityRate,
            stoppedAfterFirstUnavailableCall,
            aggregate,
            byCategory,
            stability,
            repeatedCases.Count,
            outcomes.ToList());
    }
}
