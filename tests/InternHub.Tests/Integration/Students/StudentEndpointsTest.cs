using System.Net;
using System.Net.Http.Json;
using InternHub.Features.Students.CreateStudent;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace InternHub.Tests.Integration.Students;

public class StudentEndpointsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_Valid_Returns201WithLocation()
    {
        var res = await _client.PostAsJsonAsync("/students", new CreateStudentRequest("Ana Pop", "ana@mail.com"));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.NotNull(res.Headers.Location);
    }

    [Fact]
    public async Task Create_Invalid_Returns400()
    {
        var res = await _client.PostAsJsonAsync("/students", new CreateStudentRequest("", "bad"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task GetById_Existing_Returns200()
    {
        var created = await _client.PostAsJsonAsync("/students", new CreateStudentRequest("Ana Pop", "ana2@mail.com"));
        var res = await _client.GetAsync(created.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task GetById_Missing_Returns404()
    {
        var res = await _client.GetAsync($"/students/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}