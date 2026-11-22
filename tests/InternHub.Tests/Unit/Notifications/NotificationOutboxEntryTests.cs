using FluentAssertions;
using InternHub.Api.Domain.Notifications;

namespace InternHub.Tests.Unit.Notifications;

public sealed class NotificationOutboxEntryTests
{
    private static readonly DateTime FixedUtcTimestamp =
        new(2027, 3, 10, 14, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Given_ValidNotificationData_When_CreatingNotificationOutboxEntry_Then_AllFieldsArePreserved()
    {
        // Arrange
        var id = Guid.NewGuid();
        var candidacyId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var kind = NotificationKind.ShortlistedCandidacyWithdrawn;
        var occurredAtUtc = FixedUtcTimestamp;

        // Act
        var entry = new NotificationOutboxEntry(
            id,
            candidacyId,
            offerId,
            studentId,
            kind,
            occurredAtUtc);

        // Assert
        entry.Id.Should().Be(id);
        entry.CandidacyId.Should().Be(candidacyId);
        entry.OfferId.Should().Be(offerId);
        entry.StudentId.Should().Be(studentId);
        entry.Kind.Should().Be(NotificationKind.ShortlistedCandidacyWithdrawn);
        entry.OccurredAtUtc.Should().Be(occurredAtUtc);
        entry.OccurredAtUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Given_ValidNotificationData_When_CreatingShortlistedCandidacyWithdrawnNotification_Then_SetsExplicitKindAndPreservesFields()
    {
        // Arrange
        var id = Guid.NewGuid();
        var candidacyId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var occurredAtUtc = FixedUtcTimestamp;

        // Act
        var entry = NotificationOutboxEntry.ShortlistedCandidacyWithdrawn(
            id,
            candidacyId,
            offerId,
            studentId,
            occurredAtUtc);

        // Assert
        entry.Id.Should().Be(id);
        entry.CandidacyId.Should().Be(candidacyId);
        entry.OfferId.Should().Be(offerId);
        entry.StudentId.Should().Be(studentId);
        entry.Kind.Should().Be(NotificationKind.ShortlistedCandidacyWithdrawn);
        entry.OccurredAtUtc.Should().Be(occurredAtUtc);
        entry.OccurredAtUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Given_EmptyId_When_CreatingNotificationOutboxEntry_Then_ThrowsArgumentException()
    {
        // Arrange
        var id = Guid.Empty;
        var candidacyId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        // Act
        Action act = () => new NotificationOutboxEntry(
            id,
            candidacyId,
            offerId,
            studentId,
            NotificationKind.ShortlistedCandidacyWithdrawn,
            FixedUtcTimestamp);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .Which.ParamName.Should().Be("id");
    }

    [Fact]
    public void Given_EmptyCandidacyId_When_CreatingNotificationOutboxEntry_Then_ThrowsArgumentException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var candidacyId = Guid.Empty;
        var offerId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        // Act
        Action act = () => new NotificationOutboxEntry(
            id,
            candidacyId,
            offerId,
            studentId,
            NotificationKind.ShortlistedCandidacyWithdrawn,
            FixedUtcTimestamp);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .Which.ParamName.Should().Be("candidacyId");
    }

    [Fact]
    public void Given_EmptyOfferId_When_CreatingNotificationOutboxEntry_Then_ThrowsArgumentException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var candidacyId = Guid.NewGuid();
        var offerId = Guid.Empty;
        var studentId = Guid.NewGuid();

        // Act
        Action act = () => new NotificationOutboxEntry(
            id,
            candidacyId,
            offerId,
            studentId,
            NotificationKind.ShortlistedCandidacyWithdrawn,
            FixedUtcTimestamp);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .Which.ParamName.Should().Be("offerId");
    }

    [Fact]
    public void Given_EmptyStudentId_When_CreatingNotificationOutboxEntry_Then_ThrowsArgumentException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var candidacyId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var studentId = Guid.Empty;

        // Act
        Action act = () => new NotificationOutboxEntry(
            id,
            candidacyId,
            offerId,
            studentId,
            NotificationKind.ShortlistedCandidacyWithdrawn,
            FixedUtcTimestamp);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .Which.ParamName.Should().Be("studentId");
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Given_NonUtcTimestamp_When_CreatingNotificationOutboxEntry_Then_ThrowsArgumentException(
        DateTimeKind timestampKind)
    {
        // Arrange
        var id = Guid.NewGuid();
        var candidacyId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var occurredAt = new DateTime(2027, 3, 10, 14, 30, 0, timestampKind);

        // Act
        Action act = () => new NotificationOutboxEntry(
            id,
            candidacyId,
            offerId,
            studentId,
            NotificationKind.ShortlistedCandidacyWithdrawn,
            occurredAt);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .Which.ParamName.Should().Be("occurredAtUtc");
    }

    [Fact]
    public void Given_DefaultTimestamp_When_CreatingNotificationOutboxEntry_Then_ThrowsArgumentException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var candidacyId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var occurredAt = default(DateTime);

        // Act
        Action act = () => new NotificationOutboxEntry(
            id,
            candidacyId,
            offerId,
            studentId,
            NotificationKind.ShortlistedCandidacyWithdrawn,
            occurredAt);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .Which.ParamName.Should().Be("occurredAtUtc");
    }

    [Fact]
    public void Given_MinValueUtcTimestamp_When_CreatingNotificationOutboxEntry_Then_ThrowsArgumentException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var candidacyId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

        // Act
        Action act = () => new NotificationOutboxEntry(
            id,
            candidacyId,
            offerId,
            studentId,
            NotificationKind.ShortlistedCandidacyWithdrawn,
            occurredAtUtc);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .Which.ParamName.Should().Be("occurredAtUtc");
    }

    [Theory]
    [InlineData((NotificationKind)0)]
    [InlineData((NotificationKind)(-1))]
    [InlineData((NotificationKind)999)]
    public void Given_UndefinedNotificationKind_When_CreatingNotificationOutboxEntry_Then_ThrowsArgumentOutOfRangeException(
        NotificationKind kind)
    {
        // Arrange
        var id = Guid.NewGuid();
        var candidacyId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        // Act
        Action act = () => new NotificationOutboxEntry(
            id,
            candidacyId,
            offerId,
            studentId,
            kind,
            FixedUtcTimestamp);

        // Assert
        var exception = act.Should().Throw<ArgumentOutOfRangeException>().Which;
        exception.ParamName.Should().Be("kind");
        exception.ActualValue.Should().Be(kind);
    }

    [Fact]
    public void Given_NotificationKindEnum_When_InspectingDefinedValues_Then_ShortlistedCandidacyWithdrawnIsExplicitAndStable()
    {
        // Arrange
        var expectedKind = NotificationKind.ShortlistedCandidacyWithdrawn;

        // Act
        var isExpectedDefined = Enum.IsDefined(expectedKind);
        var kindName = expectedKind.ToString();
        var numericValue = (int)expectedKind;
        var isDefaultDefined = Enum.IsDefined(default(NotificationKind));

        // Assert
        isExpectedDefined.Should().BeTrue();
        kindName.Should().Be("ShortlistedCandidacyWithdrawn");
        numericValue.Should().Be(1);
        isDefaultDefined.Should().BeFalse();
    }
}
