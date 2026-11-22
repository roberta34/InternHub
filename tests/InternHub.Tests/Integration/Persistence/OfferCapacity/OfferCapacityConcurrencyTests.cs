using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.AtomicDecision;
using InternHub.Api.Features.InternshipCandidacies.DecideInternshipCandidacy;
using InternHub.Api.Features.Offers.UpdateOffer;
using InternHub.Api.Features.Shared;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Api.Infrastructure.Persistence.AtomicDecision;
using InternHub.Domain.Students;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InternHub.Tests.Integration.Persistence.OfferCapacity;

[Collection("Integration")]
public sealed class OfferCapacityConcurrencyTests
{
    private static readonly DateTime Deadline =
        new(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_UpdateAndAcceptConcurrent_When_Completing_Then_CapacityInvariantIsPreserved()
    {
        // Arrange
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"internhub-offer-capacity-{Guid.NewGuid():N}.db");
        using var keepAlive = new SqliteConnection(ConnectionString(databasePath));
        keepAlive.Open();
        var offer = new Offer("Backend Intern", "Example Co", "C#", Deadline, 2, Guid.NewGuid());
        var student = new Student(Guid.NewGuid(), "Accepted Student", "accepted@example.com");
        var candidacy = new InternshipCandidacy(
            Guid.NewGuid(),
            student.Id,
            offer.Id,
            Deadline,
            InternshipCandidacyStatus.Submitted);

        await using (var seedContext = CreateContext(databasePath))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.Students.Add(student);
            seedContext.Offers.Add(offer);
            seedContext.InternshipCandidacies.Add(candidacy);
            await seedContext.SaveChangesAsync();
        }

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Act
        var updateTask = Task.Run(async () =>
        {
            await start.Task;
            await using var context = CreateContext(databasePath);
            var offerRepository = new EfCoreOfferRepository(context);
            var candidacyRepository = new EfCoreInternshipCandidacyRepository(context);
            var handler = new UpdateOfferHandler(offerRepository, candidacyRepository);
            return handler.Handle(new UpdateOfferCommand(
                offer.Id,
                "Updated Backend Intern",
                "Example Co",
                "C#",
                Deadline,
                1,
                offer.Version,
                offer.RecruiterId));
        });

        var acceptTask = Task.Run(async () =>
        {
            await start.Task;
            await using var context = CreateContext(databasePath);
            var decisions = new EfCoreAtomicCandidacyDecision(context);
            return await decisions.DecideAsync(candidacy.Id, CandidacyDecision.Accept);
        });

        start.SetResult();
        await Task.WhenAll(updateTask, acceptTask);
        var updateResult = await updateTask;
        var acceptResult = await acceptTask;

        // Assert
        updateResult.Status.Should().BeOneOf(ResultStatus.Success, ResultStatus.Conflict);
        acceptResult.Status.Should().BeOneOf(
            AtomicCandidacyDecisionStatus.Decided,
            AtomicCandidacyDecisionStatus.ConcurrencyConflict);

        await using var verificationContext = CreateContext(databasePath);
        var persistedOffer = await verificationContext.Offers
            .AsNoTracking()
            .SingleAsync(item => item.Id == offer.Id);
        var acceptedCount = await verificationContext.InternshipCandidacies
            .CountAsync(item =>
                item.OfferId == offer.Id &&
                item.Status == InternshipCandidacyStatus.Accepted);
        acceptedCount.Should().BeLessThanOrEqualTo(persistedOffer.MaximumAvailableSlots);
        acceptedCount.Should().BeLessThanOrEqualTo(1);

        keepAlive.Close();
        File.Delete(databasePath);
    }

    private static InternHubDbContext CreateContext(string databasePath)
    {
        var options = new DbContextOptionsBuilder<InternHubDbContext>()
            .UseSqlite(ConnectionString(databasePath))
            .Options;
        return new InternHubDbContext(options);
    }

    private static string ConnectionString(string databasePath) =>
        $"Data Source={databasePath};Default Timeout=1;Pooling=False";
}
