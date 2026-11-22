using FluentAssertions;
using InternHub.Api.Domain.Notifications;
using InternHub.Api.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InternHub.Tests.Integration.Persistence;

public sealed class NotificationOutboxPersistenceTests
{
    private const int SqliteConstraintErrorCode = 19;
    private const int SqliteUniqueConstraintExtendedErrorCode = 2067;

    private static readonly DateTime BaseUtcTimestamp =
        new(2027, 4, 18, 9, 45, 12, DateTimeKind.Utc);

    [Fact]
    public void Given_ValidNotification_When_SavingToSqlite_Then_ExistsAndCanBeReadFromNewContext()
    {
        // Arrange
        using var connection = CreateDatabase(out var options);
        var entry = NotificationOutboxEntry.ShortlistedCandidacyWithdrawn(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            BaseUtcTimestamp);

        // Act
        using (var writeContext = new InternHubDbContext(options))
        {
            writeContext.NotificationOutboxEntries.Add(entry);
            writeContext.SaveChanges();
        }

        // Assert
        using var verificationContext = new InternHubDbContext(options);
        var persisted = verificationContext.NotificationOutboxEntries
            .AsNoTracking()
            .SingleOrDefault(item => item.Id == entry.Id);

        persisted.Should().NotBeNull();
        persisted!.Id.Should().Be(entry.Id);
    }

    [Fact]
    public void Given_ValidNotification_When_PersistedAndReloaded_Then_PreservesAllFields()
    {
        // Arrange
        using var connection = CreateDatabase(out var options);
        var id = Guid.NewGuid();
        var candidacyId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var occurredAtUtc = new DateTime(2027, 5, 22, 16, 20, 35, DateTimeKind.Utc);
        var entry = new NotificationOutboxEntry(
            id,
            candidacyId,
            offerId,
            studentId,
            NotificationKind.ShortlistedCandidacyWithdrawn,
            occurredAtUtc);

        using (var writeContext = new InternHubDbContext(options))
        {
            writeContext.NotificationOutboxEntries.Add(entry);
            writeContext.SaveChanges();
        }

        // Act
        NotificationOutboxEntry persisted;
        using (var readContext = new InternHubDbContext(options))
        {
            persisted = readContext.NotificationOutboxEntries
                .AsNoTracking()
                .Single(item => item.Id == id);
        }

        var storedKindText = ReadStoredKindText(connection, id);

        // Assert
        persisted.Id.Should().Be(id);
        persisted.CandidacyId.Should().Be(candidacyId);
        persisted.OfferId.Should().Be(offerId);
        persisted.StudentId.Should().Be(studentId);
        persisted.Kind.Should().Be(NotificationKind.ShortlistedCandidacyWithdrawn);
        persisted.OccurredAtUtc.Should().Be(occurredAtUtc);
        storedKindText.Should().Be("ShortlistedCandidacyWithdrawn");
    }

    [Fact]
    public void Given_UtcNotificationTimestamp_When_ReloadingFromSqlite_Then_PreservesUtcKindAndExactInstant()
    {
        // Arrange
        using var connection = CreateDatabase(out var options);
        var expectedOccurredAtUtc = new DateTime(2027, 6, 1, 23, 59, 58, 123, DateTimeKind.Utc)
            .AddTicks(4567);
        var entry = NotificationOutboxEntry.ShortlistedCandidacyWithdrawn(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            expectedOccurredAtUtc);

        using (var writeContext = new InternHubDbContext(options))
        {
            writeContext.NotificationOutboxEntries.Add(entry);
            writeContext.SaveChanges();
        }

        // Act
        NotificationOutboxEntry persisted;
        using (var readContext = new InternHubDbContext(options))
        {
            persisted = readContext.NotificationOutboxEntries
                .AsNoTracking()
                .Single(item => item.Id == entry.Id);
        }

        // Assert
        persisted.OccurredAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        persisted.OccurredAtUtc.Should().Be(expectedOccurredAtUtc);
        persisted.OccurredAtUtc.Ticks.Should().Be(expectedOccurredAtUtc.Ticks);
    }

