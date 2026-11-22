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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.Persistence.AtomicWithdrawal;

public sealed class EfCoreAtomicCandidacyWithdrawalTests
{
    private const int SqliteConstraintErrorCode = 19;
    private const int SqliteUniqueConstraintExtendedErrorCode = 2067;

    private static readonly DateTime ApplicationDate =
        new(2027, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime WithdrawTimestampUtc =
        new(2027, 2, 20, 14, 30, 45, DateTimeKind.Utc);

    [Fact]
    public async Task Given_OwnerWithShortlistedCandidacy_When_Withdrawing_Then_UpdatesStatusAndPersistsSingleOutboxNotification()
    {
        // Arrange
        using var database = new InMemoryDatabase();
        var student = CreateStudent();
        var offer = CreateOffer();
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Shortlisted);
        Seed(database, [student], [offer], [candidacy]);

        // Act
        var result = await WithdrawAsync(database, candidacy.Id, student.Id, WithdrawTimestampUtc);

        // Assert
        result.Status.Should().Be(AtomicCandidacyWithdrawalStatus.Withdrawn);
        result.Candidacy.Should().NotBeNull();
        result.Candidacy!.Status.Should().Be(InternshipCandidacyStatus.Withdrawn);

        ReadCandidacy(database, candidacy.Id)!.Status.Should().Be(InternshipCandidacyStatus.Withdrawn);
        var notifications = ReadNotifications(database);
        var notification = notifications.Should().ContainSingle().Subject;
        notification.Id.Should().NotBe(Guid.Empty);
        notification.CandidacyId.Should().Be(candidacy.Id);
        notification.OfferId.Should().Be(offer.Id);
        notification.StudentId.Should().Be(student.Id);
        notification.Kind.Should().Be(NotificationKind.ShortlistedCandidacyWithdrawn);
        notification.OccurredAtUtc.Should().Be(WithdrawTimestampUtc);
        notification.OccurredAtUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task Given_OwnerWithSubmittedCandidacy_When_Withdrawing_Then_UpdatesStatusWithoutCreatingNotification()
    {
        // Arrange
        using var database = new InMemoryDatabase();
        var student = CreateStudent();
        var offer = CreateOffer();
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Submitted);
        Seed(database, [student], [offer], [candidacy]);

        // Act
        var result = await WithdrawAsync(database, candidacy.Id, student.Id, WithdrawTimestampUtc);

        // Assert
        result.Status.Should().Be(AtomicCandidacyWithdrawalStatus.Withdrawn);
        result.Candidacy!.Status.Should().Be(InternshipCandidacyStatus.Withdrawn);
        ReadCandidacy(database, candidacy.Id)!.Status.Should().Be(InternshipCandidacyStatus.Withdrawn);
        ReadNotifications(database).Should().BeEmpty();
    }

    [Theory]
    [InlineData(InternshipCandidacyStatus.Submitted)]
    [InlineData(InternshipCandidacyStatus.Shortlisted)]
    public async Task Given_DifferentStudent_When_WithdrawingOpenCandidacy_Then_ReturnsForbiddenWithoutModifyingDatabaseOrCreatingNotification(
        InternshipCandidacyStatus initialStatus)
    {
        // Arrange
        using var database = new InMemoryDatabase();
        var owner = CreateStudent();
        var otherStudent = CreateStudent();
        var offer = CreateOffer();
        var candidacy = CreateCandidacy(owner.Id, offer.Id, initialStatus);
        Seed(database, [owner, otherStudent], [offer], [candidacy]);

        // Act
        var result = await WithdrawAsync(database, candidacy.Id, otherStudent.Id, WithdrawTimestampUtc);

        // Assert
        result.Status.Should().Be(AtomicCandidacyWithdrawalStatus.Forbidden);
        result.Candidacy.Should().BeNull();
        result.CurrentStatus.Should().BeNull();
        ReadCandidacy(database, candidacy.Id)!.Status.Should().Be(initialStatus);
        ReadNotifications(database).Should().BeEmpty();
    }

