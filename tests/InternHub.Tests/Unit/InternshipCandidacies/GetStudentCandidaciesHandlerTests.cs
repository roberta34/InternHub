using System.Security.Claims;
using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.GetStudentCandidacies;
using InternHub.Api.Features.InternshipCandidacies.StudentAccess;
using InternHub.Api.Features.Shared;
using InternHub.Domain.Students;
using InternHub.Tests.Unit.InternshipCandidacies.Fakes;
using InternHub.Tests.Unit.Offers.Fakes;
using InternHub.Tests.Unit.Students.Fakes;

namespace InternHub.Tests.Unit.InternshipCandidacies;

public sealed class GetStudentCandidaciesHandlerTests
{
    private const string StudentIdClaimType = "student_id";
    private const string RecruiterIdClaimType = "recruiter_id";
    private const string SubjectClaimType = "sub";

    private static readonly DateTime Now = new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_ExistingStudentWithCandidacies_When_QueryingOwnList_Then_ReturnsNewestFirstWithOfferDetails()
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

        var handler = new GetStudentCandidaciesHandler(
            candidacies,
            students,
            offers,
            new ClaimsPrincipalStudentAccessGuard());
        var principal = CreateStudentPrincipal(student.Id.ToString(), sub: "account-student-1");

        // Act
        var result = await handler.HandleAsync(new GetStudentCandidaciesQuery(student.Id, principal));

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
    public async Task Given_CandidaciesForMultipleStudents_When_QueryingOwnList_Then_ExcludesOtherStudents()
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

        var handler = new GetStudentCandidaciesHandler(
            candidacies,
            students,
            offers,
            new ClaimsPrincipalStudentAccessGuard());
        var principal = CreateStudentPrincipal(student.Id.ToString());

