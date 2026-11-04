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
    public async Task Given_StudentWithCandidacies_When_GettingCandidacies_Then_ReturnsNewestFirstWithOfferDetails()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(new FixedTimeProvider(Now));
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var olderOffer = CreateOffer("Older Offer", "Older Company");
        var newerOffer = CreateOffer("Newer Offer", "Newer Company");
        var olderCandidacy = CreateCandidacy(student.Id, olderOffer.Id, Now.AddDays(-1));
        var newerCandidacy = CreateCandidacy(student.Id, newerOffer.Id, Now);
        await SeedAsync(factory, [student], [olderOffer, newerOffer], [olderCandidacy, newerCandidacy]);

        // Act
        using var response = await client.GetAsync($"/api/v1/students/{student.Id}/candidacies");

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

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await verificationDb.InternshipCandidacies.AsNoTracking().CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Given_StudentWithoutCandidacies_When_GettingCandidacies_Then_ReturnsEmptyList()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(new FixedTimeProvider(Now));
        using var client = factory.CreateClient();
        var student = CreateStudent();
        await SeedAsync(factory, [student], []);

        // Act
        using var response = await client.GetAsync($"/api/v1/students/{student.Id}/candidacies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<GetStudentCandidaciesResponse[]>())
            .Should().BeEmpty();

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await verificationDb.InternshipCandidacies.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Given_MissingStudent_When_GettingCandidacies_Then_ReturnsNotFound()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(new FixedTimeProvider(Now));
        using var client = factory.CreateClient();
        var studentId = Guid.NewGuid();

        // Act
        using var response = await client.GetAsync($"/api/v1/students/{studentId}/candidacies");

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

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
