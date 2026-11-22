using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.GetStudentCandidacies;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Domain.Students;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.InternshipCandidacies.GetStudentCandidacies;

[Collection("Integration")]
public sealed class GetStudentCandidaciesEndpointTests
{
    private static readonly DateTime Now =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_DefaultProductionRegistrationAndAnonymousRequest_When_GettingCandidacies_Then_ReturnsUnauthorizedWithoutServerError()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(
            new FixedTimeProvider(Now),
            authenticateRecruiter: false);
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var offer = CreateOffer("Offer", "Company");
        var candidacy = CreateCandidacy(student.Id, offer.Id, Now);
        await SeedAsync(factory, [student], [offer], [candidacy]);

        // Act
        using var response = await client.GetAsync($"/api/v1/students/{student.Id}/candidacies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ReadCandidacyCountAsync(factory)).Should().Be(1);
    }

    [Fact]
    public async Task Given_AnonymousRequestWithTestAuthPipeline_When_GettingCandidacies_Then_ReturnsUnauthorized()
    {
        // Arrange
        using var factory = StudentTestAuthentication.CreateFactory(new FixedTimeProvider(Now));
        using var client = factory.CreateClient();
        var student = CreateStudent();
        await SeedAsync(factory, [student], []);

        // Act
        using var response = await client.GetAsync($"/api/v1/students/{student.Id}/candidacies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ReadCandidacyCountAsync(factory)).Should().Be(0);
    }

    [Fact]
    public async Task Given_AuthenticatedRecruiter_When_GettingStudentCandidacies_Then_ReturnsForbiddenAndPreservesDatabase()
    {
        // Arrange
        using var factory = StudentTestAuthentication.CreateFactory(new FixedTimeProvider(Now));
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var offer = CreateOffer("Offer", "Company");
        var candidacy = CreateCandidacy(student.Id, offer.Id, Now);
        await SeedAsync(factory, [student], [offer], [candidacy]);
        using var request = StudentTestAuthentication.CreateRecruiterGetRequest(
            $"/api/v1/students/{student.Id}/candidacies",
            Guid.NewGuid(),
            subjectId: "account-recruiter");

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ReadCandidacyCountAsync(factory)).Should().Be(1);
    }

    [Fact]
    public async Task Given_TwoStudentsWithCandidacies_When_StudentARequestsStudentBCandidacies_Then_ReturnsForbiddenWithoutExposingData()
    {
        // Arrange
        using var factory = StudentTestAuthentication.CreateFactory(new FixedTimeProvider(Now));
        using var client = factory.CreateClient();
        var studentA = CreateStudent();
        var studentB = CreateStudent();
        var offerA = CreateOffer("Offer A", "Company A");
        var offerB = CreateOffer("Offer B", "Company B");
        var candidacyA = CreateCandidacy(studentA.Id, offerA.Id, Now.AddDays(-1));
        var candidacyB = CreateCandidacy(studentB.Id, offerB.Id, Now);
        await SeedAsync(factory, [studentA, studentB], [offerA, offerB], [candidacyA, candidacyB]);
        using var request = StudentTestAuthentication.CreateStudentGetRequest(
            $"/api/v1/students/{studentB.Id}/candidacies",
            studentA.Id.ToString(),
            subjectId: "account-student-a");

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ReadCandidacyCountAsync(factory)).Should().Be(2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-valid-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Given_AuthenticatedStudentWithInvalidIdentity_When_GettingCandidacies_Then_ReturnsForbidden(
        string? invalidStudentId)
    {
        // Arrange
        using var factory = StudentTestAuthentication.CreateFactory(new FixedTimeProvider(Now));
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var offer = CreateOffer("Offer", "Company");
        var candidacy = CreateCandidacy(student.Id, offer.Id, Now);
        await SeedAsync(factory, [student], [offer], [candidacy]);
        using var request = StudentTestAuthentication.CreateStudentGetRequest(
            $"/api/v1/students/{student.Id}/candidacies",
            invalidStudentId,
            subjectId: student.Id.ToString());

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ReadCandidacyCountAsync(factory)).Should().Be(1);
    }