    [Fact]
    public void Given_ExistingNotificationForCandidacyAndKind_When_InsertingDuplicateWithDifferentId_Then_SqliteUniqueConstraintRejectsIt()
    {
        // Arrange
        using var connection = CreateDatabase(out var options);
        var candidacyId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var first = NotificationOutboxEntry.ShortlistedCandidacyWithdrawn(
            Guid.NewGuid(),
            candidacyId,
            offerId,
            studentId,
            BaseUtcTimestamp);
        var duplicate = NotificationOutboxEntry.ShortlistedCandidacyWithdrawn(
            Guid.NewGuid(),
            candidacyId,
            offerId,
            studentId,
            BaseUtcTimestamp.AddMinutes(5));

        using (var seedContext = new InternHubDbContext(options))
        {
            seedContext.NotificationOutboxEntries.Add(first);
            seedContext.SaveChanges();
        }

        using var duplicateContext = new InternHubDbContext(options);
        duplicateContext.NotificationOutboxEntries.Add(duplicate);

        // Act
        Action act = () => duplicateContext.SaveChanges();

        // Assert
        var sqliteException = act.Should()
            .Throw<DbUpdateException>()
            .Which.InnerException.Should()
            .BeOfType<SqliteException>()
            .Which;
        sqliteException.SqliteErrorCode.Should().Be(SqliteConstraintErrorCode);
        sqliteException.SqliteExtendedErrorCode.Should().Be(SqliteUniqueConstraintExtendedErrorCode);

        using var verificationContext = new InternHubDbContext(options);
        var persistedEntries = verificationContext.NotificationOutboxEntries
            .AsNoTracking()
            .ToList();
        persistedEntries.Should().ContainSingle()
            .Which.Id.Should().Be(first.Id);
    }

    [Fact]
    public void Given_DifferentCandidaciesWithSameKind_When_SavingToSqlite_Then_AllowsBothNotifications()
    {
        // Arrange
        using var connection = CreateDatabase(out var options);
        var offerId = Guid.NewGuid();
        var first = NotificationOutboxEntry.ShortlistedCandidacyWithdrawn(
            Guid.NewGuid(),
            Guid.NewGuid(),
            offerId,
            Guid.NewGuid(),
            BaseUtcTimestamp);
        var second = NotificationOutboxEntry.ShortlistedCandidacyWithdrawn(
            Guid.NewGuid(),
            Guid.NewGuid(),
            offerId,
            Guid.NewGuid(),
            BaseUtcTimestamp.AddMinutes(1));

        // Act
        using (var writeContext = new InternHubDbContext(options))
        {
            writeContext.NotificationOutboxEntries.AddRange(first, second);
            writeContext.SaveChanges();
        }

        // Assert
        using var verificationContext = new InternHubDbContext(options);
        var persistedIds = verificationContext.NotificationOutboxEntries
            .AsNoTracking()
            .Select(item => item.Id)
            .ToList();

        persistedIds.Should().BeEquivalentTo(new[] { first.Id, second.Id });
    }

