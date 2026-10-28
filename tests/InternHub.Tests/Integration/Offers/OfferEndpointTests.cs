using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InternHub.Tests.Integration.Offers;

[Collection("Integration")]
public class OfferEndpointTests :
    IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OfferEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateOffer_WithValidData_ReturnsCreated()
    {
        var request = new
        {
            title = "Software Engineer Intern",
            companyName = "Microsoft",
            requirements = "C#, .NET, Git",
            deadline = DateTime.UtcNow.AddDays(30),
            maximumAvailableSlots = 3
        };

        var response = await _client.PostAsJsonAsync("/api/v1/offers", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task CreateOffer_WithEmptyTitle_ReturnsBadRequest()
    {
        var request = new
        {
            title = "",
            companyName = "Microsoft",
            requirements = "C#, .NET, Git",
            deadline = DateTime.UtcNow.AddDays(30),
            maximumAvailableSlots = 3
        };

        var response = await _client.PostAsJsonAsync("/api/v1/offers", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOfferById_WithMissingId_ReturnsNotFound()
    {
        var id = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/v1/offers/{id}");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateThenGetOffer_ReturnsOk()
    {
        var request = new
        {
            title = "Backend Intern",
            companyName = "Test Company",
            requirements = "C#, Git",
            deadline = DateTime.UtcNow.AddDays(30),
            maximumAvailableSlots = 2
        };

        var createResponse =
            await _client.PostAsJsonAsync("/api/v1/offers", request);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdOffer =
            await createResponse.Content.ReadFromJsonAsync<CreatedOfferResponse>();

        Assert.NotNull(createdOffer);

        var getResponse =
            await _client.GetAsync($"/api/v1/offers/{createdOffer.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    private record CreatedOfferResponse(
        Guid Id,
        string Title,
        string CompanyName,
        string Requirements,
        DateTime Deadline,
        int MaximumAvailableSlots,
        bool IsClosed,
        int Version);
}