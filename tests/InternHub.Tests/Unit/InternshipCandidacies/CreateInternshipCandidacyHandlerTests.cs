using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Features.InternshipCandidacies.AtomicSubmission;
using InternHub.Api.Features.InternshipCandidacies.CreateInternshipCandidacy;
using InternHub.Api.Features.Shared;

namespace InternHub.Tests.Unit.InternshipCandidacies;

public sealed class CreateInternshipCandidacyHandlerTests
{
    private static readonly DateTime ApplicationDate =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_AtomicSubmissionSucceeds_When_Applying_Then_ReturnsCreatedCandidacy()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var candidacy = CreateCandidacy(studentId, offerId);
        var submission = new FakeAtomicCandidacySubmission
        {
            Result = AtomicCandidacySubmissionResult.Success(candidacy)
        };
        var handler = new CreateInternshipCandidacyHandler(submission);
        using var cancellationSource = new CancellationTokenSource();

        // Act
        var result = await handler.HandleAsync(
            new CreateInternshipCandidacyCommand(studentId, offerId),
            cancellationSource.Token);

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().Be(new CreateInternshipCandidacyResponse(
            candidacy.Id,
            studentId,
            offerId,
            ApplicationDate,
            InternshipCandidacyStatus.Submitted));
        submission.StudentId.Should().Be(studentId);
        submission.OfferId.Should().Be(offerId);
        submission.CancellationToken.Should().Be(cancellationSource.Token);
        submission.CallCount.Should().Be(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Given_EmptyIdentifier_When_Applying_Then_ReturnsValidationWithoutAtomicSubmit(
        bool emptyStudentId)
    {
        // Arrange
        var studentId = emptyStudentId ? Guid.Empty : Guid.NewGuid();
        var offerId = emptyStudentId ? Guid.NewGuid() : Guid.Empty;
        var submission = new FakeAtomicCandidacySubmission();
        var handler = new CreateInternshipCandidacyHandler(submission);

        // Act
        var result = await handler.HandleAsync(
            new CreateInternshipCandidacyCommand(studentId, offerId),
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.Validation);
        result.Error.Should().Contain("must not be empty");
        submission.CallCount.Should().Be(0);
    }

    [Theory]
    [InlineData(AtomicCandidacySubmissionStatus.StudentNotFound, ResultStatus.NotFound, "Student")]
    [InlineData(AtomicCandidacySubmissionStatus.OfferNotFound, ResultStatus.NotFound, "Offer")]
    [InlineData(AtomicCandidacySubmissionStatus.OfferClosed, ResultStatus.Conflict, "closed")]
    [InlineData(AtomicCandidacySubmissionStatus.DeadlineExpired, ResultStatus.Conflict, "deadline")]
    [InlineData(AtomicCandidacySubmissionStatus.DuplicateActive, ResultStatus.Conflict, "active candidacy")]
    [InlineData(AtomicCandidacySubmissionStatus.ActiveLimitReached, ResultStatus.Conflict, "maximum of 5")]
    public async Task Given_AtomicSubmissionBusinessFailure_When_Applying_Then_MapsFailureToResult(
        AtomicCandidacySubmissionStatus submissionStatus,
        ResultStatus expectedResultStatus,
        string expectedMessage)
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var submission = new FakeAtomicCandidacySubmission
        {
            Result = AtomicCandidacySubmissionResult.Failure(submissionStatus)
        };
        var handler = new CreateInternshipCandidacyHandler(submission);

        // Act
        var result = await handler.HandleAsync(
            new CreateInternshipCandidacyCommand(studentId, offerId),
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(expectedResultStatus);
        result.Error.Should().Contain(expectedMessage);
        submission.CallCount.Should().Be(1);
    }

    private static InternshipCandidacy CreateCandidacy(Guid studentId, Guid offerId) =>
        new(
            Guid.NewGuid(),
            studentId,
            offerId,
            ApplicationDate,
            InternshipCandidacyStatus.Submitted);

    private sealed class FakeAtomicCandidacySubmission : IAtomicCandidacySubmission
    {
        public AtomicCandidacySubmissionResult? Result { get; init; }
        public int CallCount { get; private set; }
        public Guid StudentId { get; private set; }
        public Guid OfferId { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task<AtomicCandidacySubmissionResult> SubmitAsync(
            Guid studentId,
            Guid offerId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            StudentId = studentId;
            OfferId = offerId;
            CancellationToken = cancellationToken;

            return Task.FromResult(
                Result ?? AtomicCandidacySubmissionResult.Failure(
                    AtomicCandidacySubmissionStatus.StudentNotFound));
        }
    }
}
