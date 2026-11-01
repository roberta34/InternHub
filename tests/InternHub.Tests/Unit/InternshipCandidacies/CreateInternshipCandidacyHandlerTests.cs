using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.CreateInternshipCandidacy;
using InternHub.Api.Features.Shared;
using InternHub.Domain.Students;
using InternHub.Tests.Unit.InternshipCandidacies.Fakes;
using InternHub.Tests.Unit.Offers.Fakes;
using InternHub.Tests.Unit.Students.Fakes;

namespace InternHub.Tests.Unit.InternshipCandidacies;

public sealed class CreateInternshipCandidacyHandlerTests
{
    private static readonly DateTime Now = new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Given_EligibleStudentAndOpenOffer_When_Applying_Then_CreatesSubmittedCandidacy()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var result = context.Handler.Handle(
            new CreateInternshipCandidacyCommand(context.Student.Id, context.Offer.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().NotBeEmpty();
        result.Value.StudentId.Should().Be(context.Student.Id);
        result.Value.OfferId.Should().Be(context.Offer.Id);
        result.Value.ApplicationDate.Should().Be(Now);
        result.Value.Status.Should().Be(InternshipCandidacyStatus.Submitted);
        context.Candidacies.Candidacies.Should().ContainSingle()
            .Which.Id.Should().Be(result.Value.Id);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Given_AnEmptyStudentOrOfferId_When_Applying_Then_ReturnsValidation(bool emptyStudentId)
    {
        // Arrange
        var context = CreateContext();
        var studentId = emptyStudentId ? Guid.Empty : context.Student.Id;
        var offerId = emptyStudentId ? context.Offer.Id : Guid.Empty;

        // Act
        var result = context.Handler.Handle(new CreateInternshipCandidacyCommand(studentId, offerId));

        // Assert
        result.Status.Should().Be(ResultStatus.Validation);
        context.Candidacies.AddCallCount.Should().Be(0);
    }

    [Fact]
    public void Given_MissingStudent_When_Applying_Then_ReturnsNotFound()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var result = context.Handler.Handle(
            new CreateInternshipCandidacyCommand(Guid.NewGuid(), context.Offer.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        context.Candidacies.AddCallCount.Should().Be(0);
    }

    [Fact]
    public void Given_MissingOffer_When_Applying_Then_ReturnsNotFound()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var result = context.Handler.Handle(
            new CreateInternshipCandidacyCommand(context.Student.Id, Guid.NewGuid()));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        context.Candidacies.AddCallCount.Should().Be(0);
    }

    [Fact]
    public void Given_ClosedOffer_When_Applying_Then_ReturnsConflict()
    {
        // Arrange
        var context = CreateContext();
        context.Offer.Close();

        // Act
        var result = context.Handler.Handle(
            new CreateInternshipCandidacyCommand(context.Student.Id, context.Offer.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        context.Candidacies.AddCallCount.Should().Be(0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void Given_DeadlineAtOrBeforeNow_When_Applying_Then_ReturnsConflict(int deadlineOffsetMinutes)
    {
        // Arrange
        var context = CreateContext(Now.AddMinutes(deadlineOffsetMinutes));

        // Act
        var result = context.Handler.Handle(
            new CreateInternshipCandidacyCommand(context.Student.Id, context.Offer.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        context.Candidacies.AddCallCount.Should().Be(0);
    }

    [Fact]
    public void Given_ActiveCandidacyForSameOffer_When_Applying_Then_ReturnsConflict()
    {
        // Arrange
        var context = CreateContext();
        SeedCandidacy(
            context.Candidacies,
            context.Student.Id,
            context.Offer.Id,
            Now.AddDays(-1),
            InternshipCandidacyStatus.Submitted);

        // Act
        var result = context.Handler.Handle(
            new CreateInternshipCandidacyCommand(context.Student.Id, context.Offer.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        context.Candidacies.AddCallCount.Should().Be(0);
    }

    [Theory]
    [InlineData(InternshipCandidacyStatus.Withdrawn)]
    [InlineData(InternshipCandidacyStatus.Rejected)]
    public void Given_WithdrawnOrRejectedCandidacy_When_ApplyingAgain_Then_CandidacyIsNotActive(
        InternshipCandidacyStatus previousStatus)
    {
        // Arrange
        var context = CreateContext();
        SeedCandidacy(
            context.Candidacies,
            context.Student.Id,
            context.Offer.Id,
            Now.AddDays(-1),
            previousStatus);

        // Act
        var result = context.Handler.Handle(
            new CreateInternshipCandidacyCommand(context.Student.Id, context.Offer.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        context.Candidacies.Candidacies.Should().HaveCount(2);
    }

    [Fact]
    public void Given_FourActiveCandidacies_When_Applying_Then_Succeeds()
    {
        // Arrange
        var context = CreateContext();
        SeedActiveCandidacies(context, 4);

        // Act
        var result = context.Handler.Handle(
            new CreateInternshipCandidacyCommand(context.Student.Id, context.Offer.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        context.Candidacies.Candidacies.Should().HaveCount(5);
    }

    [Fact]
    public void Given_FiveActiveCandidacies_When_Applying_Then_ReturnsConflict()
    {
        // Arrange
        var context = CreateContext();
        SeedActiveCandidacies(context, 5);

        // Act
        var result = context.Handler.Handle(
            new CreateInternshipCandidacyCommand(context.Student.Id, context.Offer.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        context.Candidacies.Candidacies.Should().HaveCount(5);
        context.Candidacies.AddCallCount.Should().Be(0);
    }

    [Fact]
    public void Given_RepositoryRejectsAdd_When_Applying_Then_ReturnsConflict()
    {
        // Arrange
        var context = CreateContext();
        context.Candidacies.AddResult = false;

        // Act
        var result = context.Handler.Handle(
            new CreateInternshipCandidacyCommand(context.Student.Id, context.Offer.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        context.Candidacies.Candidacies.Should().BeEmpty();
        context.Candidacies.AddCallCount.Should().Be(1);
    }

    private static HandlerContext CreateContext(DateTime? deadline = null)
    {
        var students = new FakeStudentRepository();
        var student = new Student(Guid.NewGuid(), "Test Student", "student@example.test");
        students.Add(student);

        var offers = new FakeOfferRepository();
        var offer = CreateOffer(deadline ?? Now.AddDays(1));
        offers.Add(offer);

        var candidacies = new FakeInternshipCandidacyRepository();
        var handler = new CreateInternshipCandidacyHandler(
            candidacies,
            students,
            offers,
            new FixedTimeProvider(Now));

        return new HandlerContext(handler, candidacies, student, offer);
    }

    private static Offer CreateOffer(DateTime deadline) =>
        new("Software Intern", "Example Co", ".NET", deadline, 1);

    private static void SeedActiveCandidacies(HandlerContext context, int count) =>
        Enumerable.Range(0, count).ToList().ForEach(index =>
            SeedCandidacy(
                context.Candidacies,
                context.Student.Id,
                Guid.NewGuid(),
                Now.AddDays(-index - 1),
                InternshipCandidacyStatus.Submitted));

    private static void SeedCandidacy(
        FakeInternshipCandidacyRepository repository,
        Guid studentId,
        Guid offerId,
        DateTime applicationDate,
        InternshipCandidacyStatus status) =>
        repository.Seed(new InternshipCandidacy(
            Guid.NewGuid(),
            studentId,
            offerId,
            applicationDate,
            status));

    private sealed record HandlerContext(
        CreateInternshipCandidacyHandler Handler,
        FakeInternshipCandidacyRepository Candidacies,
        Student Student,
        Offer Offer);

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