    [Fact]
    public async Task Given_TwoStudentsWithCandidacies_When_StudentAGetsOwnCandidacies_Then_ReturnsOnlyStudentANewestFirstAndPreservesDatabase()
    {
        // Arrange
        using var factory = StudentTestAuthentication.CreateFactory(new FixedTimeProvider(Now));
        using var client = factory.CreateClient();
        var studentA = CreateStudent();
        var studentB = CreateStudent();
        var olderOffer = CreateOffer("Older Offer", "Older Company");
        var newerOffer = CreateOffer("Newer Offer", "Newer Company");
        var otherStudentOffer = CreateOffer("Other Offer", "Other Company");
        var olderCandidacy = CreateCandidacy(studentA.Id, olderOffer.Id, Now.AddDays(-1));
        var newerCandidacy = CreateCandidacy(studentA.Id, newerOffer.Id, Now);
        var otherStudentCandidacy = CreateCandidacy(studentB.Id, otherStudentOffer.Id, Now.AddHours(1));
        await SeedAsync(
            factory,
            [studentA, studentB],
            [olderOffer, newerOffer, otherStudentOffer],
            [olderCandidacy, newerCandidacy, otherStudentCandidacy]);
        var beforeCandidacies = await ReadCandidaciesAsync(factory);
        using var request = StudentTestAuthentication.CreateStudentGetRequest(
            $"/api/v1/students/{studentA.Id}/candidacies",
            studentA.Id.ToString(),
            subjectId: "account-student-a");

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetStudentCandidaciesResponse[]>();
        body.Should().NotBeNull();
        body.Should().HaveCount(2);
        body![0].Id.Should().Be(newerCandidacy.Id);
        body[0].OfferId.Should().Be(newerOffer.Id);
        body[0].OfferTitle.Should().Be("Newer Offer");
        body[0].CompanyName.Should().Be("Newer Company");
        body[0].ApplicationDate.Should().Be(Now);
        body[0].Status.Should().Be(InternshipCandidacyStatus.Submitted);
        body[1].Id.Should().Be(olderCandidacy.Id);
        body[1].OfferTitle.Should().Be("Older Offer");
        body[1].CompanyName.Should().Be("Older Company");
        body[1].ApplicationDate.Should().Be(Now.AddDays(-1));
        body.Should().NotContain(item => item.Id == otherStudentCandidacy.Id);

        var persistedCandidacies = await ReadCandidaciesAsync(factory);
        persistedCandidacies.Should().HaveCount(3);
        persistedCandidacies.Should().BeEquivalentTo(
            beforeCandidacies,
            options => options.WithStrictOrdering());
        persistedCandidacies.Should().ContainEquivalentOf(olderCandidacy);
        persistedCandidacies.Should().ContainEquivalentOf(newerCandidacy);
        persistedCandidacies.Should().ContainEquivalentOf(otherStudentCandidacy);
        persistedCandidacies.Single(item => item.Id == olderCandidacy.Id).Status
            .Should().Be(InternshipCandidacyStatus.Submitted);
        persistedCandidacies.Single(item => item.Id == newerCandidacy.Id).Status
            .Should().Be(InternshipCandidacyStatus.Submitted);
        persistedCandidacies.Single(item => item.Id == otherStudentCandidacy.Id).Status
            .Should().Be(InternshipCandidacyStatus.Submitted);
    }

    [Fact]
    public async Task Given_StudentWithoutCandidacies_When_GettingOwnCandidacies_Then_ReturnsEmptyList()
    {
        // Arrange
        using var factory = StudentTestAuthentication.CreateFactory(new FixedTimeProvider(Now));
        using var client = factory.CreateClient();
        var student = CreateStudent();
        await SeedAsync(factory, [student], []);
        using var request = StudentTestAuthentication.CreateStudentGetRequest(
            $"/api/v1/students/{student.Id}/candidacies",
            student.Id.ToString(),
            subjectId: "account-student-empty");

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<GetStudentCandidaciesResponse[]>())
            .Should().BeEmpty();
        (await ReadCandidacyCountAsync(factory)).Should().Be(0);
    }

    [Fact]
    public async Task Given_AuthenticatedStudentWithMissingRecord_When_GettingOwnCandidacies_Then_ReturnsNotFound()
    {
        // Arrange
        using var factory = StudentTestAuthentication.CreateFactory(new FixedTimeProvider(Now));
        using var client = factory.CreateClient();
        var studentId = Guid.NewGuid();
        using var request = StudentTestAuthentication.CreateStudentGetRequest(
            $"/api/v1/students/{studentId}/candidacies",
            studentId.ToString(),
            subjectId: "account-student-missing");

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Contain($"Student {studentId} not found.");

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await verificationDb.Students.AsNoTracking().CountAsync()).Should().Be(0);
        (await verificationDb.InternshipCandidacies.AsNoTracking().CountAsync()).Should().Be(0);
    }

    private static Student CreateStudent() =>
        new(Guid.NewGuid(), "Integration Student", $"student-{Guid.NewGuid():N}@example.test");

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

    private static async Task<int> ReadCandidacyCountAsync(InternHubSqliteFactory factory)
    {
        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        return await verificationDb.InternshipCandidacies.AsNoTracking().CountAsync();
    }

    private static async Task<InternshipCandidacy[]> ReadCandidaciesAsync(InternHubSqliteFactory factory)
    {
        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        return await verificationDb.InternshipCandidacies
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .ToArrayAsync();
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
