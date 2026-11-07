using FluentAssertions;
using System.Data.Common;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.AtomicSubmission;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Api.Infrastructure.Persistence.AtomicSubmission;
using InternHub.Domain.Students;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace InternHub.Tests.Integration.Persistence.AtomicSubmission;

public sealed class EfCoreAtomicCandidacySubmissionConcurrencyTests
{
    private static readonly DateTime Now =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_TwoConcurrentSubmissionsForSameStudentAndOffer_When_Submitting_Then_OneIsCreatedAndOneIsDuplicate()
    {
        // Arrange
        using var database = CreateDatabase();
        var databasePath = database.Path;
        var student = CreateStudent();
        var offer = CreateOffer();
        Seed(databasePath, [student], [offer], []);
        using var writeLock = AcquireWriteLock(databasePath);

        // Act
        var firstObserver = new SubmissionAttemptObserver();
        var secondObserver = new SubmissionAttemptObserver();
        var firstSubmission = SubmitAsync(
            databasePath,
            student.Id,
            offer.Id,
            firstObserver);
        var secondSubmission = SubmitAsync(
            databasePath,
            student.Id,
            offer.Id,
            secondObserver);
        await AwaitTransactionAttemptsAsync(firstObserver, secondObserver);
        writeLock.Dispose();
        var results = await Task.WhenAll(firstSubmission, secondSubmission);

        // Assert
        results.Select(result => result.Status)
            .Should().BeEquivalentTo(
                [AtomicCandidacySubmissionStatus.Created,
                 AtomicCandidacySubmissionStatus.DuplicateActive]);
        var persisted = ReadCandidacies(databasePath);
        persisted.Should().ContainSingle();
        persisted.Count(candidacy => candidacy.IsActive).Should().Be(1);
    }

    [Fact]
    public async Task Given_FourActiveCandidaciesAndTwoConcurrentDifferentOffers_When_Submitting_Then_OnlyOneReachesTheFiveActiveLimit()
    {
        // Arrange
        using var database = CreateDatabase();
        var databasePath = database.Path;
        var student = CreateStudent();
        var existingOffers = Enumerable.Range(0, 4)
            .Select(_ => CreateOffer())
            .ToArray();
        var newOffers = Enumerable.Range(0, 2)
            .Select(_ => CreateOffer())
            .ToArray();
        var existing = existingOffers
            .Select(offer => CreateCandidacy(
                student.Id,
                offer.Id,
                InternshipCandidacyStatus.Submitted))
            .ToArray();
        Seed(
            databasePath,
            [student],
            [.. existingOffers, .. newOffers],
            existing);
        using var writeLock = AcquireWriteLock(databasePath);

        // Act
        var firstObserver = new SubmissionAttemptObserver();
        var secondObserver = new SubmissionAttemptObserver();
        var firstSubmission = SubmitAsync(
            databasePath,
            student.Id,
            newOffers[0].Id,
            firstObserver);
        var secondSubmission = SubmitAsync(
            databasePath,
            student.Id,
            newOffers[1].Id,
            secondObserver);
        await AwaitTransactionAttemptsAsync(firstObserver, secondObserver);
        writeLock.Dispose();
        var results = await Task.WhenAll(firstSubmission, secondSubmission);

        // Assert
        results.Select(result => result.Status)
            .Should().BeEquivalentTo(
                [AtomicCandidacySubmissionStatus.Created,
                 AtomicCandidacySubmissionStatus.ActiveLimitReached]);
        var persisted = ReadCandidacies(databasePath);
        persisted.Should().HaveCount(5);
        persisted.Count(candidacy => candidacy.StudentId == student.Id &&
                                     candidacy.IsActive)
            .Should().Be(5);
    }

    [Fact]
    public async Task Given_ConcurrentSubmissions_When_Completed_Then_NoDuplicateActivePairsExist()
    {
        // Arrange
        using var database = CreateDatabase();
        var databasePath = database.Path;
        var student = CreateStudent();
        var offer = CreateOffer();
        Seed(databasePath, [student], [offer], []);
        using var writeLock = AcquireWriteLock(databasePath);

        // Act
        var firstObserver = new SubmissionAttemptObserver();
        var secondObserver = new SubmissionAttemptObserver();
        var firstSubmission = SubmitAsync(
            databasePath,
            student.Id,
            offer.Id,
            firstObserver);
        var secondSubmission = SubmitAsync(
            databasePath,
            student.Id,
            offer.Id,
            secondObserver);
        await AwaitTransactionAttemptsAsync(firstObserver, secondObserver);
        writeLock.Dispose();
        await Task.WhenAll(firstSubmission, secondSubmission);

        // Assert
        var persisted = ReadCandidacies(databasePath);
        persisted.Where(candidacy => candidacy.IsActive)
            .GroupBy(candidacy => new { candidacy.StudentId, candidacy.OfferId })
            .Should().OnlyContain(group => group.Count() == 1);
    }

