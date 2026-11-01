using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.GetStudentCandidacies;
using InternHub.Api.Features.Shared;
using InternHub.Domain.Students;
using InternHub.Tests.Unit.InternshipCandidacies.Fakes;
using InternHub.Tests.Unit.Offers.Fakes;
using InternHub.Tests.Unit.Students.Fakes;

namespace InternHub.Tests.Unit.InternshipCandidacies;

public sealed class GetStudentCandidaciesHandlerTests
{
    private static readonly DateTime Now = new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Given_ExistingStudentWithCandidacies_When_Querying_Then_ReturnsNewestFirstWithOfferDetails()
    {
        // Arrange
        var student = new Student(Guid.NewGuid(), "Test Student", "student@example.test");
        var newerOffer = CreateOffer("New Offer", "New Company");
        var olderOffer = CreateOffer("Old Offer", "Old Company");
        var older = CreateCandidacy(student.Id, olderOffer.Id, Now.AddDays(-1));
        var newer = CreateCandidacy(student.Id, newerOffer.Id, Now);
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(older);
        candidacies.Seed(newer);

        var students = new FakeStudentRepository();
        students.Add(student);

        var offers = new FakeOfferRepository();
        offers.Add(olderOffer);
        offers.Add(newerOffer);

        var handler = new GetStudentCandidaciesHandler(candidacies, students, offers);

        // Act
        var result = handler.Handle(new GetStudentCandidaciesQuery(student.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(2);
        result.Value![0].Id.Should().Be(newer.Id);
        result.Value[0].OfferTitle.Should().Be("New Offer");
        result.Value[0].CompanyName.Should().Be("New Company");
        result.Value[0].Status.Should().Be(InternshipCandidacyStatus.Submitted);
        result.Value[1].Id.Should().Be(older.Id);
        result.Value[1].OfferTitle.Should().Be("Old Offer");
        result.Value[1].CompanyName.Should().Be("Old Company");
    }

    [Fact]
    public void Given_CandidaciesForMultipleStudents_When_Querying_Then_ExcludesOtherStudents()
    {
        // Arrange
        var student = new Student(Guid.NewGuid(), "Test Student", "student@example.test");
        var otherStudent = new Student(Guid.NewGuid(), "Other Student", "other@example.test");
        var offer = CreateOffer("Offer", "Company");
        var ownCandidacy = CreateCandidacy(student.Id, offer.Id, Now);
        var otherCandidacy = CreateCandidacy(otherStudent.Id, offer.Id, Now);
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(ownCandidacy);
        candidacies.Seed(otherCandidacy);

        var students = new FakeStudentRepository();
        students.Add(student);
        students.Add(otherStudent);

        var offers = new FakeOfferRepository();
        offers.Add(offer);

        var handler = new GetStudentCandidaciesHandler(candidacies, students, offers);

        // Act
        var result = handler.Handle(new GetStudentCandidaciesQuery(student.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().ContainSingle()
            .Which.Id.Should().Be(ownCandidacy.Id);
        result.Value.Should().NotContain(item => item.Id == otherCandidacy.Id);
    }

    [Fact]
    public void Given_MissingStudent_When_Querying_Then_ReturnsNotFound()
    {
        // Arrange
        var handler = new GetStudentCandidaciesHandler(
            new FakeInternshipCandidacyRepository(),
            new FakeStudentRepository(),
            new FakeOfferRepository());

        // Act
        var result = handler.Handle(new GetStudentCandidaciesQuery(Guid.NewGuid()));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public void Given_ExistingStudentWithoutCandidacies_When_Querying_Then_ReturnsEmptyCollection()
    {
        // Arrange
        var student = new Student(Guid.NewGuid(), "Test Student", "student@example.test");
        var students = new FakeStudentRepository();
        students.Add(student);
        var candidacies = new FakeInternshipCandidacyRepository();
        var handler = new GetStudentCandidaciesHandler(
            candidacies,
            students,
            new FakeOfferRepository());

        // Act
        var result = handler.Handle(new GetStudentCandidaciesQuery(student.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public void Given_ExistingCandidacies_When_Querying_Then_DoesNotChangeRepositoryState()
    {
        // Arrange
        var student = new Student(Guid.NewGuid(), "Test Student", "student@example.test");
        var offer = CreateOffer("Offer", "Company");
        var candidacy = CreateCandidacy(student.Id, offer.Id, Now);
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(candidacy);
        var before = candidacies.Candidacies.ToArray();

        var students = new FakeStudentRepository();
        students.Add(student);

        var offers = new FakeOfferRepository();
        offers.Add(offer);

        var handler = new GetStudentCandidaciesHandler(candidacies, students, offers);

        // Act
        var result = handler.Handle(new GetStudentCandidaciesQuery(student.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        candidacies.Candidacies.Should().Equal(before);
        candidacies.AddCallCount.Should().Be(0);
        candidacies.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public void Given_CandidaciesWithSameApplicationDate_When_Querying_Then_OrdersByIdAscending()
    {
        // Arrange
        var student = new Student(Guid.NewGuid(), "Test Student", "student@example.test");
        var firstOffer = CreateOffer("First Offer", "Company");
        var secondOffer = CreateOffer("Second Offer", "Company");
        var firstById = new InternshipCandidacy(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            student.Id,
            firstOffer.Id,
            Now,
            InternshipCandidacyStatus.Submitted);
        var secondById = new InternshipCandidacy(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            student.Id,
            secondOffer.Id,
            Now,
            InternshipCandidacyStatus.Submitted);
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(secondById);
        candidacies.Seed(firstById);

        var students = new FakeStudentRepository();
        students.Add(student);

        var offers = new FakeOfferRepository();
        offers.Add(firstOffer);
        offers.Add(secondOffer);

        var handler = new GetStudentCandidaciesHandler(candidacies, students, offers);

        // Act
        var result = handler.Handle(new GetStudentCandidaciesQuery(student.Id));

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Select(item => item.Id).Should().Equal(firstById.Id, secondById.Id);
    }

    [Fact]
    public void Given_CandidacyReferencesMissingOffer_When_Querying_Then_ThrowsInvalidOperationException()
    {
        // Arrange
        var student = new Student(Guid.NewGuid(), "Test Student", "student@example.test");
        var candidacy = CreateCandidacy(student.Id, Guid.NewGuid(), Now);
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(candidacy);
        var students = new FakeStudentRepository();
        students.Add(student);
        var handler = new GetStudentCandidaciesHandler(
            candidacies,
            students,
            new FakeOfferRepository());

        // Act
        var act = () => handler.Handle(new GetStudentCandidaciesQuery(student.Id));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*referenced by candidacy*was not found*");
    }

    private static Offer CreateOffer(string title, string companyName) =>
        new(title, companyName, ".NET", Now.AddDays(10), 1);

    private static InternshipCandidacy CreateCandidacy(
        Guid studentId,
        Guid offerId,
        DateTime applicationDate) =>
        new(
            Guid.NewGuid(),
            studentId,
            offerId,
            applicationDate,
            InternshipCandidacyStatus.Submitted);
}