    [Theory]
    [InlineData(InternshipCandidacyStatus.Accepted)]
    [InlineData(InternshipCandidacyStatus.Rejected)]
    [InlineData(InternshipCandidacyStatus.Waitlisted)]
    [InlineData(InternshipCandidacyStatus.Withdrawn)]
    public async Task Given_DifferentStudentAndTerminalCandidacy_When_Withdrawing_Then_ReturnsForbiddenInsteadOfInvalidTransitionWithoutModifyingDatabase(
        InternshipCandidacyStatus terminalStatus)
    {
        // Arrange
        using var database = new InMemoryDatabase();
        var owner = CreateStudent();
        var otherStudent = CreateStudent();
        var offer = CreateOffer();
        var candidacy = CreateCandidacy(owner.Id, offer.Id, terminalStatus);
        Seed(database, [owner, otherStudent], [offer], [candidacy]);

        // Act
        var result = await WithdrawAsync(database, candidacy.Id, otherStudent.Id, WithdrawTimestampUtc);

        // Assert
        result.Status.Should().Be(AtomicCandidacyWithdrawalStatus.Forbidden);
        result.Candidacy.Should().BeNull();
        result.CurrentStatus.Should().BeNull();
        ReadCandidacy(database, candidacy.Id)!.Status.Should().Be(terminalStatus);
        ReadNotifications(database).Should().BeEmpty();
    }

    [Fact]
    public async Task Given_EmptyAuthenticatedStudentId_When_Withdrawing_Then_ReturnsForbiddenWithoutExecutingSqlOrModifyingDatabase()
    {
        // Arrange
        using var database = new InMemoryDatabase();
        var owner = CreateStudent();
        var offer = CreateOffer();
        var candidacy = CreateCandidacy(owner.Id, offer.Id, InternshipCandidacyStatus.Shortlisted);
        Seed(database, [owner], [offer], [candidacy]);
        var sqlRecorder = new SqlCommandRecorder();

        // Act
        await using var context = database.CreateContext(sqlRecorder);
        var withdrawals = new EfCoreAtomicCandidacyWithdrawal(
            context,
            new FixedTimeProvider(WithdrawTimestampUtc));
        var result = await withdrawals.WithdrawAsync(candidacy.Id, Guid.Empty);

        // Assert
        result.Status.Should().Be(AtomicCandidacyWithdrawalStatus.Forbidden);
        result.Candidacy.Should().BeNull();
        sqlRecorder.Commands.Should().BeEmpty();
        ReadCandidacy(database, candidacy.Id)!.Status.Should().Be(InternshipCandidacyStatus.Shortlisted);
        ReadNotifications(database).Should().BeEmpty();
    }

    [Theory]
    [InlineData(InternshipCandidacyStatus.Accepted)]
    [InlineData(InternshipCandidacyStatus.Rejected)]
    [InlineData(InternshipCandidacyStatus.Waitlisted)]
    [InlineData(InternshipCandidacyStatus.Withdrawn)]
    public async Task Given_OwnerWithFinalCandidacy_When_Withdrawing_Then_ReturnsInvalidTransitionWithoutModifyingDatabase(
        InternshipCandidacyStatus finalStatus)
    {
        // Arrange
        using var database = new InMemoryDatabase();
        var student = CreateStudent();
        var offer = CreateOffer();
        var candidacy = CreateCandidacy(student.Id, offer.Id, finalStatus);
        Seed(database, [student], [offer], [candidacy]);

        // Act
        var result = await WithdrawAsync(database, candidacy.Id, student.Id, WithdrawTimestampUtc);

        // Assert
        result.Status.Should().Be(AtomicCandidacyWithdrawalStatus.InvalidTransition);
        result.CurrentStatus.Should().Be(finalStatus);
        result.Candidacy.Should().BeNull();
        ReadCandidacy(database, candidacy.Id)!.Status.Should().Be(finalStatus);
        ReadNotifications(database).Should().BeEmpty();
    }

    [Fact]
    public async Task Given_MissingCandidacy_When_Withdrawing_Then_ReturnsCandidacyNotFoundWithoutModifyingDatabase()
    {
        // Arrange
        using var database = new InMemoryDatabase();
        var student = CreateStudent();
        var offer = CreateOffer();
        var existing = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Shortlisted);
        Seed(database, [student], [offer], [existing]);
        var missingCandidacyId = Guid.NewGuid();

        // Act
        var result = await WithdrawAsync(database, missingCandidacyId, student.Id, WithdrawTimestampUtc);

