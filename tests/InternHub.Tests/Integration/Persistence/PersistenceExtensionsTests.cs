using FluentAssertions;
using InternHub.Api.Features.InternshipCandidacies.AtomicSubmission;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Api.Infrastructure.Persistence.AtomicSubmission;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.Persistence;

public sealed class PersistenceExtensionsTests
{
    [Fact]
    public void Given_PersistenceServices_When_ResolvingAtomicSubmission_Then_UsesScopedDependencies()
    {
        // Arrange
        var services = new ServiceCollection();
        var timeProvider = TimeProvider.System;
        var configuration = new ConfigurationBuilder().Build();

        services.AddPersistence(configuration);
        services.AddSingleton(timeProvider);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });

        // Act
        using var firstScope = provider.CreateScope();
        var submission = firstScope.ServiceProvider
            .GetRequiredService<IAtomicCandidacySubmission>();
        var db = firstScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        var resolvedTimeProvider = firstScope.ServiceProvider
            .GetRequiredService<TimeProvider>();

        using var secondScope = provider.CreateScope();
        var secondSubmission = secondScope.ServiceProvider
            .GetRequiredService<IAtomicCandidacySubmission>();
        // Assert
        // Assert
        submission.Should().BeOfType<EfCoreAtomicCandidacySubmission>();
        secondSubmission.Should().NotBeSameAs(submission);
        firstScope.ServiceProvider.GetRequiredService<InternHubDbContext>()
            .Should().BeSameAs(db);
        resolvedTimeProvider.Should().BeSameAs(timeProvider);
    }
}
