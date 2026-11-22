using System.Data.Common;
using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Notifications;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.AtomicWithdrawal;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Api.Infrastructure.Persistence.AtomicWithdrawal;
using InternHub.Domain.Students;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace InternHub.Tests.Integration.Persistence.AtomicWithdrawal;

public sealed class EfCoreAtomicCandidacyWithdrawalConcurrencyTests
{
    private static readonly DateTime ApplicationDate =
        new(2027, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime WithdrawTimestampUtc =
        new(2027, 2, 20, 14, 30, 45, DateTimeKind.Utc);

    [Fact]
    public async Task Given_ShortlistedCandidacy_When_TwoConcurrentOwnerWithdrawalsExecute_Then_ExactlyOneSucceedsAndExactlyOneNotificationExists()
    {
        // Arrange
        using var database = CreateDatabase();
        var databasePath = database.Path;
        var student = CreateStudent();
        var offer = CreateOffer();
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Shortlisted);
        Seed(databasePath, [student], [offer], [candidacy]);
        var firstObserver = new TransactionAttemptObserver();
        var secondObserver = new TransactionAttemptObserver();
        using var writeLock = AcquireWriteLock(databasePath);

        // Act
        var firstTask = Task.Run(() =>
            WithdrawAsync(databasePath, candidacy.Id, student.Id, WithdrawTimestampUtc, firstObserver.Interceptor));
        var secondTask = Task.Run(() =>
            WithdrawAsync(databasePath, candidacy.Id, student.Id, WithdrawTimestampUtc, secondObserver.Interceptor));
        await AwaitTransactionAttemptsAsync(firstObserver, secondObserver);
        writeLock.Dispose();
        var results = await Task.WhenAll(firstTask, secondTask);

        // Assert
        results.Count(result => result.Status == AtomicCandidacyWithdrawalStatus.Withdrawn)
            .Should().Be(1);
        results.Count(result =>
                result.Status == AtomicCandidacyWithdrawalStatus.InvalidTransition ||
                result.Status == AtomicCandidacyWithdrawalStatus.ConcurrencyConflict)
            .Should().Be(1);

        ReadCandidacy(databasePath, candidacy.Id).Status.Should().Be(InternshipCandidacyStatus.Withdrawn);
        var notification = ReadNotifications(databasePath).Should().ContainSingle().Subject;
        notification.CandidacyId.Should().Be(candidacy.Id);
        notification.OfferId.Should().Be(offer.Id);
        notification.StudentId.Should().Be(student.Id);
        notification.Kind.Should().Be(NotificationKind.ShortlistedCandidacyWithdrawn);
        notification.OccurredAtUtc.Should().Be(WithdrawTimestampUtc);
        notification.OccurredAtUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task Given_ShortlistedCandidacy_When_OwnerAndDifferentStudentWithdrawConcurrently_Then_NonOwnerReceivesForbiddenAndOwnerSucceedsOnce()
    {
        // Arrange
        using var database = CreateDatabase();
        var databasePath = database.Path;
        var owner = CreateStudent();
        var otherStudent = CreateStudent();
        var offer = CreateOffer();
        var candidacy = CreateCandidacy(owner.Id, offer.Id, InternshipCandidacyStatus.Shortlisted);
        Seed(databasePath, [owner, otherStudent], [offer], [candidacy]);
        var ownerObserver = new TransactionAttemptObserver();
        var otherObserver = new TransactionAttemptObserver();
        using var writeLock = AcquireWriteLock(databasePath);

        // Act
        var ownerTask = Task.Run(() =>
            WithdrawAsync(databasePath, candidacy.Id, owner.Id, WithdrawTimestampUtc, ownerObserver.Interceptor));
        var otherTask = Task.Run(() =>
            WithdrawAsync(databasePath, candidacy.Id, otherStudent.Id, WithdrawTimestampUtc, otherObserver.Interceptor));
        await AwaitTransactionAttemptsAsync(ownerObserver, otherObserver);
        writeLock.Dispose();
        var ownerResult = await ownerTask;
        var otherResult = await otherTask;

        // Assert
        otherResult.Status.Should().Be(AtomicCandidacyWithdrawalStatus.Forbidden);
        otherResult.Candidacy.Should().BeNull();
        ownerResult.Status.Should().Be(AtomicCandidacyWithdrawalStatus.Withdrawn);
        ReadCandidacy(databasePath, candidacy.Id).Status.Should().Be(InternshipCandidacyStatus.Withdrawn);
        var notification = ReadNotifications(databasePath).Should().ContainSingle().Subject;
        notification.StudentId.Should().Be(owner.Id);
        notification.CandidacyId.Should().Be(candidacy.Id);
    }

    [Fact]
    public async Task Given_ShortlistedCandidacy_When_TwoConcurrentNonOwnersWithdraw_Then_BothReceiveForbiddenAndCandidacyRemainsUnchanged()
    {
        // Arrange
        using var database = CreateDatabase();
        var databasePath = database.Path;
        var owner = CreateStudent();
        var firstIntruder = CreateStudent();
        var secondIntruder = CreateStudent();
        var offer = CreateOffer();
        var candidacy = CreateCandidacy(owner.Id, offer.Id, InternshipCandidacyStatus.Shortlisted);
        Seed(databasePath, [owner, firstIntruder, secondIntruder], [offer], [candidacy]);
        var firstObserver = new TransactionAttemptObserver();
        var secondObserver = new TransactionAttemptObserver();
        using var writeLock = AcquireWriteLock(databasePath);

        // Act
        var firstTask = Task.Run(() =>
            WithdrawAsync(databasePath, candidacy.Id, firstIntruder.Id, WithdrawTimestampUtc, firstObserver.Interceptor));
        var secondTask = Task.Run(() =>
            WithdrawAsync(databasePath, candidacy.Id, secondIntruder.Id, WithdrawTimestampUtc, secondObserver.Interceptor));
        await AwaitTransactionAttemptsAsync(firstObserver, secondObserver);
        writeLock.Dispose();
        var results = await Task.WhenAll(firstTask, secondTask);

        // Assert
        results.Should().OnlyContain(result => result.Status == AtomicCandidacyWithdrawalStatus.Forbidden);
        ReadCandidacy(databasePath, candidacy.Id).Status.Should().Be(InternshipCandidacyStatus.Shortlisted);
        ReadNotifications(databasePath).Should().BeEmpty();
    }

    [Fact]
    public async Task Given_PersistentSqliteWriteLock_When_WithdrawingExhaustsRetries_Then_ReturnsConcurrencyConflictAndLeavesDatabaseUnchanged()
    {
        // Arrange
        using var database = CreateDatabase();
        var databasePath = database.Path;
        var student = CreateStudent();
        var offer = CreateOffer();
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Shortlisted);
        Seed(databasePath, [student], [offer], [candidacy]);
        var observer = new TransactionAttemptObserver();
        using var writeLock = AcquireWriteLock(databasePath);

        // Act
        var result = await WithdrawAsync(
            databasePath,
            candidacy.Id,
            student.Id,
            WithdrawTimestampUtc,
            observer.Interceptor);
        writeLock.Dispose();

        // Assert
        observer.AttemptCount.Should().Be(3);
        result.Status.Should().Be(AtomicCandidacyWithdrawalStatus.ConcurrencyConflict);
        result.Candidacy.Should().BeNull();
        ReadCandidacy(databasePath, candidacy.Id).Status.Should().Be(InternshipCandidacyStatus.Shortlisted);
        ReadNotifications(databasePath).Should().BeEmpty();
    }

