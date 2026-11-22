using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.ReviewCandidacyWithAi;
using InternHub.Api.Features.Shared;
using InternHub.Tests.Unit.InternshipCandidacies.Fakes;
using InternHub.Tests.Unit.Offers.Fakes;
using InternHub.Tests.Unit.RecruiterReview.Fakes;

namespace InternHub.Tests.Unit.InternshipCandidacies.ReviewCandidacyWithAi;

public sealed class ReviewCandidacyWithAiHandlerTests
{
    private const string Cv = "Built REST APIs in C# backed by SQL.";

    private static readonly DateTime ApplicationDate =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    public static TheoryData<string?> InvalidCvTexts => new()
    {
        null,
        "",
        "   ",
        new string('a', ReviewCandidacyWithAiHandler.MaximumCvLength + 1)
    };

    [Fact]
    public async Task Given_ExistingCandidacyAndOffer_When_Reviewing_Then_ReturnsSuccessWithMappedReview()
    {
        // Arrange
        var (handler, candidacy, offer, reviewer, _) = CreateSut("C#, SQL, Docker");

        // Act
        var result = await handler.HandleAsync(new ReviewCandidacyWithAiCommand(candidacy.Id, Cv));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value!.MatchedSkills.Should().Equal("C#", "SQL");
        result.Value.MissingOrUnclearSkills.Should().Equal("Docker");
        result.Value.Evidence.Select(item => item.Skill).Should().Equal("C#", "SQL");
        result.Value.Evidence[0].Excerpt.Should().Contain("C#");
        result.Value.Summary.Should().Be("Matched 2 of 3 required skills.");
        reviewer.LastOfferRequirements.Should().Be(offer.Requirements);
        reviewer.CallCount.Should().Be(1);
    }

