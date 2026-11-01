using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Features.InternshipCandidacies.GetInternshipCandidacyById;
using InternHub.Api.Features.Shared;
using InternHub.Tests.Unit.InternshipCandidacies.Fakes;

namespace InternHub.Tests.Unit.InternshipCandidacies;

public sealed class GetInternshipCandidacyByIdHandlerTests
{
    private static readonly Guid CandidacyId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid StudentId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly Guid OfferId =
        Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly DateTime ApplicationDate =
        new(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Given_ExistingCandidacyId_When_GettingCandidacyById_Then_ReturnsAllCandidacyFields()
    {
        // Arrange
        var candidacy = new InternshipCandidacy(
            CandidacyId,
            StudentId,
            OfferId,
            ApplicationDate,
            InternshipCandidacyStatus.Shortlisted);
        var repository = new FakeInternshipCandidacyRepository();
        repository.Candidacies.Add(candidacy);
        var handler = new GetInternshipCandidacyByIdHandler(repository);

        // Act
        var result = handler.Handle(new GetInternshipCandidacyByIdQuery(CandidacyId));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeEquivalentTo(new GetInternshipCandidacyByIdResponse(
            CandidacyId,
            StudentId,
            OfferId,
            ApplicationDate,
            InternshipCandidacyStatus.Shortlisted));
    }

    [Fact]
    public void Given_UnknownCandidacyId_When_GettingCandidacyById_Then_ReturnsNotFoundWithoutValue()
    {
        // Arrange
        var repository = new FakeInternshipCandidacyRepository();
        var handler = new GetInternshipCandidacyByIdHandler(repository);

        // Act
        var result = handler.Handle(new GetInternshipCandidacyByIdQuery(CandidacyId));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Contain($"Internship candidacy {CandidacyId} was not found.");
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Given_ExistingCandidacy_When_GettingCandidacyById_Then_DoesNotModifyCandidacyOrUpdateRepository()
    {
        // Arrange
        var candidacy = new InternshipCandidacy(
            CandidacyId,
            StudentId,
            OfferId,
            ApplicationDate,
            InternshipCandidacyStatus.Submitted);
        var repository = new FakeInternshipCandidacyRepository();
        repository.Candidacies.Add(candidacy);
        var handler = new GetInternshipCandidacyByIdHandler(repository);

        // Act
        handler.Handle(new GetInternshipCandidacyByIdQuery(CandidacyId));

        // Assert
        candidacy.Id.Should().Be(CandidacyId);
        candidacy.StudentId.Should().Be(StudentId);
        candidacy.OfferId.Should().Be(OfferId);
        candidacy.ApplicationDate.Should().Be(ApplicationDate);
        candidacy.Status.Should().Be(InternshipCandidacyStatus.Submitted);
        repository.UpdateCallCount.Should().Be(0);
    }
}