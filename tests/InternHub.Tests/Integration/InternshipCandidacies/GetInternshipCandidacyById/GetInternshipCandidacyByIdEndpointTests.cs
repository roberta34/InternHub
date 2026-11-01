using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.GetInternshipCandidacyById;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Domain.Students;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.InternshipCandidacies.GetInternshipCandidacyById;

[Collection("Integration")]
public sealed class GetInternshipCandidacyByIdEndpointTests
{
    private static readonly DateTime ApplicationDate =
        new(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime Deadline =
        new(2027, 2, 15, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_ExistingCandidacy_When_GettingById_Then_ReturnsCandidacyAndPreservesDatabaseState()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var student = new Student(
            Guid.NewGuid(),
            "Integration Student",
            $"integration-{Guid.NewGuid():N}@example.com");
        var offer = new Offer(
            "Backend Intern",
            "Example Company",
            "C#, Git",
            Deadline,
            2);
        var candidacy = new InternshipCandidacy(
            Guid.NewGuid(),
            student.Id,
            offer.Id,
            ApplicationDate,
            InternshipCandidacyStatus.Shortlisted);
        await SeedAsync(factory, student, offer, candidacy);

        // Act
        using var response = await client.GetAsync(
            $"/api/v1/candidacies/{candidacy.Id}");
        var body = await response.Content.ReadFromJsonAsync<GetInternshipCandidacyByIdResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().BeEquivalentTo(new GetInternshipCandidacyByIdResponse(
            candidacy.Id,
            student.Id,
            offer.Id,
            ApplicationDate,
            InternshipCandidacyStatus.Shortlisted));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        var persisted = await db.InternshipCandidacies
            .AsNoTracking()
            .SingleAsync(item => item.Id == candidacy.Id);
        persisted.Should().BeEquivalentTo(candidacy);
    }

    [Fact]
    public async Task Given_MissingCandidacy_When_GettingById_Then_ReturnsNotFoundAndLeavesDatabaseUnchanged()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var candidacyId = Guid.NewGuid();

        // Act
        using var response = await client.GetAsync(
            $"/api/v1/candidacies/{candidacyId}");
        var error = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        error.Should().Contain($"Internship candidacy {candidacyId} was not found.");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await db.InternshipCandidacies.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Given_ExistingCandidacy_When_GettingById_Then_DoesNotModifyPersistedCandidacy()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var student = new Student(
            Guid.NewGuid(),
            "Read Only Student",
            $"readonly-{Guid.NewGuid():N}@example.com");
        var offer = new Offer(
            "Data Intern",
            "Example Company",
            "SQL, C#",
            Deadline,
            1);
        var candidacy = new InternshipCandidacy(
            Guid.NewGuid(),
            student.Id,
            offer.Id,
            ApplicationDate,
            InternshipCandidacyStatus.Submitted);
        await SeedAsync(factory, student, offer, candidacy);

        // Act
        using var response = await client.GetAsync(
            $"/api/v1/candidacies/{candidacy.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        var persisted = await db.InternshipCandidacies
            .AsNoTracking()
            .SingleAsync(item => item.Id == candidacy.Id);
        persisted.Id.Should().Be(candidacy.Id);
        persisted.StudentId.Should().Be(candidacy.StudentId);
        persisted.OfferId.Should().Be(candidacy.OfferId);
        persisted.ApplicationDate.Should().Be(candidacy.ApplicationDate);
        persisted.Status.Should().Be(candidacy.Status);
    }

    private static async Task SeedAsync(
        InternHubSqliteFactory factory,
        Student student,
        Offer offer,
        InternshipCandidacy candidacy)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        db.Students.Add(student);
        db.Offers.Add(offer);
        db.InternshipCandidacies.Add(candidacy);
        await db.SaveChangesAsync();
    }
}