    private static async Task<AtomicCandidacyWithdrawalResult> WithdrawAsync(
        string databasePath,
        Guid candidacyId,
        Guid authenticatedStudentId,
        DateTime utcNow,
        params IInterceptor[] interceptors)
    {
        await using var context = CreateContext(databasePath, interceptors);
        var withdrawals = new EfCoreAtomicCandidacyWithdrawal(
            context,
            new FixedTimeProvider(utcNow));
        return await withdrawals.WithdrawAsync(candidacyId, authenticatedStudentId);
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
            $"internhub-withdrawal-{Guid.NewGuid():N}.db");
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

    private static InternshipCandidacy ReadCandidacy(string databasePath, Guid candidacyId)
    {
        using var context = CreateContext(databasePath);
        return context.InternshipCandidacies
            .AsNoTracking()
            .Single(item => item.Id == candidacyId);
    }

    private static List<NotificationOutboxEntry> ReadNotifications(string databasePath)
    {
        using var context = CreateContext(databasePath);
        return context.NotificationOutboxEntries
            .AsNoTracking()
            .ToList();
    }

    private static Student CreateStudent() =>
        new(Guid.NewGuid(), "Concurrent Withdrawal Student", $"student-{Guid.NewGuid():N}@example.com");

    private static Offer CreateOffer() =>
        new("Concurrent Withdrawal Internship", "Example Company", "C#, SQL", ApplicationDate.AddDays(10), 2);

    private static InternshipCandidacy CreateCandidacy(
        Guid studentId,
        Guid offerId,
        InternshipCandidacyStatus status) =>
        new(Guid.NewGuid(), studentId, offerId, ApplicationDate, status);

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed class TransactionAttemptObserver
    {
        public TransactionAttemptObserver()
        {
            Interceptor = new TransactionAttemptInterceptor(TransactionAttempt);
        }

        public TransactionAttemptInterceptor Interceptor { get; }

        public TaskCompletionSource<bool> TransactionAttempt { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int AttemptCount => Interceptor.AttemptCount;
    }

    private sealed class TransactionAttemptInterceptor(
        TaskCompletionSource<bool> transactionAttempt) : DbTransactionInterceptor
    {
        private readonly TaskCompletionSource<bool> _transactionAttempt = transactionAttempt;
        private int _attemptCount;

        public int AttemptCount => Volatile.Read(ref _attemptCount);

        public override InterceptionResult<DbTransaction> TransactionStarting(
            DbConnection connection,
            TransactionStartingEventData eventData,
            InterceptionResult<DbTransaction> result)
        {
            Interlocked.Increment(ref _attemptCount);
            _transactionAttempt.TrySetResult(true);
            return base.TransactionStarting(connection, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
            DbConnection connection,
            TransactionStartingEventData eventData,
            InterceptionResult<DbTransaction> result,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _attemptCount);
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