    [Fact]
    public async Task Given_ExistingValidSubmission_When_SubmittingAnotherEligibleApplication_Then_Succeeds()
    {
        // Arrange
        using var database = CreateDatabase();
        var databasePath = database.Path;
        var student = CreateStudent();
        var firstOffer = CreateOffer();
        var secondOffer = CreateOffer();
        Seed(databasePath, [student], [firstOffer, secondOffer], []);

        // Act
        var result = await SubmitAsync(
            databasePath,
            student.Id,
            firstOffer.Id);
        var secondResult = await SubmitAsync(
            databasePath,
            student.Id,
            secondOffer.Id);

        // Assert
        result.Status.Should().Be(AtomicCandidacySubmissionStatus.Created);
        secondResult.Status.Should().Be(AtomicCandidacySubmissionStatus.Created);
        ReadCandidacies(databasePath).Should().HaveCount(2);
    }

    [Theory]
    [InlineData(InternshipCandidacyStatus.Rejected)]
    [InlineData(InternshipCandidacyStatus.Withdrawn)]
    public async Task Given_InactivePreviousCandidacy_When_Reapplying_Then_CreatesNewActiveCandidacy(
        InternshipCandidacyStatus inactiveStatus)
    {
        // Arrange
        using var database = CreateDatabase();
        var databasePath = database.Path;
        var student = CreateStudent();
        var offer = CreateOffer();
        var previous = CreateCandidacy(student.Id, offer.Id, inactiveStatus);
        Seed(databasePath, [student], [offer], [previous]);

        // Act
        var result = await SubmitAsync(databasePath, student.Id, offer.Id);

        // Assert
        result.Status.Should().Be(AtomicCandidacySubmissionStatus.Created);
        ReadCandidacies(databasePath).Should().HaveCount(2);
    }

    [Fact]
    public async Task Given_FiveActiveCandidacies_When_SubmittingToAnotherOffer_Then_FailureDoesNotModifyPersistedData()
    {
        // Arrange
        using var database = CreateDatabase();
        var databasePath = database.Path;
        var student = CreateStudent();
        var offers = Enumerable.Range(0, 6)
            .Select(_ => CreateOffer())
            .ToArray();
        var existing = offers
            .Take(5)
            .Select(offer => CreateCandidacy(
                student.Id,
                offer.Id,
                InternshipCandidacyStatus.Accepted))
            .ToArray();
        Seed(databasePath, [student], offers, existing);
        var before = ReadCandidacies(databasePath);

        // Act
        var result = await SubmitAsync(databasePath, student.Id, offers[5].Id);

        // Assert
        result.Status.Should().Be(AtomicCandidacySubmissionStatus.ActiveLimitReached);
        ReadCandidacies(databasePath)
            .Select(candidacy => candidacy.Id)
            .Should().BeEquivalentTo(before.Select(candidacy => candidacy.Id));
    }

    [Fact]
    public void Given_NewFileDatabase_When_InitializingSchema_Then_FilteredUniqueIndexExists()
    {
        // Arrange
        using var database = CreateDatabase();
        using var connection = new SqliteConnection(
            ConnectionString(database.Path));
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT sql FROM sqlite_master WHERE type = 'index' " +
            "AND name = 'IX_InternshipCandidacies_StudentId_OfferId';";

        // Act
        var definition = command.ExecuteScalar()?.ToString();

        // Assert
        definition.Should().Contain("UNIQUE INDEX");
        definition.Should().Contain("Submitted");
        definition.Should().Contain("Shortlisted");
        definition.Should().Contain("Accepted");
        definition.Should().Contain("Waitlisted");
    }

    private static async Task<AtomicCandidacySubmissionResult> SubmitAsync(
        string databasePath,
        Guid studentId,
        Guid offerId,
        SubmissionAttemptObserver? observer = null)
    {
        await using var db = CreateContext(databasePath, observer);
        var submission = new EfCoreAtomicCandidacySubmission(
            db,
            new FixedTimeProvider(Now));
        return await submission.SubmitAsync(studentId, offerId);
    }

    private static async Task AwaitTransactionAttemptsAsync(
        params SubmissionAttemptObserver[] observers)
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
            $"internhub-concurrency-{Guid.NewGuid():N}.db");
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
        SubmissionAttemptObserver? observer = null)
    {
        var optionsBuilder = new DbContextOptionsBuilder<InternHubDbContext>()
            .UseSqlite(ConnectionString(databasePath));

        if (observer is not null)
            optionsBuilder.AddInterceptors(observer.Transaction);

        var options = optionsBuilder.Options;
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

    private static List<InternshipCandidacy> ReadCandidacies(string databasePath)
    {
        using var context = CreateContext(databasePath);
        return context.InternshipCandidacies
            .AsNoTracking()
            .ToList();
    }

    private static Student CreateStudent() =>
        new(Guid.NewGuid(), "Concurrent Student", $"student-{Guid.NewGuid()}@example.com");

    private static Offer CreateOffer() =>
        new("Concurrent Internship", "Example Company", "C#, SQL", Now.AddDays(1), 2);

    private static InternshipCandidacy CreateCandidacy(
        Guid studentId,
        Guid offerId,
        InternshipCandidacyStatus status) =>
        new(Guid.NewGuid(), studentId, offerId, Now.AddHours(-1), status);

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private sealed class SubmissionAttemptObserver
    {
        public SubmissionAttemptObserver()
        {
            Transaction = new SubmissionTransactionInterceptor(TransactionAttempt);
        }

        public SubmissionTransactionInterceptor Transaction { get; }

        public TaskCompletionSource<bool> TransactionAttempt { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class SubmissionTransactionInterceptor(
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
