using System.Net;
using System.Net.Http.Json;
using InternHub.Features.Students.CreateStudent;
using InternHub.Features.Students.GetStudentById;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace InternHub.Tests.Integration.Students;

public class StudentEndpointsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
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
    public async Task Create_Valid_ReturnsCreatedStudent()
    {
        var res = await _client.PostAsJsonAsync(
            "/students",
            new CreateStudentRequest("Ana Pop", $"create-{Guid.NewGuid()}@mail.com"));

        var student = await res.Content.ReadFromJsonAsync<CreateStudentResponse>();

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.NotNull(student);
        Assert.NotEqual(Guid.Empty, student.Id);
        Assert.Equal("Ana Pop", student.FullName);
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
    public async Task GetById_Existing_ReturnsStudent()
    {
        var email = $"get-{Guid.NewGuid()}@mail.com";
        var created = await _client.PostAsJsonAsync(
            "/students",
            new CreateStudentRequest("Ana Pop", email));

        var response = await _client.GetAsync(created.Headers.Location);
        var student = await response.Content.ReadFromJsonAsync<GetStudentByIdResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(student);
        Assert.NotEqual(Guid.Empty, student.Id);
        Assert.Equal("Ana Pop", student.FullName);
        Assert.Equal(email, student.Email);
    }

    [Fact]
    public async Task GetById_Missing_Returns404()
    {
        var res = await _client.GetAsync($"/students/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}