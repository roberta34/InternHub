using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using InternHub.Api.Infrastructure.AI.Ollama;
using Microsoft.Extensions.Configuration;

namespace InternHub.Tests.Unit.AI.Ollama;

public sealed class OllamaRecruiterEvaluationServiceTests
{
    [Fact]
    public async Task Given_ValidGeneratedEvaluation_When_Analyzing_Then_MapsAllFields()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            OllamaResponse("""{"matchedSkills":["C#"],"missingSkills":["Docker"],"suggestions":["Study Docker."],"summary":"Good match."}"""));
        var service = CreateService(handler);

        // Act
        var result = await service.AnalyzeAsync("C# developer", "C#, Docker");

        // Assert
        result.Should().BeEquivalentTo(new
        {
            MatchedSkills = (IReadOnlyList<string>)["C#"],
            MissingSkills = (IReadOnlyList<string>)["Docker"],
            Suggestions = (IReadOnlyList<string>)["Study Docker."],
            Summary = "Good match."
        });
    }

    [Fact]
    public async Task Given_AnalysisRequest_When_Analyzing_Then_SendsConfiguredOllamaPayload()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            OllamaResponse("""{"matchedSkills":[],"missingSkills":[],"suggestions":[],"summary":"No gaps."}"""));
        var service = CreateService(handler);

        // Act
        await service.AnalyzeAsync("Python", "Python");

        // Assert
        var request = handler.Requests.Should().ContainSingle().Which;
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri.Should().Be("http://ollama.test:11434/api/generate");
        using var payload = JsonDocument.Parse(request.Body);
        payload.RootElement.GetProperty("model").GetString().Should().Be("test-model");
        payload.RootElement.GetProperty("stream").GetBoolean().Should().BeFalse();
        payload.RootElement.GetProperty("format").GetString().Should().Be("json");
        payload.RootElement.GetProperty("options").GetProperty("temperature")
            .GetDouble().Should().Be(0.1);
        payload.RootElement.GetProperty("prompt").GetString()
            .Should().Contain("internhub-recruiter-evaluation-v1");
    }

    [Fact]
    public async Task Given_MalformedFirstEvaluation_When_Analyzing_Then_RetriesOnceAndSucceeds()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            OllamaResponse("{not-json}"),
            OllamaResponse("""{"matchedSkills":[],"missingSkills":[],"suggestions":[],"summary":"Valid."}"""));
        var service = CreateService(handler);

        // Act
        var result = await service.AnalyzeAsync("CV", "Requirements");

        // Assert
        result.Summary.Should().Be("Valid.");
        handler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task Given_InvalidEvaluationsOnBothAttempts_When_Analyzing_Then_ThrowsWithoutThirdRequest()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            OllamaResponse("{}"),
            OllamaResponse("{}"));
        var service = CreateService(handler);

        // Act
        var action = () => service.AnalyzeAsync("CV", "Requirements");

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*invalid recruiter evaluation*");
        handler.Requests.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("""{"missingSkills":[],"suggestions":[],"summary":"x"}""")]
    [InlineData("""{"matchedSkills":"C#","missingSkills":[],"suggestions":[],"summary":"x"}""")]
    [InlineData("""{"matchedSkills":[],"missingSkills":[],"suggestions":[],"summary":""}""")]
    public async Task Given_MissingOrInvalidRequiredField_When_Analyzing_Then_RetriesAndFails(
        string invalidEvaluation)
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            OllamaResponse(invalidEvaluation),
            OllamaResponse(invalidEvaluation));
        var service = CreateService(handler);

        // Act
        var action = () => service.AnalyzeAsync("CV", "Requirements");

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
        handler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task Given_HttpFailure_When_Analyzing_Then_PropagatesHttpFailureWithoutRetry()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var service = CreateService(handler);

        // Act
        var action = () => service.AnalyzeAsync("CV", "Requirements");

        // Assert
        await action.Should().ThrowAsync<HttpRequestException>();
        handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_CanceledToken_When_Analyzing_Then_PropagatesCancellation()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            OllamaResponse("""{"matchedSkills":[],"missingSkills":[],"suggestions":[],"summary":"x"}"""));
        var service = CreateService(handler);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        // Act
        var action = () => service.AnalyzeAsync(
            "CV",
            "Requirements",
            cancellationSource.Token);

        // Assert
        await action.Should().ThrowAsync<OperationCanceledException>();
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_SlowOllamaResponse_When_HttpClientTimesOut_Then_PropagatesCancellation()
    {
        // Arrange
        var handler = new TimeoutHttpMessageHandler();
        using var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMilliseconds(50)
        };
        var service = new OllamaRecruiterEvaluationService(
            httpClient,
            CreateConfiguration());

        // Act
        var action = () => service.AnalyzeAsync("CV", "Requirements");

        // Assert
        await action.Should().ThrowAsync<OperationCanceledException>();
        handler.Requests.Should().ContainSingle();
    }

    private static OllamaRecruiterEvaluationService CreateService(
        FakeHttpMessageHandler handler)
    {
        var client = new HttpClient(handler);
        return new OllamaRecruiterEvaluationService(client, CreateConfiguration());
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:Ollama:Endpoint"] = "http://ollama.test:11434",
                ["Ai:Ollama:Model"] = "test-model"
            })
            .Build();

    private static HttpResponseMessage OllamaResponse(string generatedJson) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { response = generatedJson }),
                Encoding.UTF8,
                "application/json")
        };

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, CancellationToken, HttpResponseMessage>> _responses;

        public FakeHttpMessageHandler(params HttpResponseMessage[] responses)
            : this(responses
                .Select(response =>
                    (Func<HttpRequestMessage, CancellationToken, HttpResponseMessage>)
                    ((_, _) => response))
                .ToArray())
        {
        }

        public FakeHttpMessageHandler(
            params Func<HttpRequestMessage, CancellationToken, HttpResponseMessage>[] responses)
        {
            _responses = new Queue<Func<HttpRequestMessage, CancellationToken, HttpResponseMessage>>(responses);
        }

        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(new CapturedRequest(
                request.Method,
                request.RequestUri,
                await request.Content!.ReadAsStringAsync(cancellationToken)));
            return _responses.Dequeue()(request, cancellationToken);
        }
    }

    private sealed record CapturedRequest(
        HttpMethod Method,
        Uri? RequestUri,
        string Body);

    private sealed class TimeoutHttpMessageHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new InvalidOperationException("The timeout should cancel the request.");
        }
    }
}
