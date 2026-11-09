using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.Offers.CvFeedback;
using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace InternHub.Tests.Integration.Offers.CvFeedback;

[Collection("Integration")]
public sealed class CvFeedbackEndpointTests
{
    private static readonly DateTime Deadline =
        new(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_ValidCvAndExistingOffer_When_RequestingCvFeedback_Then_ReturnsFeedbackAndDoesNotPersistOrLogCv()
    {
        // Arrange
        const string cvText = "Candidate CV content that must remain private.";
        const string offerRequirements = "C#, ASP.NET Core";
        var evaluation = new RecruiterEvaluationResult(
            ["C#"],
            ["ASP.NET Core"],
            ["Add a project demonstrating API design."],
            "The candidate has a relevant foundation.");
        var evaluator = new FakeRecruiterEvaluationService(evaluation);
        using var factory = CreateFactory(evaluator);
        using var client = factory.CreateClient();
        var logProvider = new RecordingLoggerProvider();
        factory.Services.GetRequiredService<ILoggerFactory>().AddProvider(logProvider);
        var offer = await SeedOffer(factory, offerRequirements);

        // Act
        using var response = await client.PostAsJsonAsync(
            $"/api/v1/offers/{offer.Id}/cv-feedback",
            new { cvText });
        var body = await response.Content.ReadFromJsonAsync<CvFeedbackResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotBeNull();
        body!.MatchedSkills.Should().Equal("C#");
        body.MissingSkills.Should().Equal("ASP.NET Core");
        body.Suggestions.Should().Equal("Add a project demonstrating API design.");
        body.Summary.Should().Be("The candidate has a relevant foundation.");
        evaluator.Calls.Should().Be(1);
        evaluator.ReceivedCv.Should().Be(cvText);
        evaluator.ReceivedRequirements.Should().Be(offerRequirements);
        logProvider.Entries.Should().NotContain(
            entry => entry.Contains(cvText, StringComparison.Ordinal));
        await AssertOnlyOfferIsPersisted(factory, offer.Id);
    }

    [Fact]
    public async Task Given_MissingOffer_When_RequestingCvFeedback_Then_ReturnsNotFoundWithoutCallingEvaluatorOrChangingDatabase()
    {
        // Arrange
        var evaluator = new FakeRecruiterEvaluationService();
        using var factory = CreateFactory(evaluator);
        using var client = factory.CreateClient();
        var offerId = Guid.NewGuid();

        // Act
        using var response = await client.PostAsJsonAsync(
            $"/api/v1/offers/{offerId}/cv-feedback",
            new { cvText = "Candidate CV content." });
        var error = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        error.Should().Contain($"Offer {offerId} not found.");
        evaluator.Calls.Should().Be(0);
        await AssertNoOffersOrCandidacies(factory);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n ")]
    public async Task Given_InvalidCv_When_RequestingCvFeedback_Then_ReturnsBadRequestWithoutCallingEvaluatorOrChangingDatabase(
        string? cvText)
    {
        // Arrange
        var evaluator = new FakeRecruiterEvaluationService();
        using var factory = CreateFactory(evaluator);
        using var client = factory.CreateClient();
        var offer = await SeedOffer(factory);

        // Act
        using var response = await client.PostAsJsonAsync(
            $"/api/v1/offers/{offer.Id}/cv-feedback",
            new { cvText });
        var error = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        error.Should().Contain("CV text must not be empty.");
        evaluator.Calls.Should().Be(0);
        await AssertOnlyOfferIsPersisted(factory, offer.Id);
    }

    [Fact]
    public async Task Given_CvOverMaximumLength_When_RequestingCvFeedback_Then_ReturnsBadRequestWithoutCallingEvaluatorOrChangingDatabase()
    {
        // Arrange
        var evaluator = new FakeRecruiterEvaluationService();
        using var factory = CreateFactory(evaluator);
        using var client = factory.CreateClient();
        var offer = await SeedOffer(factory);
        var cvText = new string('a', 20_001);

        // Act
        using var response = await client.PostAsJsonAsync(
            $"/api/v1/offers/{offer.Id}/cv-feedback",
            new { cvText });
        var error = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        error.Should().Contain("CV text must not exceed 20000 characters.");
        evaluator.Calls.Should().Be(0);
        await AssertOnlyOfferIsPersisted(factory, offer.Id);
    }

    [Fact]
    public async Task Given_EvaluatorUnavailable_When_RequestingCvFeedback_Then_ReturnsServiceUnavailableWithoutLeakingCvOrChangingDatabase()
    {
        // Arrange
        const string cvText = "Private CV content for provider failure.";
        var evaluator = new FakeRecruiterEvaluationService(
            exception: new HttpRequestException("Provider connection failed."));
        using var factory = CreateFactory(evaluator);
        using var client = factory.CreateClient();
        var logProvider = new RecordingLoggerProvider();
        factory.Services.GetRequiredService<ILoggerFactory>().AddProvider(logProvider);
        var offer = await SeedOffer(factory);

        // Act
        using var response = await client.PostAsJsonAsync(
            $"/api/v1/offers/{offer.Id}/cv-feedback",
            new { cvText });
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        problem.Should().NotBeNull();
        problem!.Detail.Should().Be("CV feedback is temporarily unavailable.");
        problem.Detail.Should().NotContain(cvText);
        logProvider.Entries.Should().NotContain(
            entry => entry.Contains(cvText, StringComparison.Ordinal));
        evaluator.Calls.Should().Be(1);
        await AssertOnlyOfferIsPersisted(factory, offer.Id);
    }

    private static InternHubSqliteFactory CreateFactory(
        FakeRecruiterEvaluationService evaluator) =>
        new(configureTestServices: services =>
        {
            services.RemoveAll<IRecruiterEvaluationService>();
            services.AddSingleton<IRecruiterEvaluationService>(evaluator);
        });

    private static async Task<Offer> SeedOffer(
        InternHubSqliteFactory factory,
        string requirements = "C#")
    {
        var offer = new Offer(
            "Software Engineering Intern",
            "InternHub",
            requirements,
            Deadline,
            2);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        db.Offers.Add(offer);
        await db.SaveChangesAsync();

        return offer;
    }

    private static async Task AssertOnlyOfferIsPersisted(
        InternHubSqliteFactory factory,
        Guid offerId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();

        var offers = await db.Offers.AsNoTracking().ToListAsync();
        offers.Should().ContainSingle().Which.Id.Should().Be(offerId);
        (await db.InternshipCandidacies.AsNoTracking().CountAsync()).Should().Be(0);
        (await db.Students.AsNoTracking().CountAsync()).Should().Be(0);
    }

    private static async Task AssertNoOffersOrCandidacies(InternHubSqliteFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();

        (await db.Offers.AsNoTracking().CountAsync()).Should().Be(0);
        (await db.InternshipCandidacies.AsNoTracking().CountAsync()).Should().Be(0);
        (await db.Students.AsNoTracking().CountAsync()).Should().Be(0);
    }

    private sealed class FakeRecruiterEvaluationService(
        RecruiterEvaluationResult? result = null,
        Exception? exception = null) : IRecruiterEvaluationService
    {
        private readonly RecruiterEvaluationResult _result = result ?? new(
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            string.Empty);

        public int Calls { get; private set; }
        public string? ReceivedCv { get; private set; }
        public string? ReceivedRequirements { get; private set; }

        public Task<RecruiterEvaluationResult> AnalyzeAsync(
            string cvContent,
            string offerRequirements,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            ReceivedCv = cvContent;
            ReceivedRequirements = offerRequirements;

            if (exception is not null)
                throw exception;

            return Task.FromResult(_result);
        }
    }

    private sealed class RecordingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<string> _entries = new();

        public IReadOnlyCollection<string> Entries => _entries.ToArray();

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(_entries);

        public void Dispose()
        {
        }

        private sealed class RecordingLogger(ConcurrentQueue<string> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull =>
                NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter) =>
                entries.Enqueue(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