    [Fact]
    public void Given_NotificationsAcrossOffersAndEqualTimestamps_When_QueryingByOffer_Then_FiltersByOfferAndOrdersDeterministically()
    {
        // Arrange
        using var connection = CreateDatabase(out var options);
        var targetOfferId = Guid.NewGuid();
        var otherOfferId = Guid.NewGuid();
        var tiedTimestamp = new DateTime(2027, 4, 18, 10, 0, 0, DateTimeKind.Utc);
        var newerTimestamp = new DateTime(2027, 4, 18, 11, 0, 0, DateTimeKind.Utc);
        var orderedTiedIds = new[] { Guid.NewGuid(), Guid.NewGuid() }
            .OrderBy(id => id.ToString(), StringComparer.Ordinal)
            .ToArray();

        var tiedSecond = NotificationOutboxEntry.ShortlistedCandidacyWithdrawn(
            orderedTiedIds[1],
            Guid.NewGuid(),
            targetOfferId,
            Guid.NewGuid(),
            tiedTimestamp);
        var tiedFirst = NotificationOutboxEntry.ShortlistedCandidacyWithdrawn(
            orderedTiedIds[0],
            Guid.NewGuid(),
            targetOfferId,
            Guid.NewGuid(),
            tiedTimestamp);
        var newestForTargetOffer = NotificationOutboxEntry.ShortlistedCandidacyWithdrawn(
            Guid.NewGuid(),
            Guid.NewGuid(),
            targetOfferId,
            Guid.NewGuid(),
            newerTimestamp);
        var otherOfferEntry = NotificationOutboxEntry.ShortlistedCandidacyWithdrawn(
            Guid.NewGuid(),
            Guid.NewGuid(),
            otherOfferId,
            Guid.NewGuid(),
            newerTimestamp.AddHours(1));

        using (var seedContext = new InternHubDbContext(options))
        {
            seedContext.NotificationOutboxEntries.AddRange(
                tiedSecond,
                otherOfferEntry,
                tiedFirst,
                newestForTargetOffer);
            seedContext.SaveChanges();
        }

        // Act
        List<NotificationOutboxEntry> queried;
        using (var queryContext = new InternHubDbContext(options))
        {
            queried = queryContext.NotificationOutboxEntries
                .AsNoTracking()
                .Where(entry => entry.OfferId == targetOfferId)
                .OrderByDescending(entry => entry.OccurredAtUtc)
                .ThenBy(entry => entry.Id)
                .ToList();
        }

        // Assert
        queried.Select(entry => entry.Id).Should().Equal(
            newestForTargetOffer.Id,
            tiedFirst.Id,
            tiedSecond.Id);
        queried.Should().OnlyContain(entry => entry.OfferId == targetOfferId);
    }

    [Fact]
    public void Given_CreatedSqliteSchema_When_InspectingNotificationOutboxIndexes_Then_UniqueDeduplicationAndInboxIndexesExist()
    {
        // Arrange
        using var connection = CreateDatabase(out _);

        // Act
        var indexes = ReadTableIndexes(connection, "NotificationOutboxEntries");

        // Assert
        indexes.Should().ContainSingle(index =>
            index.IsUnique &&
            index.Columns.SequenceEqual(new[] { "CandidacyId", "Kind" }));
        indexes.Should().ContainSingle(index =>
            !index.IsUnique &&
            index.Columns.SequenceEqual(new[] { "OfferId", "OccurredAtUtc" }));
    }

    private static SqliteConnection CreateDatabase(
        out DbContextOptions<InternHubDbContext> options)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        options = new DbContextOptionsBuilder<InternHubDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new InternHubDbContext(options);
        context.Database.EnsureCreated();
        return connection;
    }

    private static string ReadStoredKindText(SqliteConnection connection, Guid id)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT Kind FROM NotificationOutboxEntries WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString());
        return Convert.ToString(command.ExecuteScalar()) ?? string.Empty;
    }

    private static IReadOnlyList<SqliteIndexInfo> ReadTableIndexes(
        SqliteConnection connection,
        string tableName)
    {
        var rawIndexes = new List<(string Name, bool IsUnique)>();

        using (var listCommand = connection.CreateCommand())
        {
            listCommand.CommandText = $"PRAGMA index_list('{tableName}');";
            using var reader = listCommand.ExecuteReader();
            while (reader.Read())
            {
                rawIndexes.Add((reader.GetString(1), reader.GetInt32(2) == 1));
            }
        }

        var result = new List<SqliteIndexInfo>();
        foreach (var (name, isUnique) in rawIndexes)
        {
            var columns = new List<string>();
            using var infoCommand = connection.CreateCommand();
            infoCommand.CommandText = $"PRAGMA index_info('{name}');";
            using var reader = infoCommand.ExecuteReader();
            while (reader.Read())
            {
                columns.Add(reader.GetString(2));
            }

            result.Add(new SqliteIndexInfo(name, isUnique, columns));
        }

        return result;
    }

    private sealed record SqliteIndexInfo(
        string Name,
        bool IsUnique,
        IReadOnlyList<string> Columns);
}