        // Assert
        result.Status.Should().Be(AtomicCandidacyWithdrawalStatus.CandidacyNotFound);
        result.Candidacy.Should().BeNull();
        ReadCandidacy(database, existing.Id)!.Status.Should().Be(InternshipCandidacyStatus.Shortlisted);
        ReadNotifications(database).Should().BeEmpty();
    }

    [Fact]
    public async Task Given_OwnerWithShortlistedCandidacy_When_WithdrawnTwiceSequentially_Then_SecondCallReturnsInvalidTransitionAndOnlyOneNotificationExists()
    {
        // Arrange
        using var database = new InMemoryDatabase();
        var student = CreateStudent();
        var offer = CreateOffer();
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Shortlisted);
        Seed(database, [student], [offer], [candidacy]);

        // Act
        var firstResult = await WithdrawAsync(database, candidacy.Id, student.Id, WithdrawTimestampUtc);
        var secondResult = await WithdrawAsync(database, candidacy.Id, student.Id, WithdrawTimestampUtc.AddMinutes(5));

        // Assert
        firstResult.Status.Should().Be(AtomicCandidacyWithdrawalStatus.Withdrawn);
        secondResult.Status.Should().Be(AtomicCandidacyWithdrawalStatus.InvalidTransition);
        secondResult.CurrentStatus.Should().Be(InternshipCandidacyStatus.Withdrawn);
        ReadCandidacy(database, candidacy.Id)!.Status.Should().Be(InternshipCandidacyStatus.Withdrawn);
        ReadNotifications(database).Should().ContainSingle(entry =>
            entry.CandidacyId == candidacy.Id &&
            entry.OccurredAtUtc == WithdrawTimestampUtc);
    }

    [Fact]
    public async Task Given_TransientSqliteBusyAfterReadAndOwnershipChangeBeforeRetry_When_Retrying_Then_ReopensTransactionRereadsCandidacyAndReturnsForbidden()
    {
        // Arrange
        using var database = new InMemoryDatabase();
        var originalOwner = CreateStudent();
        var newOwner = CreateStudent();
        var offer = CreateOffer();
        var candidacy = CreateCandidacy(originalOwner.Id, offer.Id, InternshipCandidacyStatus.Shortlisted);
        Seed(database, [originalOwner, newOwner], [offer], [candidacy]);
        var readTracker = new CandidacyReadTransactionTracker();
        var failFirstSave = new FailFirstSaveWithSqliteBusyInterceptor();
        var reassignBeforeRetry = new ReassignOwnershipBeforeRetryTransactionInterceptor(
            candidacy.Id,
            newOwner.Id);

        // Act
        await using var context = database.CreateContext(
            readTracker,
            failFirstSave,
            reassignBeforeRetry);
        var withdrawals = new EfCoreAtomicCandidacyWithdrawal(
            context,
            new FixedTimeProvider(WithdrawTimestampUtc));
        var result = await withdrawals.WithdrawAsync(candidacy.Id, originalOwner.Id);

        // Assert
        result.Status.Should().Be(AtomicCandidacyWithdrawalStatus.Forbidden);
        result.Candidacy.Should().BeNull();
        result.CurrentStatus.Should().BeNull();
        reassignBeforeRetry.TransactionStarts.Should().Be(2);
        reassignBeforeRetry.ReassignedRows.Should().Be(1);
        failFirstSave.SavingAttempts.Should().Be(1);
        readTracker.CandidacySelectTransactions.Should().HaveCount(2);
        readTracker.CandidacySelectTransactions.Should().NotContainNulls();
        readTracker.CandidacySelectTransactions.Distinct().Should().HaveCount(2);

        var persisted = ReadCandidacy(database, candidacy.Id)!;
        persisted.Status.Should().Be(InternshipCandidacyStatus.Shortlisted);
        persisted.StudentId.Should().Be(newOwner.Id);
        ReadNotifications(database).Should().BeEmpty();
    }

    [Fact]
    public async Task Given_PreExistingOutboxEntryForShortlistedCandidacy_When_NotificationInsertFailsUniqueConstraint_Then_RollsBackCandidacyStatusAndInsertsNoNewNotification()
    {
        // Arrange
        using var database = new InMemoryDatabase();
        var student = CreateStudent();
        var offer = CreateOffer();
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Shortlisted);
        var preExistingNotification = NotificationOutboxEntry.ShortlistedCandidacyWithdrawn(
            Guid.NewGuid(),
            candidacy.Id,
            offer.Id,
            student.Id,
            WithdrawTimestampUtc.AddMinutes(-10));
        Seed(database, [student], [offer], [candidacy], [preExistingNotification]);
        var sqlRecorder = new SqlCommandRecorder();

        // Act
        Func<Task> act = async () =>
        {
            await using var context = database.CreateContext(sqlRecorder);
            var withdrawals = new EfCoreAtomicCandidacyWithdrawal(
                context,
                new FixedTimeProvider(WithdrawTimestampUtc));
            await withdrawals.WithdrawAsync(candidacy.Id, student.Id);
        };

        // Assert
        var sqliteException = (await act.Should().ThrowAsync<DbUpdateException>())
            .Which.InnerException.Should()
            .BeOfType<SqliteException>()
            .Which;
        sqliteException.SqliteErrorCode.Should().Be(SqliteConstraintErrorCode);
        sqliteException.SqliteExtendedErrorCode.Should().Be(SqliteUniqueConstraintExtendedErrorCode);

        sqlRecorder.Commands.Should().Contain(sql =>
            sql.Contains("UPDATE \"InternshipCandidacies\"", StringComparison.OrdinalIgnoreCase));
        sqlRecorder.Commands.Should().Contain(sql =>
            sql.Contains("INSERT INTO \"NotificationOutboxEntries\"", StringComparison.OrdinalIgnoreCase));

        ReadCandidacy(database, candidacy.Id)!.Status.Should().Be(InternshipCandidacyStatus.Shortlisted);
        var notifications = ReadNotifications(database);
        notifications.Should().ContainSingle()
            .Which.Id.Should().Be(preExistingNotification.Id);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    public async Task Given_CancellationDuringFinalSqliteContentionAttempt_When_Withdrawing_Then_ThrowsOperationCanceledInsteadOfConcurrencyConflict(
        int sqliteContentionErrorCode)
    {
        // Arrange
        using var database = new InMemoryDatabase();
        var student = CreateStudent();
        var offer = CreateOffer();
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Shortlisted);
        Seed(database, [student], [offer], [candidacy]);
        using var cancellation = new CancellationTokenSource();
        var interceptor = new CancelOnFinalContentionInterceptor(
            cancellation,
            cancelOnAttempt: 3,
            sqliteContentionErrorCode);

        // Act
        Func<Task> act = async () =>
        {
            await using var context = database.CreateContext(interceptor);
            var withdrawals = new EfCoreAtomicCandidacyWithdrawal(
                context,
                new FixedTimeProvider(WithdrawTimestampUtc));
            await withdrawals.WithdrawAsync(candidacy.Id, student.Id, cancellation.Token);
        };

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        interceptor.SavingAttempts.Should().Be(3);
        ReadCandidacy(database, candidacy.Id)!.Status.Should().Be(InternshipCandidacyStatus.Shortlisted);
        ReadNotifications(database).Should().BeEmpty();
    }

    [Fact]
    public void Given_PersistenceServices_When_ResolvingAtomicWithdrawal_Then_UsesScopedRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddPersistence(configuration);
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(WithdrawTimestampUtc));

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });

        // Act
        using var firstScope = provider.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<IAtomicCandidacyWithdrawal>();
        var sameScopeInstance = firstScope.ServiceProvider.GetRequiredService<IAtomicCandidacyWithdrawal>();

        using var secondScope = provider.CreateScope();
        var second = secondScope.ServiceProvider.GetRequiredService<IAtomicCandidacyWithdrawal>();

        // Assert
        first.Should().BeOfType<EfCoreAtomicCandidacyWithdrawal>();
        sameScopeInstance.Should().BeSameAs(first);
        second.Should().NotBeSameAs(first);
    }

    private static async Task<AtomicCandidacyWithdrawalResult> WithdrawAsync(
        InMemoryDatabase database,
        Guid candidacyId,
        Guid authenticatedStudentId,
        DateTime utcNow)
    {
        await using var context = database.CreateContext();
        var withdrawals = new EfCoreAtomicCandidacyWithdrawal(
            context,
            new FixedTimeProvider(utcNow));
        return await withdrawals.WithdrawAsync(candidacyId, authenticatedStudentId);
    }

    private static void Seed(
        InMemoryDatabase database,
        Student[] students,
        Offer[] offers,
        InternshipCandidacy[] candidacies,
        NotificationOutboxEntry[]? notifications = null)
    {
        using var context = database.CreateContext();
        context.Students.AddRange(students);
        context.Offers.AddRange(offers);
        context.InternshipCandidacies.AddRange(candidacies);

        if (notifications is not null)
        {
            context.NotificationOutboxEntries.AddRange(notifications);
        }

        context.SaveChanges();
    }

    private static InternshipCandidacy? ReadCandidacy(InMemoryDatabase database, Guid candidacyId)
    {
        using var context = database.CreateContext();
        return context.InternshipCandidacies
            .AsNoTracking()
            .SingleOrDefault(item => item.Id == candidacyId);
    }

    private static List<NotificationOutboxEntry> ReadNotifications(InMemoryDatabase database)
    {
        using var context = database.CreateContext();
        return context.NotificationOutboxEntries
            .AsNoTracking()
            .ToList();
    }

    private static Student CreateStudent() =>
        new(Guid.NewGuid(), "Withdrawal Student", $"student-{Guid.NewGuid():N}@example.com");

    private static Offer CreateOffer() =>
        new("Withdrawal Internship", "Example Company", "C#, SQL", ApplicationDate.AddDays(10), 2);

    private static InternshipCandidacy CreateCandidacy(
        Guid studentId,
        Guid offerId,
        InternshipCandidacyStatus status) =>
        new(Guid.NewGuid(), studentId, offerId, ApplicationDate, status);

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed class SqlCommandRecorder : DbCommandInterceptor
    {
        private readonly List<string> _commands = [];

        public IReadOnlyList<string> Commands => _commands;

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            _commands.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            _commands.Add(command.CommandText);
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private sealed class CandidacyReadTransactionTracker : DbCommandInterceptor
    {
        private readonly List<DbTransaction?> _candidacySelectTransactions = [];

        public IReadOnlyList<DbTransaction?> CandidacySelectTransactions => _candidacySelectTransactions;

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FROM \"InternshipCandidacies\"", StringComparison.OrdinalIgnoreCase))
            {
                _candidacySelectTransactions.Add(command.Transaction);
            }

            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private sealed class FailFirstSaveWithSqliteBusyInterceptor : SaveChangesInterceptor
    {
        private int _savingAttempts;

        public int SavingAttempts => Volatile.Read(ref _savingAttempts);

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var attempt = Interlocked.Increment(ref _savingAttempts);

            if (attempt == 1)
            {
                throw new DbUpdateException(
                    "Simulated transient SQLite busy during first withdrawal save.",
                    new SqliteException("SQLite Error 5: 'database is locked'.", 5));
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private sealed class ReassignOwnershipBeforeRetryTransactionInterceptor(
        Guid candidacyId,
        Guid reassignedOwnerId) : DbTransactionInterceptor
    {
        private int _transactionStarts;
        private int _reassignedRows;

        public int TransactionStarts => Volatile.Read(ref _transactionStarts);
        public int ReassignedRows => Volatile.Read(ref _reassignedRows);

        public override async ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
            DbConnection connection,
            TransactionStartingEventData eventData,
            InterceptionResult<DbTransaction> result,
            CancellationToken cancellationToken = default)
        {
            var attempt = Interlocked.Increment(ref _transactionStarts);

            if (attempt == 2)
            {
                await using var updateCommand = connection.CreateCommand();
                updateCommand.CommandText =
                    "UPDATE \"InternshipCandidacies\" SET \"StudentId\" = $newStudentId WHERE \"Id\" = $candidacyId;";
                var studentParameter = updateCommand.CreateParameter();
                studentParameter.ParameterName = "$newStudentId";
                studentParameter.Value = reassignedOwnerId.ToString();
                updateCommand.Parameters.Add(studentParameter);

                var candidacyParameter = updateCommand.CreateParameter();
                candidacyParameter.ParameterName = "$candidacyId";
                candidacyParameter.Value = candidacyId.ToString();
                updateCommand.Parameters.Add(candidacyParameter);

                _reassignedRows = await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            return await base.TransactionStartingAsync(connection, eventData, result, cancellationToken);
        }
    }

    private sealed class CancelOnFinalContentionInterceptor(
        CancellationTokenSource cancellation,
        int cancelOnAttempt,
        int sqliteErrorCode) : SaveChangesInterceptor
    {
        private int _savingAttempts;

        public int SavingAttempts => Volatile.Read(ref _savingAttempts);

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var attempt = Interlocked.Increment(ref _savingAttempts);

            if (attempt == cancelOnAttempt)
            {
                cancellation.Cancel();
            }

            throw new DbUpdateException(
                "Simulated SQLite contention during withdrawal.",
                new SqliteException("Simulated SQLite contention.", sqliteErrorCode));
        }
    }

    private sealed class InMemoryDatabase : IDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        public InMemoryDatabase()
        {
            _connection.Open();
            using var context = CreateContext();
            context.Database.EnsureCreated();
        }

        public InternHubDbContext CreateContext(params IInterceptor[] interceptors) =>
            new(new DbContextOptionsBuilder<InternHubDbContext>()
                .UseSqlite(_connection)
                .AddInterceptors(interceptors)
                .Options);

        public void Dispose() => _connection.Dispose();
    }
}
