using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.GetStudentCandidacySummary;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Domain.Students;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.InternshipCandidacies.GetStudentCandidacySummary;

[Collection("Integration")]
public sealed class GetStudentCandidacySummaryEndpointTests
{
    private static readonly DateTime ApplicationDate =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime Deadline =
        new(2027, 2, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_DefaultProductionRegistrationAndAnonymousRequest_When_GettingSummary_Then_ReturnsUnauthorizedWithoutServerError()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var offer = CreateOffer("Offer", "Company");
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Submitted);
        await SeedAsync(factory, [student], [offer], [candidacy]);

        // Act
        using var response = await client.GetAsync(
            $"/api/v1/students/{student.Id}/candidacies/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ReadCandidaciesAsync(factory)).Should().ContainSingle();
    }

    [Fact]
    public async Task Given_AnonymousRequestWithTestAuthPipeline_When_GettingSummary_Then_ReturnsUnauthorized()
    {
        // Arrange
        using var factory = StudentTestAuthentication.CreateFactory();
        using var client = factory.CreateClient();
        var student = CreateStudent();
        await SeedAsync(factory, [student], []);

        // Act
        using var response = await client.GetAsync(
            $"/api/v1/students/{student.Id}/candidacies/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ReadCandidaciesAsync(factory)).Should().BeEmpty();
    }

    [Fact]
    public async Task Given_AuthenticatedRecruiter_When_GettingStudentSummary_Then_ReturnsForbiddenAndPreservesDatabase()
    {
        // Arrange
        using var factory = StudentTestAuthentication.CreateFactory();
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var offer = CreateOffer("Offer", "Company");
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Shortlisted);
        await SeedAsync(factory, [student], [offer], [candidacy]);
        using var request = StudentTestAuthentication.CreateRecruiterGetRequest(
            $"/api/v1/students/{student.Id}/candidacies/summary",
            Guid.NewGuid(),
            subjectId: "account-recruiter");

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ReadCandidaciesAsync(factory)).Should().ContainSingle();
    }

    [Fact]
    public async Task Given_TwoStudentsWithCandidacies_When_StudentARequestsStudentBSummary_Then_ReturnsForbiddenWithoutExposingData()
    {
        // Arrange
        using var factory = StudentTestAuthentication.CreateFactory();
        using var client = factory.CreateClient();
        var studentA = CreateStudent();
        var studentB = CreateStudent();
        var offerA = CreateOffer("Offer A", "Company A");
        var offerB = CreateOffer("Offer B", "Company B");
        var candidacyA = CreateCandidacy(studentA.Id, offerA.Id, InternshipCandidacyStatus.Submitted);
        var candidacyB = CreateCandidacy(studentB.Id, offerB.Id, InternshipCandidacyStatus.Accepted);
        await SeedAsync(factory, [studentA, studentB], [offerA, offerB], [candidacyA, candidacyB]);
        using var request = StudentTestAuthentication.CreateStudentGetRequest(
            $"/api/v1/students/{studentB.Id}/candidacies/summary",
            studentA.Id.ToString(),
            subjectId: "account-student-a");

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ReadCandidaciesAsync(factory)).Should().HaveCount(2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-valid-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Given_AuthenticatedStudentWithInvalidIdentity_When_GettingSummary_Then_ReturnsForbidden(
        string? invalidStudentId)
    {
        // Arrange
        using var factory = StudentTestAuthentication.CreateFactory();
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var offer = CreateOffer("Offer", "Company");
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Submitted);
        await SeedAsync(factory, [student], [offer], [candidacy]);
        using var request = StudentTestAuthentication.CreateStudentGetRequest(
            $"/api/v1/students/{student.Id}/candidacies/summary",
            invalidStudentId,
            subjectId: student.Id.ToString());

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ReadCandidaciesAsync(factory)).Should().ContainSingle();
    }

    [Fact]
    public async Task Given_StudentWithAllStatuses_When_GettingOwnSummary_Then_ReturnsAllCounts()
    {
        // Arrange
        using var factory = StudentTestAuthentication.CreateFactory();
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var statuses = new[]
        {
            InternshipCandidacyStatus.Submitted,
            InternshipCandidacyStatus.Shortlisted,
            InternshipCandidacyStatus.Accepted,
            InternshipCandidacyStatus.Rejected,
            InternshipCandidacyStatus.Waitlisted,
            InternshipCandidacyStatus.Withdrawn
        };
        var offers = statuses.Select((_, index) =>
                CreateOffer($"Offer {index}", $"Company {index}"))
            .ToArray();
        var candidacies = statuses
            .Select((status, index) =>
                CreateCandidacy(student.Id, offers[index].Id, status))
            .ToArray();
        await SeedAsync(factory, [student], offers, candidacies);
        using var request = StudentTestAuthentication.CreateStudentGetRequest(
            $"/api/v1/students/{student.Id}/candidacies/summary",
            student.Id.ToString(),
            subjectId: "account-student-all");

        // Act
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<GetStudentCandidacySummaryResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().BeEquivalentTo(new GetStudentCandidacySummaryResponse(
            student.Id,
            6,
            4,
            1,
            1,
            1,
            1,
            1,
            1));
    }

    [Fact]
    public async Task Given_StudentWithoutCandidacies_When_GettingOwnSummary_Then_ReturnsZeroCounts()
    {
        // Arrange
        using var factory = StudentTestAuthentication.CreateFactory();
        using var client = factory.CreateClient();
        var student = CreateStudent();
        await SeedAsync(factory, [student], []);
        using var request = StudentTestAuthentication.CreateStudentGetRequest(
            $"/api/v1/students/{student.Id}/candidacies/summary",
            student.Id.ToString(),
            subjectId: "account-student-zero");

        // Act
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<GetStudentCandidacySummaryResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().BeEquivalentTo(new GetStudentCandidacySummaryResponse(
            student.Id,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0));
    }

    [Fact]
    public async Task Given_AuthenticatedStudentWithMissingRecord_When_GettingOwnSummary_Then_ReturnsNotFound()
    {
        // Arrange
        using var factory = StudentTestAuthentication.CreateFactory();
        using var client = factory.CreateClient();
        var studentId = Guid.NewGuid();
        using var request = StudentTestAuthentication.CreateStudentGetRequest(
            $"/api/v1/students/{studentId}/candidacies/summary",
            studentId.ToString(),
            subjectId: "account-student-missing");

        // Act
        using var response = await client.SendAsync(request);
        var error = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        error.Should().Contain($"Student {studentId} not found.");

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb =
            verificationScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await verificationDb.Students.AsNoTracking().CountAsync()).Should().Be(0);
        (await verificationDb.InternshipCandidacies.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Given_CandidaciesForMultipleStudents_When_GettingOwnSummary_Then_ReturnsOnlyRequestedStudentAndPreservesState()
    {
        // Arrange
        using var factory = StudentTestAuthentication.CreateFactory();
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var otherStudent = CreateStudent();
        var studentOffer = CreateOffer("Requested Offer", "Requested Company");
        var otherOffer = CreateOffer("Other Offer", "Other Company");
        var studentCandidacy = CreateCandidacy(
            student.Id,
            studentOffer.Id,
            InternshipCandidacyStatus.Accepted);
        var otherCandidacy = CreateCandidacy(
            otherStudent.Id,
            otherOffer.Id,
            InternshipCandidacyStatus.Submitted);
        await SeedAsync(
            factory,
            [student, otherStudent],
            [studentOffer, otherOffer],
            [studentCandidacy, otherCandidacy]);
        using var request = StudentTestAuthentication.CreateStudentGetRequest(
            $"/api/v1/students/{student.Id}/candidacies/summary",
            student.Id.ToString(),
            subjectId: "account-student-own");

        // Act
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<GetStudentCandidacySummaryResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().BeEquivalentTo(new GetStudentCandidacySummaryResponse(
            student.Id,
            1,
            1,
            0,
            0,
            1,
            0,
            0,
            0));

        var persistedCandidacies = await ReadCandidaciesAsync(factory);
        persistedCandidacies.Should().HaveCount(2);
        persistedCandidacies.Should().ContainEquivalentOf(studentCandidacy);
        persistedCandidacies.Should().ContainEquivalentOf(otherCandidacy);
        persistedCandidacies.Single(item => item.Id == studentCandidacy.Id).Status
            .Should().Be(InternshipCandidacyStatus.Accepted);
        persistedCandidacies.Single(item => item.Id == otherCandidacy.Id).Status
            .Should().Be(InternshipCandidacyStatus.Submitted);
    }

    private static Student CreateStudent() =>
        new(
            Guid.NewGuid(),
            "Integration Student",
            $"student-{Guid.NewGuid():N}@example.test");

    private static Offer CreateOffer(string title, string companyName) =>
        new(title, companyName, ".NET, SQL", Deadline, 1);

    private static InternshipCandidacy CreateCandidacy(
        Guid studentId,
        Guid offerId,
        InternshipCandidacyStatus status) =>
        new(
            Guid.NewGuid(),
            studentId,
            offerId,
            ApplicationDate,
            status);

    private static async Task SeedAsync(
        InternHubSqliteFactory factory,
        IReadOnlyCollection<Student> students,
        IReadOnlyCollection<Offer> offers,
        IReadOnlyCollection<InternshipCandidacy>? candidacies = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        db.Students.AddRange(students);
        db.Offers.AddRange(offers);

        if (candidacies is not null)
            db.InternshipCandidacies.AddRange(candidacies);

        await db.SaveChangesAsync();
    }

    private static async Task<InternshipCandidacy[]> ReadCandidaciesAsync(InternHubSqliteFactory factory)
    {
        using var verificationScope = factory.Services.CreateScope();
        var verificationDb =
            verificationScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        return await verificationDb.InternshipCandidacies
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .ToArrayAsync();
    }
}
