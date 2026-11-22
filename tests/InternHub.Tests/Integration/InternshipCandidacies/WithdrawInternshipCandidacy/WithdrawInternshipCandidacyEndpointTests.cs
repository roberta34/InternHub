using System.Net;
using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Notifications;
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

    private static readonly DateTime WithdrawTimestampUtc =
        new(2027, 2, 10, 15, 45, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_SubmittedCandidacy_When_Withdrawing_Then_ReturnsNoContentAndPersistsWithdrawnWithoutNotification()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(new FixedTimeProvider(WithdrawTimestampUtc));
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var offer = CreateOffer();
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Submitted);
        await SeedAsync(factory, student, offer, candidacy);

        // Act
        using var response = await client.PostAsync(
            $"/api/v1/candidacies/{candidacy.Id}/withdraw",
            content: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
        (await ReadStatusAsync(factory, candidacy.Id)).Should().Be(InternshipCandidacyStatus.Withdrawn);
        (await ReadNotificationsAsync(factory)).Should().BeEmpty();
    }

    [Fact]
    public async Task Given_ShortlistedCandidacy_When_Withdrawing_Then_ReturnsNoContentAndPersistsWithdrawnWithNotification()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(new FixedTimeProvider(WithdrawTimestampUtc));
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var offer = CreateOffer();
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Shortlisted);
        await SeedAsync(factory, student, offer, candidacy);

        // Act
        using var response = await client.PostAsync(
            $"/api/v1/candidacies/{candidacy.Id}/withdraw",
            content: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
        (await ReadStatusAsync(factory, candidacy.Id)).Should().Be(InternshipCandidacyStatus.Withdrawn);

        var notifications = await ReadNotificationsAsync(factory);
        var notification = notifications.Should().ContainSingle().Subject;
        notification.CandidacyId.Should().Be(candidacy.Id);
        notification.OfferId.Should().Be(offer.Id);
        notification.StudentId.Should().Be(student.Id);
        notification.Kind.Should().Be(NotificationKind.ShortlistedCandidacyWithdrawn);
        notification.OccurredAtUtc.Should().Be(WithdrawTimestampUtc);
        notification.OccurredAtUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData(InternshipCandidacyStatus.Accepted)]
    [InlineData(InternshipCandidacyStatus.Rejected)]
    [InlineData(InternshipCandidacyStatus.Waitlisted)]
    [InlineData(InternshipCandidacyStatus.Withdrawn)]
    public async Task Given_FinalCandidacy_When_Withdrawing_Then_ReturnsConflictAndPreservesStatusWithoutNotification(
        InternshipCandidacyStatus finalStatus)
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(new FixedTimeProvider(WithdrawTimestampUtc));
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
        (await ReadNotificationsAsync(factory)).Should().BeEmpty();
    }

    [Fact]
    public async Task Given_MissingCandidacy_When_Withdrawing_Then_ReturnsNotFoundAndLeavesDatabaseUnchanged()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(new FixedTimeProvider(WithdrawTimestampUtc));
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
        (await db.NotificationOutboxEntries.AsNoTracking().CountAsync()).Should().Be(0);
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

    private static async Task<List<NotificationOutboxEntry>> ReadNotificationsAsync(
        InternHubSqliteFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();

        return await db.NotificationOutboxEntries
            .AsNoTracking()
            .ToListAsync();
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}
