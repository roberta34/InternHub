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
    public async Task Given_StudentWithAllStatuses_When_GettingSummary_Then_ReturnsAllCounts()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
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

        // Act
        using var response = await client.GetAsync(
            $"/api/v1/students/{student.Id}/candidacies/summary");
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
    public async Task Given_StudentWithoutCandidacies_When_GettingSummary_Then_ReturnsZeroCounts()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var student = CreateStudent();
        await SeedAsync(factory, [student], []);

        // Act
        using var response = await client.GetAsync(
            $"/api/v1/students/{student.Id}/candidacies/summary");
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
    public async Task Given_MissingStudent_When_GettingSummary_Then_ReturnsNotFound()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var studentId = Guid.NewGuid();

        // Act
        using var response = await client.GetAsync(
            $"/api/v1/students/{studentId}/candidacies/summary");
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
    public async Task Given_CandidaciesForMultipleStudents_When_GettingSummary_Then_ReturnsOnlyRequestedStudentAndPreservesState()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
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

        // Act
        using var response = await client.GetAsync(
            $"/api/v1/students/{student.Id}/candidacies/summary");
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

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb =
            verificationScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        var persistedCandidacies = await verificationDb.InternshipCandidacies
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .ToArrayAsync();
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
}
