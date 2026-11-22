using System.Data.Common;
using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.AtomicDecision;
using InternHub.Api.Features.InternshipCandidacies.DecideInternshipCandidacy;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Api.Infrastructure.Persistence.AtomicDecision;
using InternHub.Domain.Students;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace InternHub.Tests.Integration.Persistence.AtomicDecision;

public sealed class EfCoreAtomicCandidacyDecisionConcurrencyTests
{
    private static readonly DateTime Now =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_OneSlotLeftAndTwoConcurrentAccepts_When_Deciding_Then_OneIsAcceptedAndOneIsWaitlisted()
    {
        // Arrange
        using var database = CreateDatabase();
        var databasePath = database.Path;
        var offer = CreateOffer(maximumAvailableSlots: 1);
        var students = Enumerable.Range(0, 2).Select(_ => CreateStudent()).ToArray();
        var candidacies = students
            .Select(student => CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Submitted))
            .ToArray();
        Seed(databasePath, students, [offer], candidacies);
        var observers = candidacies.Select(_ => new TransactionAttemptObserver()).ToArray();
        using var writeLock = AcquireWriteLock(databasePath);

        // Act
        var decisions = candidacies
            .Zip(observers, (candidacy, observer) => Task.Run(() =>
                DecideAsync(databasePath, candidacy.Id, CandidacyDecision.Accept, observer.Interceptor)))
            .ToArray();
        await AwaitTransactionAttemptsAsync(observers);
        writeLock.Dispose();
        var results = await Task.WhenAll(decisions);

