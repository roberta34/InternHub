using System.Net;
using FluentAssertions;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.Offers.CvFeedback;
using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Api.Features.Shared;

namespace InternHub.Tests.Unit.Offers.CvFeedback;

public sealed class CvFeedbackHandlerTests
{
    private static readonly DateTime Deadline =
        new(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_ValidCvAndExistingOffer_When_Analyzing_Then_ReturnsMappedFeedback()
    {
        // Arrange
        var offer = CreateOffer();
        var evaluation = new RecruiterEvaluationResult(
            ["Python", "SQL"],
            ["Docker"],
            ["Consider gaining experience with Docker."],
            "Matched 2 of 3 recognized required skills.");
        var offers = new FakeOfferRepository();
        offers.Add(offer);
        var evaluator = new FakeRecruiterEvaluationService(evaluation);
        var handler = new CvFeedbackHandler(offers, evaluator);
        const string cvText = "Experience with Python and SQL.";

        // Act
        var result = await handler.HandleAsync(
            new CvFeedbackCommand(offer.Id, cvText),
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeEquivalentTo(new CvFeedbackResponse(
            evaluation.MatchedSkills,
            evaluation.MissingSkills,
            evaluation.Suggestions,
            evaluation.Summary));
        evaluator.CvContent.Should().Be(cvText);
        evaluator.OfferRequirements.Should().Be(offer.Requirements);
        offers.GetByIdCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Given_MissingOffer_When_AnalyzingCv_Then_ReturnsNotFoundWithoutCallingEvaluator()
    {
        // Arrange
        var offers = new FakeOfferRepository();
        var evaluator = new FakeRecruiterEvaluationService();
        var handler = new CvFeedbackHandler(offers, evaluator);
        var offerId = Guid.NewGuid();

        // Act
        var result = await handler.HandleAsync(
            new CvFeedbackCommand(offerId, "A valid CV."),
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Contain(offerId.ToString());
        evaluator.CallCount.Should().Be(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n ")]
    public async Task Given_EmptyOrWhitespaceCv_When_Analyzing_Then_ReturnsValidation(
        string? cvText)
    {
        // Arrange
        var offers = new FakeOfferRepository();
        var evaluator = new FakeRecruiterEvaluationService();
        var handler = new CvFeedbackHandler(offers, evaluator);

        // Act
        var result = await handler.HandleAsync(
            new CvFeedbackCommand(Guid.NewGuid(), cvText),
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.Validation);
        evaluator.CallCount.Should().Be(0);
        offers.GetByIdCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_CvExceedingMaximumLength_When_Analyzing_Then_ReturnsValidation()
    {
        // Arrange
        var offers = new FakeOfferRepository();
        var evaluator = new FakeRecruiterEvaluationService();
        var handler = new CvFeedbackHandler(offers, evaluator);
        var cvText = new string('a', CvFeedbackHandler.MaximumCvLength + 1);

        // Act
        var result = await handler.HandleAsync(
            new CvFeedbackCommand(Guid.NewGuid(), cvText),
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.Validation);
        result.Error.Should().Contain(CvFeedbackHandler.MaximumCvLength.ToString());
        evaluator.CallCount.Should().Be(0);
        offers.GetByIdCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_CancellationToken_When_AnalyzingCv_Then_ForwardsTokenToEvaluator()
    {
        // Arrange
        var offer = CreateOffer();
        var offers = new FakeOfferRepository();
        offers.Add(offer);
        var evaluator = new FakeRecruiterEvaluationService();
        var handler = new CvFeedbackHandler(offers, evaluator);
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;

        // Act
        var result = await handler.HandleAsync(
            new CvFeedbackCommand(offer.Id, "A valid CV."),
            cancellationToken);

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        evaluator.CancellationToken.Should().Be(cancellationToken);
    }

    [Fact]
    public async Task Given_HttpProviderFailure_When_AnalyzingCv_Then_ReturnsServiceUnavailable()
    {
        // Arrange
        var offer = CreateOffer();
        var offers = new FakeOfferRepository();
        offers.Add(offer);
        var evaluator = new FakeRecruiterEvaluationService
        {
            Exception = new HttpRequestException("Provider unavailable.")
        };
        var handler = new CvFeedbackHandler(offers, evaluator);

        // Act
        var result = await handler.HandleAsync(
            new CvFeedbackCommand(offer.Id, "A valid CV."),
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.ServiceUnavailable);
        result.Error.Should().Be("CV feedback is temporarily unavailable.");
    }

    [Fact]
    public async Task Given_InvalidProviderResponse_When_AnalyzingCv_Then_ReturnsServiceUnavailable()
    {
        // Arrange
        var offer = CreateOffer();
        var offers = new FakeOfferRepository();
        offers.Add(offer);
        var evaluator = new FakeRecruiterEvaluationService
        {
            Exception = new InvalidOperationException("Invalid provider response.")
        };
        var handler = new CvFeedbackHandler(offers, evaluator);

        // Act
        var result = await handler.HandleAsync(
            new CvFeedbackCommand(offer.Id, "A valid CV."),
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.ServiceUnavailable);
        result.Error.Should().Be("CV feedback is temporarily unavailable.");
    }

    [Fact]
    public async Task Given_ProviderTimeout_When_AnalyzingCv_Then_ReturnsServiceUnavailable()
    {
        // Arrange
        var offer = CreateOffer();
        var offers = new FakeOfferRepository();
        offers.Add(offer);
        var evaluator = new FakeRecruiterEvaluationService
        {
            Exception = new OperationCanceledException("Provider timed out.")
        };
        var handler = new CvFeedbackHandler(offers, evaluator);

        // Act
        var result = await handler.HandleAsync(
            new CvFeedbackCommand(offer.Id, "A valid CV."),
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.ServiceUnavailable);
        result.Error.Should().Be("CV feedback is temporarily unavailable.");
    }

    private static Offer CreateOffer() =>
        new("Backend Intern", "Example Co", "Python, SQL, Docker", Deadline, 2);

    private sealed class FakeOfferRepository : IOfferRepository
    {
        private readonly List<Offer> _offers = [];

        public int GetByIdCallCount { get; private set; }

        public bool Add(Offer offer)
        {
            _offers.Add(offer);
            return true;
        }

        public Offer? GetById(Guid id)
        {
            GetByIdCallCount++;
            return _offers.SingleOrDefault(offer => offer.Id == id);
        }

        public IReadOnlyCollection<Offer> GetAll() => _offers.ToArray();

        public bool Update(Offer offer) =>
            _offers.Any(existingOffer => existingOffer.Id == offer.Id);

        public bool Delete(Guid id)
        {
            var offer = GetById(id);
            return offer is not null && _offers.Remove(offer);
        }
    }

    private sealed class FakeRecruiterEvaluationService(
        RecruiterEvaluationResult? evaluation = null) : IRecruiterEvaluationService
    {
        public Exception? Exception { get; init; }
        public int CallCount { get; private set; }
        public string? CvContent { get; private set; }
        public string? OfferRequirements { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task<RecruiterEvaluationResult> AnalyzeAsync(
            string cvContent,
            string offerRequirements,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            CvContent = cvContent;
            OfferRequirements = offerRequirements;
            CancellationToken = cancellationToken;

            return Exception is null
                ? Task.FromResult(evaluation ?? new RecruiterEvaluationResult(
                    [],
                    [],
                    [],
                    "No recognized skills were found in the internship requirements."))
                : Task.FromException<RecruiterEvaluationResult>(Exception);
        }
    }
}
