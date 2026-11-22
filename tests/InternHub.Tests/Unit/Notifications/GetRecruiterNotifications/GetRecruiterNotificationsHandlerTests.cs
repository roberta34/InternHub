using System.Security.Claims;
using FluentAssertions;
using InternHub.Api.Domain.Notifications;
using InternHub.Api.Features.Notifications.GetRecruiterNotifications;
using InternHub.Api.Features.Shared;
using InternHub.Tests.Unit.Notifications.Fakes;

namespace InternHub.Tests.Unit.Notifications.GetRecruiterNotifications;

public sealed class GetRecruiterNotificationsHandlerTests
{
    private const string RecruiterIdClaimType = "recruiter_id";
    private const string SubjectClaimType = "sub";

    private static readonly DateTime BaseTimestampUtc =
        new(2027, 4, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_ValidRecruiterWithDistinctAccountSubAndRecruiterId_When_Handling_Then_ReturnsCompanyNotifications()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var firstRecruiterId = Guid.NewGuid();
        var secondRecruiterId = Guid.NewGuid();
        var firstOfferId = Guid.NewGuid();
        var secondOfferId = Guid.NewGuid();
        var otherCompanyOfferId = Guid.NewGuid();

        var firstNotification = CreateNotification(firstOfferId, BaseTimestampUtc);
        var secondNotification = CreateNotification(secondOfferId, BaseTimestampUtc.AddMinutes(10));
        var otherCompanyNotification = CreateNotification(otherCompanyOfferId, BaseTimestampUtc.AddMinutes(20));

        var companyLookup = new FakeRecruiterCompanyAccessLookup();
        companyLookup.AssignRecruiterToCompany(firstRecruiterId, companyId);
        companyLookup.AssignRecruiterToCompany(secondRecruiterId, companyId);
        companyLookup.AssignOffersToCompany(companyId, firstOfferId, secondOfferId);

        var reader = new FakeNotificationOutboxReader();
        reader.Seed(firstNotification, otherCompanyNotification, secondNotification);

        var handler = new GetRecruiterNotificationsHandler(companyLookup, reader);
        var principal = CreateRecruiterPrincipal(
            secondRecruiterId.ToString(),
            sub: accountId.ToString());

        // Act
        var result = await handler.HandleAsync(new GetRecruiterNotificationsQuery(principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().Equal(
            ToResponse(secondNotification),
            ToResponse(firstNotification));
        companyLookup.LastQueriedRecruiterId.Should().Be(secondRecruiterId);
        reader.LastRequestedOfferIds.Should().BeEquivalentTo([firstOfferId, secondOfferId]);
    }

    [Fact]
    public async Task Given_ValidRecruiterWithMatchingSubAndNameIdentifierAndDistinctRecruiterId_When_Handling_Then_ReturnsCompanyNotifications()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        const string accountId = "account-42";
        var recruiterId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var notification = CreateNotification(offerId, BaseTimestampUtc);

        var companyLookup = new FakeRecruiterCompanyAccessLookup();
        companyLookup.AssignRecruiterToCompany(recruiterId, companyId);
        companyLookup.AssignOffersToCompany(companyId, offerId);

        var reader = new FakeNotificationOutboxReader();
        reader.Seed(notification);

        var handler = new GetRecruiterNotificationsHandler(companyLookup, reader);
        var principal = CreateRecruiterPrincipal(
            recruiterId.ToString(),
            sub: accountId,
            nameIdentifier: accountId);

        // Act
        var result = await handler.HandleAsync(new GetRecruiterNotificationsQuery(principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().Equal(ToResponse(notification));
        companyLookup.LastQueriedRecruiterId.Should().Be(recruiterId);
    }

    [Fact]
    public async Task Given_ValidRecruiterWithoutNotifications_When_Handling_Then_ReturnsEmptyList()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var recruiterId = Guid.NewGuid();
        var companyOfferId = Guid.NewGuid();
        var otherOfferId = Guid.NewGuid();

        var companyLookup = new FakeRecruiterCompanyAccessLookup();
        companyLookup.AssignRecruiterToCompany(recruiterId, companyId);
        companyLookup.AssignOffersToCompany(companyId, companyOfferId);

        var reader = new FakeNotificationOutboxReader();
        reader.Seed(CreateNotification(otherOfferId, BaseTimestampUtc));

        var handler = new GetRecruiterNotificationsHandler(companyLookup, reader);
        var principal = CreateRecruiterPrincipal(recruiterId.ToString());

        // Act
        var result = await handler.HandleAsync(new GetRecruiterNotificationsQuery(principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_ValidRecruiterOfCompanyWithoutOffers_When_Handling_Then_ReturnsEmptyListWithoutQueryingNotifications()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var recruiterId = Guid.NewGuid();

        var companyLookup = new FakeRecruiterCompanyAccessLookup();
        companyLookup.AssignRecruiterToCompany(recruiterId, companyId);

        var reader = new FakeNotificationOutboxReader();
        reader.Seed(CreateNotification(Guid.NewGuid(), BaseTimestampUtc));

        var handler = new GetRecruiterNotificationsHandler(companyLookup, reader);
        var principal = CreateRecruiterPrincipal(recruiterId.ToString());

        // Act
        var result = await handler.HandleAsync(new GetRecruiterNotificationsQuery(principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeEmpty();
        reader.ReadCalls.Should().Be(0);
    }

    [Fact]
    public async Task Given_RecruiterFromOtherCompany_When_Handling_Then_DoesNotReceiveFirstCompanyNotifications()
    {
        // Arrange
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var recruiterA = Guid.NewGuid();
        var recruiterB = Guid.NewGuid();
        var offerA = Guid.NewGuid();
        var offerB = Guid.NewGuid();

        var notificationA = CreateNotification(offerA, BaseTimestampUtc.AddMinutes(5));
        var notificationB = CreateNotification(offerB, BaseTimestampUtc);

        var companyLookup = new FakeRecruiterCompanyAccessLookup();
        companyLookup.AssignRecruiterToCompany(recruiterA, companyA);
        companyLookup.AssignRecruiterToCompany(recruiterB, companyB);
        companyLookup.AssignOffersToCompany(companyA, offerA);
        companyLookup.AssignOffersToCompany(companyB, offerB);

        var reader = new FakeNotificationOutboxReader();
        reader.Seed(notificationA, notificationB);

        var handler = new GetRecruiterNotificationsHandler(companyLookup, reader);
        var principalB = CreateRecruiterPrincipal(recruiterB.ToString());

        // Act
        var result = await handler.HandleAsync(new GetRecruiterNotificationsQuery(principalB));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().ContainSingle()
            .Which.Should().Be(ToResponse(notificationB));
        result.Value.Should().NotContain(item => item.OfferId == offerA);
    }

    [Fact]
    public async Task Given_UnauthenticatedUser_When_Handling_Then_ReturnsUnauthorizedWithoutQueryingDependencies()
    {
        // Arrange
        var companyLookup = new FakeRecruiterCompanyAccessLookup();
        var reader = new FakeNotificationOutboxReader();
        var handler = new GetRecruiterNotificationsHandler(companyLookup, reader);
        var unauthenticatedPrincipal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = await handler.HandleAsync(new GetRecruiterNotificationsQuery(unauthenticatedPrincipal));

        // Assert
        result.Status.Should().Be(ResultStatus.Unauthorized);
        result.Value.Should().BeNull();
        companyLookup.RecruiterCompanyLookupCalls.Should().Be(0);
        reader.ReadCalls.Should().Be(0);
    }

    [Fact]
    public async Task Given_NullPrincipal_When_Handling_Then_ReturnsUnauthorizedWithoutQueryingDependencies()
    {
        // Arrange
        var companyLookup = new FakeRecruiterCompanyAccessLookup();
        var reader = new FakeNotificationOutboxReader();
        var handler = new GetRecruiterNotificationsHandler(companyLookup, reader);

        // Act
        var result = await handler.HandleAsync(new GetRecruiterNotificationsQuery(null));

        // Assert
        result.Status.Should().Be(ResultStatus.Unauthorized);
        result.Value.Should().BeNull();
        companyLookup.RecruiterCompanyLookupCalls.Should().Be(0);
        reader.ReadCalls.Should().Be(0);
    }

    [Fact]
    public async Task Given_AuthenticatedStudent_When_Handling_Then_ReturnsForbiddenWithoutQueryingDependencies()
    {
        // Arrange
        var companyLookup = new FakeRecruiterCompanyAccessLookup();
        var reader = new FakeNotificationOutboxReader();
        var handler = new GetRecruiterNotificationsHandler(companyLookup, reader);
        var studentPrincipal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Role, "Student"),
                    new Claim(SubjectClaimType, Guid.NewGuid().ToString())
                ],
                authenticationType: "TestScheme"));

        // Act
        var result = await handler.HandleAsync(new GetRecruiterNotificationsQuery(studentPrincipal));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Value.Should().BeNull();
        companyLookup.RecruiterCompanyLookupCalls.Should().Be(0);
        reader.ReadCalls.Should().Be(0);
    }

    [Fact]
    public async Task Given_AuthenticatedRecruiterWithAccountSubOnlyAndMissingRecruiterId_When_Handling_Then_ReturnsForbiddenWithoutQueryingDependencies()
    {
        // Arrange
        var companyLookup = new FakeRecruiterCompanyAccessLookup();
        var reader = new FakeNotificationOutboxReader();
        reader.Seed(CreateNotification(Guid.NewGuid(), BaseTimestampUtc));
        var handler = new GetRecruiterNotificationsHandler(companyLookup, reader);
        var principal = CreateRecruiterPrincipal(
            recruiterId: null,
            sub: Guid.NewGuid().ToString());

        // Act
        var result = await handler.HandleAsync(new GetRecruiterNotificationsQuery(principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Value.Should().BeNull();
        companyLookup.RecruiterCompanyLookupCalls.Should().Be(0);
        reader.ReadCalls.Should().Be(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Given_AuthenticatedRecruiterWithInvalidRecruiterIdClaim_When_Handling_Then_ReturnsForbiddenWithoutQueryingDependencies(
        string? invalidRecruiterId)
    {
        // Arrange
        var companyLookup = new FakeRecruiterCompanyAccessLookup();
        var reader = new FakeNotificationOutboxReader();
        reader.Seed(CreateNotification(Guid.NewGuid(), BaseTimestampUtc));
        var handler = new GetRecruiterNotificationsHandler(companyLookup, reader);
        var principal = CreateRecruiterPrincipal(invalidRecruiterId);

        // Act
        var result = await handler.HandleAsync(new GetRecruiterNotificationsQuery(principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Value.Should().BeNull();
        companyLookup.RecruiterCompanyLookupCalls.Should().Be(0);
        reader.ReadCalls.Should().Be(0);
    }

    [Fact]
    public async Task Given_RecruiterRoleInOneIdentityAndRecruiterIdInAnotherIdentity_When_Handling_Then_ReturnsForbiddenWithoutQueryingDependencies()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var recruiterId = Guid.NewGuid();
        var offerId = Guid.NewGuid();

        var companyLookup = new FakeRecruiterCompanyAccessLookup();
        companyLookup.AssignRecruiterToCompany(recruiterId, companyId);
        companyLookup.AssignOffersToCompany(companyId, offerId);

        var reader = new FakeNotificationOutboxReader();
        reader.Seed(CreateNotification(offerId, BaseTimestampUtc));

        var roleOnlyIdentity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Recruiter")],
            authenticationType: "SchemeA");
        var idOnlyIdentity = new ClaimsIdentity(
            [new Claim(RecruiterIdClaimType, recruiterId.ToString())],
            authenticationType: "SchemeB");
        var principal = new ClaimsPrincipal([roleOnlyIdentity, idOnlyIdentity]);
        var handler = new GetRecruiterNotificationsHandler(companyLookup, reader);

        // Act
        var result = await handler.HandleAsync(new GetRecruiterNotificationsQuery(principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Value.Should().BeNull();
        companyLookup.RecruiterCompanyLookupCalls.Should().Be(0);
        reader.ReadCalls.Should().Be(0);
    }

    [Fact]
    public async Task Given_TwoEligibleAuthenticatedRecruiterIdentities_When_Handling_Then_ReturnsForbiddenWithoutQueryingDependencies()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var firstRecruiterId = Guid.NewGuid();
        var secondRecruiterId = Guid.NewGuid();
        var offerId = Guid.NewGuid();

        var companyLookup = new FakeRecruiterCompanyAccessLookup();
        companyLookup.AssignRecruiterToCompany(firstRecruiterId, companyId);
        companyLookup.AssignRecruiterToCompany(secondRecruiterId, companyId);
        companyLookup.AssignOffersToCompany(companyId, offerId);

        var reader = new FakeNotificationOutboxReader();
        reader.Seed(CreateNotification(offerId, BaseTimestampUtc));

        var firstIdentity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "Recruiter"),
                new Claim(RecruiterIdClaimType, firstRecruiterId.ToString())
            ],
            authenticationType: "SchemeA");
        var secondIdentity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "Recruiter"),
                new Claim(RecruiterIdClaimType, secondRecruiterId.ToString())
            ],
            authenticationType: "SchemeB");
        var principal = new ClaimsPrincipal([firstIdentity, secondIdentity]);
        var handler = new GetRecruiterNotificationsHandler(companyLookup, reader);

        // Act
        var result = await handler.HandleAsync(new GetRecruiterNotificationsQuery(principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Value.Should().BeNull();
        companyLookup.RecruiterCompanyLookupCalls.Should().Be(0);
        reader.ReadCalls.Should().Be(0);
    }

    [Theory]
    [InlineData(SubjectClaimType)]
    [InlineData(ClaimTypes.NameIdentifier)]
    [InlineData(RecruiterIdClaimType)]
    public async Task Given_AdditionalAuthenticatedIdentityWithIdentifier_When_Handling_Then_ReturnsForbiddenWithoutQueryingDependencies(
        string claimType)
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var recruiterId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var companyLookup = new FakeRecruiterCompanyAccessLookup();
        companyLookup.AssignRecruiterToCompany(recruiterId, companyId);
        companyLookup.AssignOffersToCompany(companyId, offerId);
        var reader = new FakeNotificationOutboxReader();
        reader.Seed(CreateNotification(offerId, BaseTimestampUtc));

        var recruiter = CreateRecruiterPrincipal(recruiterId.ToString());
        recruiter.AddIdentity(new ClaimsIdentity(
            [new Claim(claimType, Guid.NewGuid().ToString())],
            authenticationType: "OtherScheme"));
        var handler = new GetRecruiterNotificationsHandler(companyLookup, reader);

        // Act
        var result = await handler.HandleAsync(new GetRecruiterNotificationsQuery(recruiter));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Value.Should().BeNull();
        companyLookup.RecruiterCompanyLookupCalls.Should().Be(0);
        companyLookup.CompanyOfferLookupCalls.Should().Be(0);
        reader.ReadCalls.Should().Be(0);
    }

    [Fact]
    public async Task Given_EmptyRecruiterIdClaimAlongsideValidRecruiterId_When_Handling_Then_ReturnsForbiddenWithoutQueryingDependencies()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var recruiterId = Guid.NewGuid();
        var offerId = Guid.NewGuid();

        var companyLookup = new FakeRecruiterCompanyAccessLookup();
        companyLookup.AssignRecruiterToCompany(recruiterId, companyId);
        companyLookup.AssignOffersToCompany(companyId, offerId);

        var reader = new FakeNotificationOutboxReader();
        reader.Seed(CreateNotification(offerId, BaseTimestampUtc));

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Role, "Recruiter"),
                    new Claim(RecruiterIdClaimType, string.Empty),
                    new Claim(RecruiterIdClaimType, recruiterId.ToString())
                ],
                authenticationType: "TestScheme"));
        var handler = new GetRecruiterNotificationsHandler(companyLookup, reader);

        // Act
        var result = await handler.HandleAsync(new GetRecruiterNotificationsQuery(principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Value.Should().BeNull();
        companyLookup.RecruiterCompanyLookupCalls.Should().Be(0);
        reader.ReadCalls.Should().Be(0);
    }

    [Fact]
    public async Task Given_DuplicateRecruiterIdClaimsWithSameGuid_When_Handling_Then_ReturnsForbiddenWithoutQueryingDependencies()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var recruiterId = Guid.NewGuid();
        var offerId = Guid.NewGuid();

        var companyLookup = new FakeRecruiterCompanyAccessLookup();
        companyLookup.AssignRecruiterToCompany(recruiterId, companyId);
        companyLookup.AssignOffersToCompany(companyId, offerId);

        var reader = new FakeNotificationOutboxReader();
        reader.Seed(CreateNotification(offerId, BaseTimestampUtc));

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Role, "Recruiter"),
                    new Claim(RecruiterIdClaimType, recruiterId.ToString()),
                    new Claim(RecruiterIdClaimType, recruiterId.ToString())
                ],
                authenticationType: "TestScheme"));
        var handler = new GetRecruiterNotificationsHandler(companyLookup, reader);

        // Act
        var result = await handler.HandleAsync(new GetRecruiterNotificationsQuery(principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Value.Should().BeNull();
        companyLookup.RecruiterCompanyLookupCalls.Should().Be(0);
        reader.ReadCalls.Should().Be(0);
    }

    [Fact]
    public async Task Given_ContradictoryRecruiterIdClaims_When_Handling_Then_ReturnsForbiddenWithoutQueryingDependencies()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var firstRecruiterId = Guid.NewGuid();
        var secondRecruiterId = Guid.NewGuid();
        var offerId = Guid.NewGuid();

        var companyLookup = new FakeRecruiterCompanyAccessLookup();
        companyLookup.AssignRecruiterToCompany(firstRecruiterId, companyId);
        companyLookup.AssignOffersToCompany(companyId, offerId);

        var reader = new FakeNotificationOutboxReader();
        reader.Seed(CreateNotification(offerId, BaseTimestampUtc));

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Role, "Recruiter"),
                    new Claim(RecruiterIdClaimType, firstRecruiterId.ToString()),
                    new Claim(RecruiterIdClaimType, secondRecruiterId.ToString())
                ],
                authenticationType: "TestScheme"));
        var handler = new GetRecruiterNotificationsHandler(companyLookup, reader);

        // Act
        var result = await handler.HandleAsync(new GetRecruiterNotificationsQuery(principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Value.Should().BeNull();
        companyLookup.RecruiterCompanyLookupCalls.Should().Be(0);
        reader.ReadCalls.Should().Be(0);
    }

    [Fact]
    public async Task Given_ContradictorySubAndNameIdentifierClaims_When_Handling_Then_ReturnsForbiddenWithoutQueryingDependencies()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var recruiterId = Guid.NewGuid();
        var offerId = Guid.NewGuid();

        var companyLookup = new FakeRecruiterCompanyAccessLookup();
        companyLookup.AssignRecruiterToCompany(recruiterId, companyId);
        companyLookup.AssignOffersToCompany(companyId, offerId);

        var reader = new FakeNotificationOutboxReader();
        reader.Seed(CreateNotification(offerId, BaseTimestampUtc));

        var principal = CreateRecruiterPrincipal(
            recruiterId.ToString(),
            sub: Guid.NewGuid().ToString(),
            nameIdentifier: Guid.NewGuid().ToString());
        var handler = new GetRecruiterNotificationsHandler(companyLookup, reader);

        // Act
        var result = await handler.HandleAsync(new GetRecruiterNotificationsQuery(principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Value.Should().BeNull();
        companyLookup.RecruiterCompanyLookupCalls.Should().Be(0);
        reader.ReadCalls.Should().Be(0);
    }

    [Fact]
    public async Task Given_DuplicateSubClaimsAlongsideValidRecruiterId_When_Handling_Then_ReturnsForbiddenWithoutQueryingDependencies()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var recruiterId = Guid.NewGuid();
        var offerId = Guid.NewGuid();

        var companyLookup = new FakeRecruiterCompanyAccessLookup();
        companyLookup.AssignRecruiterToCompany(recruiterId, companyId);
        companyLookup.AssignOffersToCompany(companyId, offerId);

        var reader = new FakeNotificationOutboxReader();
        reader.Seed(CreateNotification(offerId, BaseTimestampUtc));

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Role, "Recruiter"),
                    new Claim(RecruiterIdClaimType, recruiterId.ToString()),
                    new Claim(SubjectClaimType, accountId.ToString()),
                    new Claim(SubjectClaimType, accountId.ToString())
                ],
                authenticationType: "TestScheme"));
        var handler = new GetRecruiterNotificationsHandler(companyLookup, reader);

        // Act
        var result = await handler.HandleAsync(new GetRecruiterNotificationsQuery(principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Value.Should().BeNull();
        companyLookup.RecruiterCompanyLookupCalls.Should().Be(0);
        reader.ReadCalls.Should().Be(0);
    }

    [Fact]
    public async Task Given_AuthenticatedRecruiterWithoutCompanyAssociation_When_Handling_Then_ReturnsForbidden()
    {
        // Arrange
        var unlinkedRecruiterId = Guid.NewGuid();
        var companyLookup = new FakeRecruiterCompanyAccessLookup();
        var reader = new FakeNotificationOutboxReader();
        reader.Seed(CreateNotification(Guid.NewGuid(), BaseTimestampUtc));
        var handler = new GetRecruiterNotificationsHandler(companyLookup, reader);
        var principal = CreateRecruiterPrincipal(unlinkedRecruiterId.ToString());

        // Act
        var result = await handler.HandleAsync(new GetRecruiterNotificationsQuery(principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Value.Should().BeNull();
        companyLookup.RecruiterCompanyLookupCalls.Should().Be(1);
        companyLookup.LastQueriedRecruiterId.Should().Be(unlinkedRecruiterId);
        companyLookup.CompanyOfferLookupCalls.Should().Be(0);
        reader.ReadCalls.Should().Be(0);
    }

    [Fact]
    public async Task Given_UnorderedNotificationsWithEqualTimestamps_When_Handling_Then_OrdersByOccurredAtUtcDescendingThenIdAscending()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var recruiterId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var tiedTimestamp = BaseTimestampUtc.AddHours(1);
        var orderedTiedIds = new[] { Guid.NewGuid(), Guid.NewGuid() }
            .OrderBy(id => id)
            .ToArray();

        var oldest = CreateNotification(offerId, BaseTimestampUtc);
        var tiedSecond = CreateNotificationWithId(orderedTiedIds[1], offerId, tiedTimestamp);
        var tiedFirst = CreateNotificationWithId(orderedTiedIds[0], offerId, tiedTimestamp);
        var newest = CreateNotification(offerId, BaseTimestampUtc.AddHours(2));

        var companyLookup = new FakeRecruiterCompanyAccessLookup();
        companyLookup.AssignRecruiterToCompany(recruiterId, companyId);
        companyLookup.AssignOffersToCompany(companyId, offerId);

        var reader = new FakeNotificationOutboxReader();
        reader.Seed(oldest, tiedSecond, newest, tiedFirst);

        var handler = new GetRecruiterNotificationsHandler(companyLookup, reader);
        var principal = CreateRecruiterPrincipal(recruiterId.ToString());

        // Act
        var result = await handler.HandleAsync(new GetRecruiterNotificationsQuery(principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value!.Select(item => item.Id).Should().Equal(
            newest.Id,
            tiedFirst.Id,
            tiedSecond.Id,
            oldest.Id);
    }

    [Fact]
    public async Task Given_ValidRecruiter_When_HandlingTwice_Then_DoesNotMutateOrRemoveStoredNotifications()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var recruiterId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var notification = CreateNotification(offerId, BaseTimestampUtc);

        var companyLookup = new FakeRecruiterCompanyAccessLookup();
        companyLookup.AssignRecruiterToCompany(recruiterId, companyId);
        companyLookup.AssignOffersToCompany(companyId, offerId);

        var reader = new FakeNotificationOutboxReader();
        reader.Seed(notification);

        var handler = new GetRecruiterNotificationsHandler(companyLookup, reader);
        var principal = CreateRecruiterPrincipal(recruiterId.ToString());

        // Act
        var firstResult = await handler.HandleAsync(new GetRecruiterNotificationsQuery(principal));
        var secondResult = await handler.HandleAsync(new GetRecruiterNotificationsQuery(principal));

        // Assert
        firstResult.Status.Should().Be(ResultStatus.Success);
        secondResult.Status.Should().Be(ResultStatus.Success);
        firstResult.Value.Should().Equal(ToResponse(notification));
        secondResult.Value.Should().Equal(ToResponse(notification));
        reader.Entries.Should().ContainSingle().Which.Should().BeSameAs(notification);
    }

    [Fact]
    public async Task Given_CancellationToken_When_Handling_Then_PropagatesTokenToLookupAndReader()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var recruiterId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();

        var companyLookup = new FakeRecruiterCompanyAccessLookup();
        companyLookup.AssignRecruiterToCompany(recruiterId, companyId);
        companyLookup.AssignOffersToCompany(companyId, offerId);

        var reader = new FakeNotificationOutboxReader();
        var handler = new GetRecruiterNotificationsHandler(companyLookup, reader);
        var principal = CreateRecruiterPrincipal(recruiterId.ToString());

        // Act
        var result = await handler.HandleAsync(
            new GetRecruiterNotificationsQuery(principal),
            cancellation.Token);

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        companyLookup.LastRecruiterLookupToken.Should().Be(cancellation.Token);
        companyLookup.LastCompanyOffersLookupToken.Should().Be(cancellation.Token);
        reader.LastCancellationToken.Should().Be(cancellation.Token);
    }

    [Fact]
    public async Task Given_CanceledToken_When_Handling_Then_ThrowsOperationCanceledException()
    {
        // Arrange
        var companyLookup = new FakeRecruiterCompanyAccessLookup();
        var reader = new FakeNotificationOutboxReader();
        var handler = new GetRecruiterNotificationsHandler(companyLookup, reader);
        var principal = CreateRecruiterPrincipal(Guid.NewGuid().ToString());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        // Act
        Func<Task> act = async () => await handler.HandleAsync(
            new GetRecruiterNotificationsQuery(principal),
            cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        companyLookup.RecruiterCompanyLookupCalls.Should().Be(0);
        reader.ReadCalls.Should().Be(0);
    }

    private static ClaimsPrincipal CreateRecruiterPrincipal(
        string? recruiterId,
        string? sub = null,
        string? nameIdentifier = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, "Recruiter") };

        if (recruiterId is not null)
        {
            claims.Add(new Claim(RecruiterIdClaimType, recruiterId));
        }

        if (sub is not null)
        {
            claims.Add(new Claim(SubjectClaimType, sub));
        }

        if (nameIdentifier is not null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, nameIdentifier));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestScheme"));
    }

    private static NotificationOutboxEntry CreateNotification(Guid offerId, DateTime occurredAtUtc) =>
        CreateNotificationWithId(Guid.NewGuid(), offerId, occurredAtUtc);

    private static NotificationOutboxEntry CreateNotificationWithId(
        Guid id,
        Guid offerId,
        DateTime occurredAtUtc) =>
        NotificationOutboxEntry.ShortlistedCandidacyWithdrawn(
            id,
            Guid.NewGuid(),
            offerId,
            Guid.NewGuid(),
            occurredAtUtc);

    private static GetRecruiterNotificationsResponse ToResponse(NotificationOutboxEntry entry) =>
        new(
            entry.Id,
            entry.CandidacyId,
            entry.OfferId,
            entry.StudentId,
            entry.Kind,
            entry.OccurredAtUtc);
}
