using System.Net;
using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Domain.Students;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.InternshipCandidacies.WithdrawInternshipCandidacy;

[Collection("Integration")]
public sealed class WithdrawInternshipCandidacyEndpointTests
{
    private static readonly DateTime ApplicationDate =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(InternshipCandidacyStatus.Submitted)]
    [InlineData(InternshipCandidacyStatus.Shortlisted)]
    public async Task Given_OpenCandidacy_When_Withdrawing_Then_ReturnsNoContentAndPersistsWithdrawn(
        InternshipCandidacyStatus initialStatus)
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var offer = CreateOffer();
        var candidacy = CreateCandidacy(student.Id, offer.Id, initialStatus);
        await SeedAsync(factory, student, offer, candidacy);

        // Act
        using var response = await client.PostAsync(
            $"/api/v1/candidacies/{candidacy.Id}/withdraw",
            content: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
        (await ReadStatusAsync(factory, candidacy.Id)).Should().Be(InternshipCandidacyStatus.Withdrawn);
    }

    [Theory]
    [InlineData(InternshipCandidacyStatus.Accepted)]
    [InlineData(InternshipCandidacyStatus.Rejected)]
    [InlineData(InternshipCandidacyStatus.Waitlisted)]
    [InlineData(InternshipCandidacyStatus.Withdrawn)]
    public async Task Given_FinalCandidacy_When_Withdrawing_Then_ReturnsConflictAndPreservesStatus(
        InternshipCandidacyStatus finalStatus)
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var offer = CreateOffer();
        var candidacy = CreateCandidacy(student.Id, offer.Id, finalStatus);
        await SeedAsync(factory, student, offer, candidacy);

        // Act
        using var response = await client.PostAsync(
            $"/api/v1/candidacies/{candidacy.Id}/withdraw",
            content: null);
        var error = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        error.Should().Contain(
            $"Candidacy {candidacy.Id} cannot be withdrawn from status {finalStatus}.");
        (await ReadStatusAsync(factory, candidacy.Id)).Should().Be(finalStatus);
    }

    [Fact]
    public async Task Given_MissingCandidacy_When_Withdrawing_Then_ReturnsNotFoundAndLeavesDatabaseUnchanged()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var candidacyId = Guid.NewGuid();

        // Act
        using var response = await client.PostAsync(
            $"/api/v1/candidacies/{candidacyId}/withdraw",
            content: null);
        var error = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        error.Should().Contain($"Candidacy {candidacyId} not found.");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await db.InternshipCandidacies.AsNoTracking().CountAsync()).Should().Be(0);
    }

    private static Student CreateStudent() =>
        new(Guid.NewGuid(), "Integration Student", $"student-{Guid.NewGuid():N}@example.test");

    private static Offer CreateOffer() =>
        new("Software Intern", "Example Company", ".NET", ApplicationDate.AddDays(10), 1);

    private static InternshipCandidacy CreateCandidacy(
        Guid studentId,
        Guid offerId,
        InternshipCandidacyStatus status) =>
        new(Guid.NewGuid(), studentId, offerId, ApplicationDate, status);

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

    private static async Task<InternshipCandidacyStatus> ReadStatusAsync(
        InternHubSqliteFactory factory,
        Guid candidacyId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();

        return await db.InternshipCandidacies
            .AsNoTracking()
            .Where(item => item.Id == candidacyId)
            .Select(item => item.Status)
            .SingleAsync();
    }
}
