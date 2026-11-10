using FluentAssertions;
using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Dataset;
using InternHub.Tests.AI.Evaluation.Running;
using InternHub.Tests.Unit.AI.Evaluation.Metrics;

namespace InternHub.Tests.Unit.AI.Evaluation.Running;

public sealed class EvaluationRunnerTests
{
    private static readonly IReadOnlyDictionary<string, string?> NoSettings = new Dictionary<string, string?>();

    private static readonly RecruiterEvaluationResult CSharpOnly =
        new(["c#"], [], [], "Summary.");

    private static EvaluationRunner CreateRunner() => new(new OutcomeClassifier());

    private static EvaluationRunContext Context(int runsPerCase) =>
        new("stub-provider", "stub-model", "0.1", "prompt-v-test", runsPerCase, "2026-01-01", "abc1234");

    // clear-01 expects matched {c#}; negation-01 expects matched {c#} and missing {docker}.
    private static EvaluationDataset TwoCaseDataset() =>
        new(
            "test",
            true,
            new Dictionary<string, string>(),
            [
                EvaluationCaseBuilder.With(["c#"], []) with { Id = "clear-01" },
                EvaluationCaseBuilder.With(["c#"], ["docker"]) with { Id = "negation-01", Category = EvaluationCategory.Negation }
            ]);

    private static EvaluationDataset ThreeCaseDataset() =>
        new(
            "test",
            true,
            new Dictionary<string, string>(),
            [
                EvaluationCaseBuilder.With(["c#"], []) with { Id = "clear-01" },
                EvaluationCaseBuilder.With(["c#"], []) with { Id = "clear-02" },
                EvaluationCaseBuilder.With(["c#"], []) with { Id = "clear-03" }
            ]);

    private static EvaluationDataset OneCaseDataset() =>
        new(
            "test",
            true,
            new Dictionary<string, string>(),
            [EvaluationCaseBuilder.With(["c#"], []) with { Id = "clear-01" }]);