    [Theory]
    [MemberData(nameof(InvalidCvTexts))]
    public async Task Given_InvalidCvText_When_Reviewing_Then_ReturnsValidationAndDoesNotCallAi(string? cvText)
    {
        // Arrange
        var (handler, candidacy, _, reviewer, _) = CreateSut("C#");

        // Act
        var result = await handler.HandleAsync(new ReviewCandidacyWithAiCommand(candidacy.Id, cvText));

        // Assert
        result.Status.Should().Be(ResultStatus.Validation);
        reviewer.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_CvTextAtMaximumLength_When_Reviewing_Then_CallsAiAndReturnsSuccess()
    {
        // Arrange
        var (handler, candidacy, _, reviewer, _) = CreateSut("C#");
        var cvText = new string('a', ReviewCandidacyWithAiHandler.MaximumCvLength);

        // Act
        var result = await handler.HandleAsync(new ReviewCandidacyWithAiCommand(candidacy.Id, cvText));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        reviewer.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Given_MissingCandidacy_When_Reviewing_Then_ReturnsNotFoundAndDoesNotCallAi()
    {
        // Arrange
        var (handler, _, _, reviewer, _) = CreateSut("C#");
        var missingCandidacyId = Guid.NewGuid();

        // Act
        var result = await handler.HandleAsync(new ReviewCandidacyWithAiCommand(missingCandidacyId, Cv));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Be($"Candidacy {missingCandidacyId} not found.");
        reviewer.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_CandidacyWhoseOfferIsMissing_When_Reviewing_Then_ReturnsNotFoundAndDoesNotCallAi()
    {
        // Arrange
        var candidacies = new FakeInternshipCandidacyRepository();
        var candidacy = CreateCandidacy(Guid.NewGuid());
        candidacies.Seed(candidacy);
        var reviewer = new FakeRecruiterCandidacyReviewService();
        var handler = new ReviewCandidacyWithAiHandler(
            candidacies,
            new FakeOfferRepository(),
            reviewer,
            FakeRecruiterAccessGuard.AuthorizedAs(RecruiterId));

        // Act
        var result = await handler.HandleAsync(new ReviewCandidacyWithAiCommand(candidacy.Id, Cv));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Be($"Offer of candidacy {candidacy.Id} not found.");
        reviewer.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_UnavailableProvider_When_Reviewing_Then_ReturnsServiceUnavailableWithoutCvText()
    {
        // Arrange
        var (handler, candidacy, _, reviewer, _) = CreateSut("C#");
        reviewer.SimulateUnavailable = true;

        // Act
        var result = await handler.HandleAsync(new ReviewCandidacyWithAiCommand(candidacy.Id, Cv));

        // Assert
        result.Status.Should().Be(ResultStatus.ServiceUnavailable);
        result.Error.Should().Be("AI review is temporarily unavailable.");
        result.Error.Should().NotContain(Cv);
        reviewer.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Given_ClientCancelledRequest_When_Reviewing_Then_PropagatesOperationCanceledException()
    {
        // Arrange
        var (handler, candidacy, _, reviewer, _) = CreateSut("C#");
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        var act = () => handler.HandleAsync(
            new ReviewCandidacyWithAiCommand(candidacy.Id, Cv),
            cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        reviewer.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Given_SubmittedCandidacy_When_Reviewing_Then_KeepsStatusAndNeverUpdates()
    {
        // Arrange
        var (handler, candidacy, _, _, candidacies) = CreateSut("C#");

        // Act
        await handler.HandleAsync(new ReviewCandidacyWithAiCommand(candidacy.Id, Cv));

        // Assert
        candidacies.GetById(candidacy.Id)!.Status.Should().Be(InternshipCandidacyStatus.Submitted);
        candidacies.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_AnonymousUserAndInvalidCv_When_Reviewing_Then_ReturnsUnauthorizedAndDoesNotCallAi()
    {
        // Arrange
        var (handler, candidacy, _, reviewer, _) = CreateSut("C#", guard: FakeRecruiterAccessGuard.Unauthorized());

        // Act
        var result = await handler.HandleAsync(new ReviewCandidacyWithAiCommand(candidacy.Id, null));

        // Assert
        result.Status.Should().Be(ResultStatus.Unauthorized);
        reviewer.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_NonRecruiterAndInvalidCv_When_Reviewing_Then_ReturnsForbiddenAndDoesNotCallAi()
    {
        // Arrange
        var (handler, candidacy, _, reviewer, _) = CreateSut("C#", guard: FakeRecruiterAccessGuard.Forbidden());

        // Act
        var result = await handler.HandleAsync(new ReviewCandidacyWithAiCommand(candidacy.Id, null));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        reviewer.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_UserFromCommand_When_Reviewing_Then_PassesItToRecruiterAccessGuard()
    {
        // Arrange
        var guard = FakeRecruiterAccessGuard.AuthorizedAs(RecruiterId);
        var (handler, candidacy, _, _, _) = CreateSut("C#", guard: guard);
        var user = new System.Security.Claims.ClaimsPrincipal();

        // Act
        await handler.HandleAsync(new ReviewCandidacyWithAiCommand(candidacy.Id, Cv, user));

        // Assert
        guard.CallCount.Should().Be(1);
        guard.LastUser.Should().BeSameAs(user);
    }

    [Fact]
    public async Task Given_OfferOwnedByAnotherRecruiter_When_Reviewing_Then_ReturnsForbiddenAndDoesNotCallAi()
    {
        // Arrange
        var (handler, candidacy, _, reviewer, _) = CreateSut("C#", offerRecruiterId: Guid.NewGuid());

        // Act
        var result = await handler.HandleAsync(new ReviewCandidacyWithAiCommand(candidacy.Id, Cv));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Error.Should().Be($"Only the owning Recruiter can review candidacy {candidacy.Id}.");
        reviewer.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_LegacyOfferWithoutOwner_When_Reviewing_Then_ReturnsForbiddenAndDoesNotCallAi()
    {
        // Arrange
        var (handler, candidacy, _, reviewer, _) = CreateSut("C#", legacyOffer: true);

        // Act
        var result = await handler.HandleAsync(new ReviewCandidacyWithAiCommand(candidacy.Id, Cv));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        reviewer.CallCount.Should().Be(0);
    }

    private static readonly Guid RecruiterId = Guid.NewGuid();

    private static (
        ReviewCandidacyWithAiHandler Handler,
        InternshipCandidacy Candidacy,
        Offer Offer,
        FakeRecruiterCandidacyReviewService Reviewer,
        FakeInternshipCandidacyRepository Candidacies) CreateSut(
        string requirements,
        FakeRecruiterAccessGuard? guard = null,
        Guid? offerRecruiterId = null,
        bool legacyOffer = false)
    {
        var offer = legacyOffer
            ? new Offer("Backend Intern", "Example Co", requirements, ApplicationDate.AddDays(10), 2)
            : new Offer(
                "Backend Intern",
                "Example Co",
                requirements,
                ApplicationDate.AddDays(10),
                2,
                offerRecruiterId ?? RecruiterId);
        var offers = new FakeOfferRepository();
        offers.Offers.Add(offer);
        var candidacy = CreateCandidacy(offer.Id);
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(candidacy);
        var reviewer = new FakeRecruiterCandidacyReviewService();
        var handler = new ReviewCandidacyWithAiHandler(
            candidacies,
            offers,
            reviewer,
            guard ?? FakeRecruiterAccessGuard.AuthorizedAs(RecruiterId));

        return (handler, candidacy, offer, reviewer, candidacies);
    }

    private static InternshipCandidacy CreateCandidacy(Guid offerId) =>
        new(Guid.NewGuid(), Guid.NewGuid(), offerId, ApplicationDate, InternshipCandidacyStatus.Submitted);
}
