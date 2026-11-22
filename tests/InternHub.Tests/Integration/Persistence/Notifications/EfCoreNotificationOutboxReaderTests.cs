using System.Data.Common;
using FluentAssertions;
using InternHub.Api.Domain.Notifications;
using InternHub.Api.Features.Notifications.GetRecruiterNotifications;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Api.Infrastructure.Persistence.Notifications;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.Persistence.Notifications;

public sealed class EfCoreNotificationOutboxReaderTests
{
    private static readonly DateTime BaseTimestampUtc =
        new(2027, 5, 1, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_NotificationsAcrossMultipleOffers_When_ReadingByOfferIds_Then_FiltersInSqlAndOrdersDeterministicallyWithoutTracking()
    {
        // Arrange
        using var database = new InMemoryDatabase();
        var companyOffer1 = Guid.NewGuid();
        var companyOffer2 = Guid.NewGuid();
        var otherCompanyOffer = Guid.NewGuid();
        var tiedTimestamp = BaseTimestampUtc.AddHours(1);
        var orderedTiedIds = new[] { Guid.NewGuid(), Guid.NewGuid() }
            .OrderBy(id => id.ToString(), StringComparer.Ordinal)
            .ToArray();

        var tiedSecond = CreateNotification(orderedTiedIds[1], companyOffer1, tiedTimestamp);
        var tiedFirst = CreateNotification(orderedTiedIds[0], companyOffer2, tiedTimestamp);
        var newest = CreateNotification(Guid.NewGuid(), companyOffer1, BaseTimestampUtc.AddHours(2));
        var otherOfferNotification = CreateNotification(Guid.NewGuid(), otherCompanyOffer, BaseTimestampUtc.AddHours(3));
        Seed(database, tiedSecond, otherOfferNotification, tiedFirst, newest);

        var sqlRecorder = new SqlCommandRecorder();

        // Act
        await using var context = database.CreateContext(sqlRecorder);
        var reader = new EfCoreNotificationOutboxReader(context);
        var results = await reader.GetByOfferIdsAsync([companyOffer1, companyOffer2]);

        // Assert
        results.Select(entry => entry.Id).Should().Equal(
            newest.Id,
            tiedFirst.Id,
            tiedSecond.Id);
        results.Should().OnlyContain(entry =>
            entry.OfferId == companyOffer1 || entry.OfferId == companyOffer2);
        results.Should().OnlyContain(entry => entry.OccurredAtUtc.Kind == DateTimeKind.Utc);
        context.ChangeTracker.Entries().Should().BeEmpty();
        sqlRecorder.Commands.Should().ContainSingle(sql =>
            sql.Contains("WHERE", StringComparison.OrdinalIgnoreCase) &&
            sql.Contains("\"OfferId\"", StringComparison.OrdinalIgnoreCase) &&
            sql.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Given_EmptyOfferIds_When_Reading_Then_ReturnsEmptyWithoutExecutingSql()
    {
        // Arrange
        using var database = new InMemoryDatabase();
        Seed(database, CreateNotification(Guid.NewGuid(), Guid.NewGuid(), BaseTimestampUtc));
        var sqlRecorder = new SqlCommandRecorder();

        // Act
        await using var context = database.CreateContext(sqlRecorder);
        var reader = new EfCoreNotificationOutboxReader(context);
        var results = await reader.GetByOfferIdsAsync([]);

        // Assert
        results.Should().BeEmpty();
        sqlRecorder.Commands.Should().BeEmpty();
    }

    [Fact]
    public void Given_PersistenceServices_When_ResolvingNotificationReaderAndCompanyLookup_Then_UsesScopedRegistrationsAndFailClosedLookup()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddPersistence(configuration);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });

        // Act
        using var firstScope = provider.CreateScope();
        var reader = firstScope.ServiceProvider.GetRequiredService<INotificationOutboxReader>();
        var sameScopeReader = firstScope.ServiceProvider.GetRequiredService<INotificationOutboxReader>();
        var lookup = firstScope.ServiceProvider.GetRequiredService<IRecruiterCompanyAccessLookup>();

        using var secondScope = provider.CreateScope();
        var secondReader = secondScope.ServiceProvider.GetRequiredService<INotificationOutboxReader>();

        // Assert
        reader.Should().BeOfType<EfCoreNotificationOutboxReader>();
        sameScopeReader.Should().BeSameAs(reader);
        secondReader.Should().NotBeSameAs(reader);
        lookup.Should().BeOfType<UnconfiguredRecruiterCompanyAccessLookup>();
    }

    private static NotificationOutboxEntry CreateNotification(
        Guid id,
        Guid offerId,
        DateTime occurredAtUtc) =>
        NotificationOutboxEntry.ShortlistedCandidacyWithdrawn(
            id,
            Guid.NewGuid(),
            offerId,
            Guid.NewGuid(),
            occurredAtUtc);

    private static void Seed(
        InMemoryDatabase database,
        params NotificationOutboxEntry[] entries)
    {
        using var context = database.CreateContext();
        context.NotificationOutboxEntries.AddRange(entries);
        context.SaveChanges();
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