    [Theory]
    [InlineData(null, "Ai:Ollama:Model")]
    [InlineData("", "Ai:Ollama:Model")]
    [InlineData("   ", "Ai:Ollama:Model")]
    public async Task Given_RequiredSettingMissingOrBlank_When_Running_Then_ThrowsSetupExceptionWithoutCallingProvider(
        string? modelValue,
        string expectedKey)
    {
        // Arrange
        var service = new StubRecruiterEvaluationService(() => CSharpOnly);
        var runner = CreateRunner();
        var requiredSettings = new Dictionary<string, string?>
        {
            ["Ai:Ollama:Endpoint"] = "http://localhost:11434",
            ["Ai:Ollama:Model"] = modelValue
        };

        // Act
        var run = () => runner.RunAsync(TwoCaseDataset(), service, requiredSettings, Context(1));

        // Assert
        await run.Should().ThrowAsync<EvaluationSetupException>().WithMessage($"*{expectedKey}*");
        service.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_ThreeRunsPerCaseAndTwoCases_When_Running_Then_CallsProviderThreeTimesPerCase()
    {
        // Arrange
        var service = new StubRecruiterEvaluationService(() => CSharpOnly);
        var runner = CreateRunner();

        // Act
        var report = await runner.RunAsync(TwoCaseDataset(), service, NoSettings, Context(3));

        // Assert
        service.CallCount.Should().Be(6);
        report.TotalCalls.Should().Be(6);
        report.OutcomeCounts[OutcomeKind.Success].Should().Be(6);
        report.Outcomes.Count(outcome => outcome.CaseId == "clear-01").Should().Be(3);
        report.Outcomes.Count(outcome => outcome.CaseId == "negation-01").Should().Be(3);
    }

    [Fact]
    public async Task Given_IdenticalResultsAndThreeRunsPerCase_When_Running_Then_StabilityIsOneOverBothCases()
    {
        // Arrange
        var service = new StubRecruiterEvaluationService(() => CSharpOnly);
        var runner = CreateRunner();

        // Act
        var report = await runner.RunAsync(TwoCaseDataset(), service, NoSettings, Context(3));

        // Assert
        report.StabilityCaseCount.Should().Be(2);
        report.Stability!.Matched.Should().BeApproximately(1, 0.0001);
        report.Stability.Missing.Should().BeApproximately(1, 0.0001);
    }

    [Fact]
    public async Task Given_OneRunPerCase_When_Running_Then_StabilityIsNotComputed()
    {
        // Arrange
        var service = new StubRecruiterEvaluationService(() => CSharpOnly);
        var runner = CreateRunner();

        // Act
        var report = await runner.RunAsync(TwoCaseDataset(), service, NoSettings, Context(1));

        // Assert
        report.Stability.Should().BeNull();
        report.StabilityCaseCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_SuccessfulResults_When_Running_Then_AggregatesOnlyThoseResults()
    {
        // Arrange
        // Matched F1: clear-01 = 1, negation-01 = 1 -> macro 1.
        // Missing F1: clear-01 both empty = 1, negation-01 expected {docker}, predicted {} = 0 -> macro 0.5.
        var service = new StubRecruiterEvaluationService(() => CSharpOnly);
        var runner = CreateRunner();

        // Act
        var report = await runner.RunAsync(TwoCaseDataset(), service, NoSettings, Context(1));

        // Assert
        report.Aggregate!.CaseCount.Should().Be(2);
        report.Aggregate.Macro["Matched F1"].Should().BeApproximately(1, 0.0001);
        report.Aggregate.Macro["Missing F1"].Should().BeApproximately(0.5, 0.0001);
    }

    [Fact]
    public async Task Given_TwoCategories_When_Running_Then_BreaksDownByPresentCategories()
    {
        // Arrange
        var service = new StubRecruiterEvaluationService(() => CSharpOnly);
        var runner = CreateRunner();

        // Act
        var report = await runner.RunAsync(TwoCaseDataset(), service, NoSettings, Context(1));

        // Assert
        report.ByCategory.Keys.Should().BeEquivalentTo([EvaluationCategory.Clear, EvaluationCategory.Negation]);
    }

    [Fact]
    public async Task Given_FirstCallUnavailable_When_Running_Then_StopsAfterOneCall()
    {
        // Arrange
        var service = new StubRecruiterEvaluationService(() => throw new HttpRequestException("Connection refused."));
        var runner = CreateRunner();

        // Act
        var report = await runner.RunAsync(TwoCaseDataset(), service, NoSettings, Context(3));

        // Assert
        service.CallCount.Should().Be(1);
        report.StoppedAfterFirstUnavailableCall.Should().BeTrue();
        report.TotalCalls.Should().Be(1);
        report.OutcomeCounts[OutcomeKind.InfrastructureUnavailable].Should().Be(1);
        report.UnavailabilityRate.Should().BeApproximately(1, 0.0001);
        report.ValidResponseRate.Should().BeNull();
        report.Aggregate.Should().BeNull();
    }

    [Fact]
    public async Task Given_FirstCallTimesOut_When_Running_Then_StopsAsInfrastructureUnavailable()
    {
        // Arrange
        var service = new StubRecruiterEvaluationService(() => throw new TaskCanceledException("Timed out."));
        var runner = CreateRunner();

        // Act
        var report = await runner.RunAsync(TwoCaseDataset(), service, NoSettings, Context(1));

        // Assert
        service.CallCount.Should().Be(1);
        report.StoppedAfterFirstUnavailableCall.Should().BeTrue();
        report.Outcomes.Should().ContainSingle()
            .Which.Kind.Should().Be(OutcomeKind.InfrastructureUnavailable);
    }

    [Fact]
    public async Task Given_SuccessThenInvalidThenUnavailable_When_Running_Then_CountsEachOutcomeAndRatesSeparately()
    {
        // Arrange
        // Reached the model: 1 success + 1 invalid -> valid-response rate 1 / 2.
        // Unavailable: 1 of 3 calls -> unavailability rate 1 / 3.
        var service = new StubRecruiterEvaluationService(
            () => CSharpOnly,
            () => throw new InvalidOperationException("Invalid model output."),
            () => throw new HttpRequestException("Connection refused."));
        var runner = CreateRunner();

        // Act
        var report = await runner.RunAsync(ThreeCaseDataset(), service, NoSettings, Context(1));

        // Assert
        report.StoppedAfterFirstUnavailableCall.Should().BeFalse();
        report.OutcomeCounts[OutcomeKind.Success].Should().Be(1);
        report.OutcomeCounts[OutcomeKind.InvalidResponse].Should().Be(1);
        report.OutcomeCounts[OutcomeKind.InfrastructureUnavailable].Should().Be(1);
        report.ValidResponseRate!.Value.Should().BeApproximately(0.5, 0.0001);
        report.UnavailabilityRate.Should().BeApproximately(0.3333, 0.0001);
        report.Aggregate!.CaseCount.Should().Be(1);
    }

    [Fact]
    public async Task Given_ResultWithNullList_When_Running_Then_CountsInvalidResponseAndSkipsMetrics()
    {
        // Arrange
        var service = new StubRecruiterEvaluationService(() => new RecruiterEvaluationResult(null!, [], [], "Summary."));
        var runner = CreateRunner();

        // Act
        var report = await runner.RunAsync(OneCaseDataset(), service, NoSettings, Context(1));

        // Assert
        report.OutcomeCounts[OutcomeKind.InvalidResponse].Should().Be(1);
        report.Outcomes.Should().ContainSingle()
            .Which.Result.Should().BeNull();
        report.Aggregate.Should().BeNull();
    }

    [Fact]
    public async Task Given_UnknownException_When_Running_Then_RethrowsIt()
    {
        // Arrange
        var service = new StubRecruiterEvaluationService(() => throw new NotSupportedException("Unexpected."));
        var runner = CreateRunner();

        // Act
        var run = () => runner.RunAsync(TwoCaseDataset(), service, NoSettings, Context(1));

        // Assert
        await run.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task Given_CancellationRequestedByCaller_When_Running_Then_RethrowsIt()
    {
        // Arrange
        var service = new StubRecruiterEvaluationService(() => throw new TaskCanceledException("Cancelled."));
        var runner = CreateRunner();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        // Act
        var run = () => runner.RunAsync(TwoCaseDataset(), service, NoSettings, Context(1), cancellation.Token);

        // Assert
        await run.Should().ThrowAsync<TaskCanceledException>();
    }
}
