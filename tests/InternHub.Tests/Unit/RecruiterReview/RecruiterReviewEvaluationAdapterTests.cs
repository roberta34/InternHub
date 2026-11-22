using FluentAssertions;
using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Api.Features.RecruiterReview;
using InternHub.Api.Features.StudentCvFeedback;
using InternHub.Api.Infrastructure.AI.RecruiterReview;
using InternHub.Tests.Unit.RecruiterReview.Fakes;

namespace InternHub.Tests.Unit.RecruiterReview;

public sealed class RecruiterReviewEvaluationAdapterTests
{
    private const string Requirements = "C#, SQL, Docker";
    private const string Cv = "Built REST APIs in C# backed by SQL.";

    public static TheoryData<Exception> ProviderFailures => new()
    {
        new StudentCvFeedbackProviderException(StudentCvFeedbackFailureKind.ProviderUnavailable, "Unavailable."),
        new HttpRequestException("Connection refused."),
        new InvalidOperationException("Invalid JSON."),
        new TaskCanceledException("Timed out.")
    };

    [Fact]
    public async Task Given_EvaluationWithSkillsPresentInCv_When_Reviewing_Then_MapsFields()
    {
        // Arrange
        var evaluator = new StubRecruiterEvaluationService
        {
            Result = new RecruiterEvaluationResult(["C#", "SQL"], ["Docker"], ["Learn Docker."], "Good fit.")
        };
        var adapter = new RecruiterReviewEvaluationAdapter(evaluator);

        // Act
        var review = await adapter.ReviewAsync(Requirements, Cv);

        // Assert
        review.MatchedSkills.Should().Equal("C#", "SQL");
        review.MissingOrUnclearSkills.Should().Equal("Docker");
        review.Summary.Should().Be("Good fit.");
        evaluator.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Given_MatchedSkillsPresentInCv_When_Reviewing_Then_EvidenceOnlyForThemWithExcerptContainingSkill()
    {
        // Arrange
        var evaluator = new StubRecruiterEvaluationService
        {
            Result = new RecruiterEvaluationResult(["C#", "SQL"], ["Docker"], [], "Summary.")
        };
        var adapter = new RecruiterReviewEvaluationAdapter(evaluator);

        // Act
        var review = await adapter.ReviewAsync(Requirements, Cv);

        // Assert
        review.Evidence.Select(item => item.Skill).Should().Equal("C#", "SQL");
        review.Evidence[0].Excerpt.Should().Contain("C#");
        review.Evidence[1].Excerpt.Should().Contain("SQL");
    }

    [Fact]
    public async Task Given_MatchedSkillAbsentFromCv_When_Reviewing_Then_MovesItToMissingOrUnclearWithoutEvidence()
    {
        // Arrange
        var evaluator = new StubRecruiterEvaluationService
        {
            Result = new RecruiterEvaluationResult(["C#", "Kubernetes"], ["Docker"], [], "Summary.")
        };
        var adapter = new RecruiterReviewEvaluationAdapter(evaluator);

        // Act
        var review = await adapter.ReviewAsync("C#, Kubernetes, Docker", Cv);

        // Assert
        review.MatchedSkills.Should().Equal("C#");
        review.MissingOrUnclearSkills.Should().Equal("Kubernetes", "Docker");
        review.Evidence.Select(item => item.Skill).Should().Equal("C#");
    }

    [Fact]
    public async Task Given_SkillReportedAsMatchedAndMissing_When_Reviewing_Then_KeepsItOnlyAsMatched()
    {
        // Arrange
        var evaluator = new StubRecruiterEvaluationService
        {
            Result = new RecruiterEvaluationResult(["C#"], ["c#", "Docker"], [], "Summary.")
        };
        var adapter = new RecruiterReviewEvaluationAdapter(evaluator);

        // Act
        var review = await adapter.ReviewAsync(Requirements, Cv);

        // Assert
        review.MatchedSkills.Should().Equal("C#");
        review.MissingOrUnclearSkills.Should().Equal("Docker");
    }

    [Theory]
    [MemberData(nameof(ProviderFailures))]
    public async Task Given_ProviderFailure_When_Reviewing_Then_ThrowsReviewProviderExceptionWithoutCvText(
        Exception failure)
    {
        // Arrange
        var evaluator = new StubRecruiterEvaluationService { ExceptionToThrow = failure };
        var adapter = new RecruiterReviewEvaluationAdapter(evaluator);

        // Act
        var act = () => adapter.ReviewAsync(Requirements, Cv);

        // Assert
        var thrown = await act.Should().ThrowExactlyAsync<RecruiterCandidacyReviewProviderException>();
        thrown.Which.InnerException.Should().BeSameAs(failure);
        thrown.Which.Message.Should().NotContain(Cv);
    }

    [Fact]
    public async Task Given_ClientCancelledRequest_When_Reviewing_Then_PropagatesOperationCanceledException()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var evaluator = new StubRecruiterEvaluationService
        {
            ExceptionToThrow = new OperationCanceledException(cancellation.Token)
        };
        var adapter = new RecruiterReviewEvaluationAdapter(evaluator);

        // Act
        var act = () => adapter.ReviewAsync(Requirements, Cv, cancellation.Token);

        // Assert
        await act.Should().ThrowExactlyAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Given_UnexpectedArgumentException_When_Reviewing_Then_PropagatesItUnchanged()
    {
        // Arrange
        var failure = new ArgumentException("Unexpected.");
        var evaluator = new StubRecruiterEvaluationService { ExceptionToThrow = failure };
        var adapter = new RecruiterReviewEvaluationAdapter(evaluator);

        // Act
        var act = () => adapter.ReviewAsync(Requirements, Cv);

        // Assert
        (await act.Should().ThrowExactlyAsync<ArgumentException>()).Which.Should().BeSameAs(failure);
    }

    [Fact]
    public async Task Given_EvaluationWithNullSkillLists_When_Reviewing_Then_ThrowsReviewProviderException()
    {
        // Arrange
        var evaluator = new StubRecruiterEvaluationService
        {
            Result = new RecruiterEvaluationResult(null!, null!, [], "Summary.")
        };
        var adapter = new RecruiterReviewEvaluationAdapter(evaluator);

        // Act
        var act = () => adapter.ReviewAsync(Requirements, Cv);

        // Assert
        await act.Should().ThrowExactlyAsync<RecruiterCandidacyReviewProviderException>();
    }
}
