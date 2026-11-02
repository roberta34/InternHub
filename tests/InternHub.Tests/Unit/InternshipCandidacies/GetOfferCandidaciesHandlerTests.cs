using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.GetOfferCandidacies;
using InternHub.Api.Features.Shared;
using InternHub.Domain.Students;
using InternHub.Tests.Unit.InternshipCandidacies.Fakes;
using InternHub.Tests.Unit.Offers.Fakes;
using InternHub.Tests.Unit.Students.Fakes;

namespace InternHub.Tests.Unit.InternshipCandidacies;

public sealed class GetOfferCandidaciesHandlerTests
{
    private static readonly DateTime Now = new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Given_MissingOffer_When_Querying_Then_ReturnsNotFound()
    {
        // Arrange
        var offerId = Guid.NewGuid();
        var handler = new GetOfferCandidaciesHandler(
            new FakeInternshipCandidacyRepository(),
            new FakeOfferRepository(),
            new FakeStudentRepository());

        // Act
        var result = handler.Handle(new GetOfferCandidaciesQuery(offerId));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Be($"Offer {offerId} not found.");
    }

    [Fact]
    public void Given_OfferWithoutCandidacies_When_Querying_Then_ReturnsEmptyList()
    {
        // Arrange
        var offer = CreateOffer();
        var offers = new FakeOfferRepository();
        offers.Add(offer);
        var handler = new GetOfferCandidaciesHandler(
            new FakeInternshipCandidacyRepository(),
            offers,
            new FakeStudentRepository());

        // Act
        var result = handler.Handle(new GetOfferCandidaciesQuery(offer.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public void Given_CandidaciesForMultipleOffers_When_Querying_Then_ReturnsOnlyRequestedOfferWithStudentName()
    {
        // Arrange
        var offer = CreateOffer();
        var otherOffer = CreateOffer();
        var student = new Student(Guid.NewGuid(), "Test Student", "student@example.test");
        var ownCandidacy = new InternshipCandidacy(
            Guid.NewGuid(),
            student.Id,
            offer.Id,
            Now,
            InternshipCandidacyStatus.Shortlisted);
        var otherCandidacy = new InternshipCandidacy(
            Guid.NewGuid(),
            student.Id,
            otherOffer.Id,
            Now,
            InternshipCandidacyStatus.Submitted);
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(ownCandidacy);
        candidacies.Seed(otherCandidacy);

        var offers = new FakeOfferRepository();
        offers.Add(offer);
        offers.Add(otherOffer);

        var students = new FakeStudentRepository();
        students.Add(student);

        var handler = new GetOfferCandidaciesHandler(candidacies, offers, students);

        // Act
        var result = handler.Handle(new GetOfferCandidaciesQuery(offer.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().ContainSingle()
            .Which.Should().Be(new GetOfferCandidaciesResponse(
                ownCandidacy.Id,
                student.Id,
                "Test Student",
                Now,
                InternshipCandidacyStatus.Shortlisted));
    }

    [Fact]
    public void Given_CandidaciesWithDifferentAndEqualDates_When_Querying_Then_OrdersByDateDescendingThenId()
    {
        // Arrange
        var offer = CreateOffer();
        var student = new Student(Guid.NewGuid(), "Test Student", "student@example.test");
        var older = CreateCandidacy("00000000-0000-0000-0000-000000000001", student.Id, offer.Id, Now.AddDays(-1));
        var sameDateSecondById = CreateCandidacy("00000000-0000-0000-0000-000000000003", student.Id, offer.Id, Now);
        var sameDateFirstById = CreateCandidacy("00000000-0000-0000-0000-000000000002", student.Id, offer.Id, Now);
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(older);
        candidacies.Seed(sameDateSecondById);
        candidacies.Seed(sameDateFirstById);

        var offers = new FakeOfferRepository();
        offers.Add(offer);

        var students = new FakeStudentRepository();
        students.Add(student);

        var handler = new GetOfferCandidaciesHandler(candidacies, offers, students);

        // Act
        var result = handler.Handle(new GetOfferCandidaciesQuery(offer.Id));

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Select(item => item.Id).Should().Equal(
            sameDateFirstById.Id,
            sameDateSecondById.Id,
            older.Id);
    }

    [Fact]
    public void Given_ExistingCandidacies_When_Querying_Then_DoesNotCallAddOrUpdate()
    {
        // Arrange
        var offer = CreateOffer();
        var student = new Student(Guid.NewGuid(), "Test Student", "student@example.test");
        var candidacy = CreateCandidacy(Guid.NewGuid().ToString(), student.Id, offer.Id, Now);
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(candidacy);
        var before = candidacies.Candidacies.ToArray();

        var offers = new FakeOfferRepository();
        offers.Add(offer);

        var students = new FakeStudentRepository();
        students.Add(student);

        var handler = new GetOfferCandidaciesHandler(candidacies, offers, students);

        // Act
        var result = handler.Handle(new GetOfferCandidaciesQuery(offer.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        candidacies.Candidacies.Should().Equal(before);
        candidacies.AddCallCount.Should().Be(0);
        candidacies.UpdateCallCount.Should().Be(0);
        candidacy.Status.Should().Be(InternshipCandidacyStatus.Submitted);
    }

    [Fact]
    public void Given_CandidacyReferencesMissingStudent_When_Querying_Then_ThrowsInvalidOperationException()
    {
        // Arrange
        var offer = CreateOffer();
        var candidacy = CreateCandidacy(Guid.NewGuid().ToString(), Guid.NewGuid(), offer.Id, Now);
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(candidacy);
        var offers = new FakeOfferRepository();
        offers.Add(offer);
        var handler = new GetOfferCandidaciesHandler(candidacies, offers, new FakeStudentRepository());

        // Act
        var act = () => handler.Handle(new GetOfferCandidaciesQuery(offer.Id));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*referenced by candidacy*was not found*");
    }

    private static Offer CreateOffer() =>
        new("Offer", "Company", ".NET", Now.AddDays(10), 1);

    private static InternshipCandidacy CreateCandidacy(
        string id,
        Guid studentId,
        Guid offerId,
        DateTime applicationDate) =>
        new(
            Guid.Parse(id),
            studentId,
            offerId,
            applicationDate,
            InternshipCandidacyStatus.Submitted);
}