        // Assert
        results.Select(result => result.Status)
            .Should().OnlyContain(status => status == AtomicCandidacyDecisionStatus.Decided);
        results.Select(result => result.Candidacy!.Status)
            .Should().BeEquivalentTo(
                [InternshipCandidacyStatus.Accepted,
                 InternshipCandidacyStatus.Waitlisted]);
        CountAccepted(databasePath, offer.Id).Should().Be(1);
    }

    [Fact]
    public async Task Given_OneSlotLeftAndFiveConcurrentAccepts_When_Deciding_Then_NeverExceedsMaximumSlots()
    {
        // Arrange
        using var database = CreateDatabase();
        var databasePath = database.Path;
        var offer = CreateOffer(maximumAvailableSlots: 2);
        var acceptedStudent = CreateStudent();
        var accepted = CreateCandidacy(acceptedStudent.Id, offer.Id, InternshipCandidacyStatus.Accepted);
        var students = Enumerable.Range(0, 5).Select(_ => CreateStudent()).ToArray();
        var candidacies = students
            .Select(student => CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Submitted))
            .ToArray();
        Seed(databasePath, [acceptedStudent, .. students], [offer], [accepted, .. candidacies]);
        var observers = candidacies.Select(_ => new TransactionAttemptObserver()).ToArray();
        using var writeLock = AcquireWriteLock(databasePath);

        // Act
        var decisions = candidacies
            .Zip(observers, (candidacy, observer) => Task.Run(() =>
                DecideAsync(databasePath, candidacy.Id, CandidacyDecision.Accept, observer.Interceptor)))
            .ToArray();
        await AwaitTransactionAttemptsAsync(observers);
        writeLock.Dispose();
        var results = await Task.WhenAll(decisions);

        // Assert
        results.Count(result => result.Candidacy?.Status == InternshipCandidacyStatus.Accepted)
            .Should().BeLessThanOrEqualTo(1);
        CountAccepted(databasePath, offer.Id).Should().Be(offer.MaximumAvailableSlots);
    }

    [Fact]
    public async Task Given_OneInjectedVersionConflict_When_Accepting_Then_RetriesAndAccepts()
    {
        // Arrange
        using var database = CreateDatabase();
        var databasePath = database.Path;
        var student = CreateStudent();
        var offer = CreateOffer(maximumAvailableSlots: 1);
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Submitted);
        Seed(databasePath, [student], [offer], [candidacy]);
        var versionBefore = ReadOfferVersion(databasePath, offer.Id);
        var conflict = new OfferVersionConflictInterceptor(offer.Id, conflictingAttempts: 1);

        // Act
        var result = await DecideAsync(databasePath, candidacy.Id, CandidacyDecision.Accept, conflict);

        // Assert
        result.Status.Should().Be(AtomicCandidacyDecisionStatus.Decided);
        result.Candidacy!.Status.Should().Be(InternshipCandidacyStatus.Accepted);
        conflict.SavingAttempts.Should().Be(2);
        ReadCandidacy(databasePath, candidacy.Id).Status.Should().Be(InternshipCandidacyStatus.Accepted);
        ReadOfferVersion(databasePath, offer.Id).Should().Be(versionBefore + 1);
    }

    [Fact]
    public async Task Given_VersionConflictOnEveryAttempt_When_Accepting_Then_ReturnsConcurrencyConflictAndChangesNothing()
    {
        // Arrange
        using var database = CreateDatabase();
        var databasePath = database.Path;
        var student = CreateStudent();
        var offer = CreateOffer(maximumAvailableSlots: 1);
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Submitted);
        Seed(databasePath, [student], [offer], [candidacy]);
        var versionBefore = ReadOfferVersion(databasePath, offer.Id);
        var conflict = new OfferVersionConflictInterceptor(offer.Id, conflictingAttempts: int.MaxValue);

        // Act
        var result = await DecideAsync(databasePath, candidacy.Id, CandidacyDecision.Accept, conflict);

        // Assert
        result.Status.Should().Be(AtomicCandidacyDecisionStatus.ConcurrencyConflict);
        result.Candidacy.Should().BeNull();
        conflict.SavingAttempts.Should().Be(3);
        ReadCandidacy(databasePath, candidacy.Id).Status.Should().Be(InternshipCandidacyStatus.Submitted);
        ReadOfferVersion(databasePath, offer.Id).Should().Be(versionBefore);
    }

    [Fact]
    public async Task Given_ThreeSlotsAndTwentyConcurrentAccepts_When_Deciding_Then_SlotInvariantsHold()
    {
        // Arrange
        using var database = CreateDatabase();
        var databasePath = database.Path;
        var offer = CreateOffer(maximumAvailableSlots: 3);
        var students = Enumerable.Range(0, 20).Select(_ => CreateStudent()).ToArray();
        var candidacies = students
            .Select(student => CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Submitted))
            .ToArray();
        Seed(databasePath, students, [offer], candidacies);
        var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var decisions = candidacies
            .Select(candidacy => Task.Run(async () =>
            {
                await startGate.Task;
                var result = await DecideAsync(databasePath, candidacy.Id, CandidacyDecision.Accept);
                return (CandidacyId: candidacy.Id, Result: result);
            }))
            .ToArray();

        // Act
        startGate.SetResult();
        var outcomes = await Task.WhenAll(decisions);

        // Assert
        outcomes.Select(outcome => outcome.Result.Status)
            .Should().OnlyContain(status =>
                status == AtomicCandidacyDecisionStatus.Decided ||
                status == AtomicCandidacyDecisionStatus.ConcurrencyConflict);
        var persisted = ReadCandidacies(databasePath, offer.Id);
        var acceptedCount = persisted.Count(candidacy => candidacy.Status == InternshipCandidacyStatus.Accepted);
        var waitlistedCount = persisted.Count(candidacy => candidacy.Status == InternshipCandidacyStatus.Waitlisted);
        acceptedCount.Should().BeLessThanOrEqualTo(offer.MaximumAvailableSlots);
        (waitlistedCount == 0 || acceptedCount == offer.MaximumAvailableSlots)
            .Should().BeTrue("a candidacy is waitlisted only when every slot is already accepted");
        persisted.Should().OnlyContain(candidacy =>
            candidacy.Status == InternshipCandidacyStatus.Accepted ||
            candidacy.Status == InternshipCandidacyStatus.Waitlisted ||
            candidacy.Status == InternshipCandidacyStatus.Submitted);
        persisted
            .Where(candidacy => candidacy.Status == InternshipCandidacyStatus.Submitted)
            .Select(candidacy => candidacy.Id)
            .Should().BeSubsetOf(outcomes
                .Where(outcome => outcome.Result.Status == AtomicCandidacyDecisionStatus.ConcurrencyConflict)
                .Select(outcome => outcome.CandidacyId));
    }

    private static async Task<AtomicCandidacyDecisionResult> DecideAsync(
        string databasePath,
        Guid candidacyId,
        CandidacyDecision decision,
        params IInterceptor[] interceptors)
    {
        await using var context = CreateContext(databasePath, interceptors);
        var decisions = new EfCoreAtomicCandidacyDecision(context);
        return await decisions.DecideAsync(candidacyId, decision);
    }

    private static async Task AwaitTransactionAttemptsAsync(
        params TransactionAttemptObserver[] observers)
    {
        await Task.WhenAll(
            observers.Select(observer =>
                observer.TransactionAttempt.Task.WaitAsync(TimeSpan.FromSeconds(5))));
    }

    private static IDisposable AcquireWriteLock(string databasePath)
    {
        var connection = new SqliteConnection(ConnectionString(databasePath));
        connection.Open();
        var transaction = connection.BeginTransaction(
            System.Data.IsolationLevel.Serializable);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE Students SET Email = Email;";
        command.ExecuteNonQuery();
        return new OwnedSqliteWriteLock(connection, transaction);
    }

    private static TemporarySqliteDatabase CreateDatabase()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"internhub-decision-{Guid.NewGuid():N}.db");
        var database = new TemporarySqliteDatabase(databasePath);
        try
        {
            using var context = CreateContext(databasePath);
            context.Database.EnsureCreated();
            return database;
        }
        catch
        {
            database.Dispose();
            throw;
        }
    }

    private static InternHubDbContext CreateContext(
        string databasePath,
        params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<InternHubDbContext>()
            .UseSqlite(ConnectionString(databasePath))
            .AddInterceptors(interceptors)
            .Options;
        return new InternHubDbContext(options);
    }

    private static string ConnectionString(string databasePath) =>
        $"Data Source={databasePath};Default Timeout=1;Pooling=False";

    private static void Seed(
        string databasePath,
        Student[] students,
        Offer[] offers,
        InternshipCandidacy[] candidacies)
    {
        using var context = CreateContext(databasePath);
        context.Students.AddRange(students);
        context.Offers.AddRange(offers);
        context.InternshipCandidacies.AddRange(candidacies);
        context.SaveChanges();
    }

    private static List<InternshipCandidacy> ReadCandidacies(string databasePath, Guid offerId)
    {
        using var context = CreateContext(databasePath);
        return context.InternshipCandidacies
            .AsNoTracking()
            .Where(candidacy => candidacy.OfferId == offerId)
            .ToList();
    }

    private static InternshipCandidacy ReadCandidacy(string databasePath, Guid candidacyId)
    {
        using var context = CreateContext(databasePath);
        return context.InternshipCandidacies
            .AsNoTracking()
            .Single(candidacy => candidacy.Id == candidacyId);
    }

    private static int CountAccepted(string databasePath, Guid offerId) =>
        ReadCandidacies(databasePath, offerId)
            .Count(candidacy => candidacy.Status == InternshipCandidacyStatus.Accepted);

    private static int ReadOfferVersion(string databasePath, Guid offerId)
    {
        using var context = CreateContext(databasePath);
        return context.Offers
            .AsNoTracking()
            .Single(offer => offer.Id == offerId)
            .Version;
    }

    private static Student CreateStudent() =>
        new(Guid.NewGuid(), "Concurrent Student", $"student-{Guid.NewGuid()}@example.com");

    private static Offer CreateOffer(int maximumAvailableSlots) =>
        new("Concurrent Internship", "Example Company", "C#, SQL", Now.AddDays(10), maximumAvailableSlots);

    private static InternshipCandidacy CreateCandidacy(
        Guid studentId,
        Guid offerId,
        InternshipCandidacyStatus status) =>
        new(Guid.NewGuid(), studentId, offerId, Now.AddHours(-1), status);

    private sealed class TransactionAttemptObserver
    {
        public TransactionAttemptObserver()
        {
            Interceptor = new TransactionAttemptInterceptor(TransactionAttempt);
        }

        public TransactionAttemptInterceptor Interceptor { get; }

        public TaskCompletionSource<bool> TransactionAttempt { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class TransactionAttemptInterceptor(
        TaskCompletionSource<bool> transactionAttempt) : DbTransactionInterceptor
    {
        private readonly TaskCompletionSource<bool> _transactionAttempt =
            transactionAttempt;

        public override InterceptionResult<DbTransaction> TransactionStarting(
            DbConnection connection,
            TransactionStartingEventData eventData,
            InterceptionResult<DbTransaction> result)
        {
            _transactionAttempt.TrySetResult(true);
            return base.TransactionStarting(connection, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
            DbConnection connection,
            TransactionStartingEventData eventData,
            InterceptionResult<DbTransaction> result,
            CancellationToken cancellationToken = default)
        {
            _transactionAttempt.TrySetResult(true);
            return base.TransactionStartingAsync(
                connection,
                eventData,
                result,
                cancellationToken);
        }
    }

    private sealed class OfferVersionConflictInterceptor(
        Guid offerId,
        int conflictingAttempts) : SaveChangesInterceptor
    {
        private int _savingAttempts;

        public int SavingAttempts => Volatile.Read(ref _savingAttempts);

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var attempt = Interlocked.Increment(ref _savingAttempts);

            if (attempt <= conflictingAttempts)
            {
                // Simulates another request committing an offer change between this
                // attempt's read and its save, inside the same transaction.
                await eventData.Context!.Set<Offer>()
                    .Where(offer => offer.Id == offerId)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(offer => offer.Version, offer => offer.Version + 1),
                        cancellationToken);
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private sealed class TemporarySqliteDatabase : IDisposable
    {
        public TemporarySqliteDatabase(string path)
        {
            Path = path;
            KeepAliveConnection = new SqliteConnection(
                ConnectionString(path));
            KeepAliveConnection.Open();
        }

        public string Path { get; }

        private SqliteConnection KeepAliveConnection { get; }

        public void Dispose()
        {
            KeepAliveConnection.Dispose();
            File.Delete(Path);
            File.Delete($"{Path}-wal");
            File.Delete($"{Path}-shm");
        }
    }

    private sealed class OwnedSqliteWriteLock(
        SqliteConnection connection,
        SqliteTransaction transaction) : IDisposable
    {
        private readonly SqliteConnection _connection = connection;
        private readonly SqliteTransaction _transaction = transaction;

        public void Dispose()
        {
            _transaction.Dispose();
            _connection.Dispose();
        }
    }
}