        // Act
        var result = await handler.HandleAsync(new GetStudentCandidaciesQuery(student.Id, principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().ContainSingle()
            .Which.Id.Should().Be(ownCandidacy.Id);
        result.Value.Should().NotContain(item => item.Id == otherCandidacy.Id);
    }

    [Fact]
    public async Task Given_AuthenticatedStudentWithMissingStudentRecord_When_QueryingOwnList_Then_ReturnsNotFound()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var handler = new GetStudentCandidaciesHandler(
            new FakeInternshipCandidacyRepository(),
            new FakeStudentRepository(),
            new FakeOfferRepository(),
            new ClaimsPrincipalStudentAccessGuard());
        var principal = CreateStudentPrincipal(studentId.ToString());

        // Act
        var result = await handler.HandleAsync(new GetStudentCandidaciesQuery(studentId, principal));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Given_ExistingStudentWithoutCandidacies_When_QueryingOwnList_Then_ReturnsEmptyCollection()
    {
        // Arrange
        var student = new Student(Guid.NewGuid(), "Test Student", "student@example.test");
        var students = new FakeStudentRepository();
        students.Add(student);
        var candidacies = new FakeInternshipCandidacyRepository();
        var handler = new GetStudentCandidaciesHandler(
            candidacies,
            students,
            new FakeOfferRepository(),
            new ClaimsPrincipalStudentAccessGuard());
        var principal = CreateStudentPrincipal(student.Id.ToString());

        // Act
        var result = await handler.HandleAsync(new GetStudentCandidaciesQuery(student.Id, principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_ExistingCandidacies_When_QueryingOwnList_Then_DoesNotChangeRepositoryState()
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

        var handler = new GetStudentCandidaciesHandler(
            candidacies,
            students,
            offers,
            new ClaimsPrincipalStudentAccessGuard());
        var principal = CreateStudentPrincipal(student.Id.ToString());

        // Act
        var result = await handler.HandleAsync(new GetStudentCandidaciesQuery(student.Id, principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        candidacies.Candidacies.Should().Equal(before);
        candidacies.AddCallCount.Should().Be(0);
        candidacies.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_CandidaciesWithSameApplicationDate_When_QueryingOwnList_Then_OrdersByIdAscending()
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

        var handler = new GetStudentCandidaciesHandler(
            candidacies,
            students,
            offers,
            new ClaimsPrincipalStudentAccessGuard());
        var principal = CreateStudentPrincipal(student.Id.ToString());

        // Act
        var result = await handler.HandleAsync(new GetStudentCandidaciesQuery(student.Id, principal));

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Select(item => item.Id).Should().Equal(firstById.Id, secondById.Id);
    }

    [Fact]
    public async Task Given_CandidacyReferencesMissingOffer_When_QueryingOwnList_Then_ThrowsInvalidOperationException()
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
            new FakeOfferRepository(),
            new ClaimsPrincipalStudentAccessGuard());
        var principal = CreateStudentPrincipal(student.Id.ToString());

        // Act
        Func<Task> act = async () =>
            await handler.HandleAsync(new GetStudentCandidaciesQuery(student.Id, principal));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*referenced by candidacy*was not found*");
    }

    [Fact]
    public async Task Given_UnauthenticatedRequest_When_Querying_Then_ReturnsUnauthorizedWithoutTouchingRepositories()
    {
        // Arrange
        var student = new Student(Guid.NewGuid(), "Test Student", "student@example.test");
        var offer = CreateOffer("Offer", "Company");
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(CreateCandidacy(student.Id, offer.Id, Now));
        var students = new FakeStudentRepository();
        students.Add(student);
        var offers = new FakeOfferRepository();
        offers.Add(offer);
        var handler = new GetStudentCandidaciesHandler(
            candidacies,
            students,
            offers,
            new ClaimsPrincipalStudentAccessGuard());

        // Act
        var result = await handler.HandleAsync(
            new GetStudentCandidaciesQuery(student.Id, new ClaimsPrincipal(new ClaimsIdentity())));

        // Assert
        result.Status.Should().Be(ResultStatus.Unauthorized);
        result.Value.Should().BeNull();
        students.GetByIdCallCount.Should().Be(0);
        candidacies.GetByStudentCallCount.Should().Be(0);
        offers.GetByIdCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_StudentA_When_QueryingStudentBList_Then_ReturnsForbiddenWithoutTouchingRepositories()
    {
        // Arrange
        var studentA = new Student(Guid.NewGuid(), "Student A", "a@example.test");
        var studentB = new Student(Guid.NewGuid(), "Student B", "b@example.test");
        var offer = CreateOffer("Offer", "Company");
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(CreateCandidacy(studentB.Id, offer.Id, Now));
        var students = new FakeStudentRepository();
        students.Add(studentA);
        students.Add(studentB);
        var offers = new FakeOfferRepository();
        offers.Add(offer);
        var handler = new GetStudentCandidaciesHandler(
            candidacies,
            students,
            offers,
            new ClaimsPrincipalStudentAccessGuard());
        var principalA = CreateStudentPrincipal(studentA.Id.ToString(), sub: "account-a");

        // Act
        var result = await handler.HandleAsync(new GetStudentCandidaciesQuery(studentB.Id, principalA));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Value.Should().BeNull();
        students.GetByIdCallCount.Should().Be(0);
        candidacies.GetByStudentCallCount.Should().Be(0);
        offers.GetByIdCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_AuthenticatedRecruiter_When_QueryingStudentList_Then_ReturnsForbiddenWithoutTouchingRepositories()
    {
        // Arrange
        var student = new Student(Guid.NewGuid(), "Student", "student@example.test");
        var offer = CreateOffer("Offer", "Company");
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(CreateCandidacy(student.Id, offer.Id, Now));
        var students = new FakeStudentRepository();
        students.Add(student);
        var offers = new FakeOfferRepository();
        offers.Add(offer);
        var handler = new GetStudentCandidaciesHandler(
            candidacies,
            students,
            offers,
            new ClaimsPrincipalStudentAccessGuard());
        var recruiterPrincipal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Role, "Recruiter"),
                    new Claim(RecruiterIdClaimType, Guid.NewGuid().ToString()),
                    new Claim(SubjectClaimType, "account-recruiter")
                ],
                authenticationType: "TestScheme"));

        // Act
        var result = await handler.HandleAsync(new GetStudentCandidaciesQuery(student.Id, recruiterPrincipal));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Value.Should().BeNull();
        students.GetByIdCallCount.Should().Be(0);
        candidacies.GetByStudentCallCount.Should().Be(0);
        offers.GetByIdCallCount.Should().Be(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Given_InvalidStudentIdentityClaim_When_QueryingList_Then_ReturnsForbiddenWithoutTouchingRepositories(
        string? invalidStudentId)
    {
        // Arrange
        var student = new Student(Guid.NewGuid(), "Student", "student@example.test");
        var candidacies = new FakeInternshipCandidacyRepository();
        var students = new FakeStudentRepository();
        students.Add(student);
        var offers = new FakeOfferRepository();
        var handler = new GetStudentCandidaciesHandler(
            candidacies,
            students,
            offers,
            new ClaimsPrincipalStudentAccessGuard());
        var principal = CreateStudentPrincipal(invalidStudentId, sub: student.Id.ToString());

        // Act
        var result = await handler.HandleAsync(new GetStudentCandidaciesQuery(student.Id, principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Value.Should().BeNull();
        students.GetByIdCallCount.Should().Be(0);
        candidacies.GetByStudentCallCount.Should().Be(0);
        offers.GetByIdCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_DuplicateOrCrossIdentityStudentClaims_When_QueryingList_Then_ReturnsForbiddenWithoutTouchingRepositories()
    {
        // Arrange
        var student = new Student(Guid.NewGuid(), "Student", "student@example.test");
        var candidacies = new FakeInternshipCandidacyRepository();
        var students = new FakeStudentRepository();
        students.Add(student);
        var offers = new FakeOfferRepository();
        var handler = new GetStudentCandidaciesHandler(
            candidacies,
            students,
            offers,
            new ClaimsPrincipalStudentAccessGuard());
        var splitPrincipal = new ClaimsPrincipal(
            [
                new ClaimsIdentity([new Claim(ClaimTypes.Role, "Student")], authenticationType: "SchemeA"),
                new ClaimsIdentity([new Claim(StudentIdClaimType, student.Id.ToString())], authenticationType: "SchemeB")
            ]);
        var duplicatePrincipal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Role, "Student"),
                    new Claim(StudentIdClaimType, student.Id.ToString()),
                    new Claim(StudentIdClaimType, student.Id.ToString())
                ],
                authenticationType: "TestScheme"));

        // Act
        var splitResult = await handler.HandleAsync(new GetStudentCandidaciesQuery(student.Id, splitPrincipal));
        var duplicateResult = await handler.HandleAsync(new GetStudentCandidaciesQuery(student.Id, duplicatePrincipal));

        // Assert
        splitResult.Status.Should().Be(ResultStatus.Forbidden);
        duplicateResult.Status.Should().Be(ResultStatus.Forbidden);
        students.GetByIdCallCount.Should().Be(0);
        candidacies.GetByStudentCallCount.Should().Be(0);
        offers.GetByIdCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_AuthorizedQueryWithCancellationToken_When_Handling_Then_PropagatesCancellationTokenToAccessGuard()
    {
        // Arrange
        var student = new Student(Guid.NewGuid(), "Test Student", "student@example.test");
        var students = new FakeStudentRepository();
        students.Add(student);
        var guard = new FakeStudentAccessGuard();
        var handler = new GetStudentCandidaciesHandler(
            new FakeInternshipCandidacyRepository(),
            students,
            new FakeOfferRepository(),
            guard);
        using var cancellation = new CancellationTokenSource();
        var principal = CreateStudentPrincipal(student.Id.ToString());

        // Act
        var result = await handler.HandleAsync(
            new GetStudentCandidaciesQuery(student.Id, principal),
            cancellation.Token);

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        guard.AuthorizeCallCount.Should().Be(1);
        guard.LastRequestedStudentId.Should().Be(student.Id);
        guard.LastCancellationToken.Should().Be(cancellation.Token);
    }

    [Fact]
    public async Task Given_CanceledToken_When_Handling_Then_ThrowsOperationCanceledExceptionWithoutTouchingRepositories()
    {
        // Arrange
        var student = new Student(Guid.NewGuid(), "Test Student", "student@example.test");
        var students = new FakeStudentRepository();
        students.Add(student);
        var candidacies = new FakeInternshipCandidacyRepository();
        var handler = new GetStudentCandidaciesHandler(
            candidacies,
            students,
            new FakeOfferRepository(),
            new ClaimsPrincipalStudentAccessGuard());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        // Act
        Func<Task> act = async () => await handler.HandleAsync(
            new GetStudentCandidaciesQuery(student.Id, CreateStudentPrincipal(student.Id.ToString())),
            cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        students.GetByIdCallCount.Should().Be(0);
        candidacies.GetByStudentCallCount.Should().Be(0);
    }

    private static ClaimsPrincipal CreateStudentPrincipal(
        string? studentId,
        string? sub = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, "Student") };

        if (studentId is not null)
        {
            claims.Add(new Claim(StudentIdClaimType, studentId));
        }

        if (sub is not null)
        {
            claims.Add(new Claim(SubjectClaimType, sub));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestScheme"));
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
