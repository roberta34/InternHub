using System.Net;
using System.Net.Http.Json;
using InternHub.Api.Features.Students.CreateStudent;
using InternHub.Features.Students.GetStudentById;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using InternHub.Features.Students.GetStudents;
using InternHub.Api.Features.Students.UpdateStudent;

namespace InternHub.Tests.Integration.Students;

[Collection("Integration")]
public class StudentEndpointsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Given_ValidStudent_WhenCreatingStudent_ThenReturns201WithLocation()
    {
        var res = await _client.PostAsJsonAsync(
            "/api/v1/students",
            new CreateStudentRequest(
                "Ana Pop",
                $"created-location-{Guid.NewGuid()}@mail.com"));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.NotNull(res.Headers.Location);
    }

    [Fact]
    public async Task Given_ValidStudent_WhenCreatingStudent_ThenReturnsCreatedStudent()
    {
        var res = await _client.PostAsJsonAsync(
            "/api/v1/students",
            new CreateStudentRequest("Ana Pop", $"create-{Guid.NewGuid()}@mail.com"));

        var student = await res.Content.ReadFromJsonAsync<CreateStudentResponse>();

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.NotNull(student);
        Assert.NotEqual(Guid.Empty, student.Id);
        Assert.Equal("Ana Pop", student.FullName);
    }

    [Fact]
    public async Task Given_InvalidStudent_WhenCreatingStudent_ThenReturns400()
    {
        var res = await _client.PostAsJsonAsync("/api/v1/students", new CreateStudentRequest("", "bad"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Given_ExistingStudent_WhenGettingStudentById_ThenReturns200()
    {
        var created = await _client.PostAsJsonAsync(
            "/api/v1/students",
            new CreateStudentRequest(
                "Ana Pop",
                $"get-status-{Guid.NewGuid()}@mail.com"));
        var res = await _client.GetAsync(created.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Given_ExistingStudent_WhenGettingStudentById_ThenReturnsStudent()
    {
        var email = $"get-{Guid.NewGuid()}@mail.com";
        var created = await _client.PostAsJsonAsync(
            "/api/v1/students",
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
    public async Task Given_MissingStudent_WhenGettingStudentById_ThenReturns404()
    {
        var res = await _client.GetAsync($"/api/v1/students/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
    
    [Fact]
    public async Task Given_ValidStudent_WhenGettingStudents_ThenReturns200()
    {
        var response = await _client.GetAsync("/api/v1/students");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    
    [Fact]
    public async Task Given_ValidStudent_WhenGettingStudents_ThenContainsStudent()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/students",
            new CreateStudentRequest(
                "Maria Popescu",
                $"list-{Guid.NewGuid()}@mail.com"));

        var createdStudent =
            await createResponse.Content.ReadFromJsonAsync<CreateStudentResponse>();

        Assert.NotNull(createdStudent);

        var response = await _client.GetAsync("/api/v1/students");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var students =
            await response.Content.ReadFromJsonAsync<List<GetStudentsResponse>>();

        Assert.NotNull(students);

        Assert.Contains(
            students,
            student => student.Id == createdStudent.Id);
    }
    
    [Fact]
    public async Task Update_ExistingStudent_Returns204()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/students",
            new CreateStudentRequest(
                "Ana Pop",
                $"update-{Guid.NewGuid()}@mail.com"));

        var createdStudent =
            await createResponse.Content.ReadFromJsonAsync<CreateStudentResponse>();

        Assert.NotNull(createdStudent);

        var updateRequest = new UpdateStudentRequest(
            "Ana Popescu",
            $"updated-{Guid.NewGuid()}@mail.com");

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/students/{createdStudent.Id}",
            updateRequest);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
    
    [Fact]
    public async Task Update_ExistingStudent_ChangesStudentData()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/students",
            new CreateStudentRequest(
                "Ana Pop",
                $"before-{Guid.NewGuid()}@mail.com"));

        var createdStudent =
            await createResponse.Content.ReadFromJsonAsync<CreateStudentResponse>();

        Assert.NotNull(createdStudent);

        var newEmail = $"after-{Guid.NewGuid()}@mail.com";

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/v1/students/{createdStudent.Id}",
            new UpdateStudentRequest(
                "Ana Popescu",
                newEmail));

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var getResponse =
            await _client.GetAsync($"/api/v1/students/{createdStudent.Id}");

        var updatedStudent =
            await getResponse.Content.ReadFromJsonAsync<GetStudentByIdResponse>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(updatedStudent);

        Assert.Equal("Ana Popescu", updatedStudent.FullName);
        Assert.Equal(newEmail, updatedStudent.Email);
    }
    
    [Fact]
    public async Task Update_MissingStudent_Returns404()
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/v1/students/{Guid.NewGuid()}",
            new UpdateStudentRequest(
                "Ana Pop",
                "ana@mail.com"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task Update_InvalidData_Returns400()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/students",
            new CreateStudentRequest(
                "Ana Pop",
                $"invalid-update-{Guid.NewGuid()}@mail.com"));

        var createdStudent =
            await createResponse.Content.ReadFromJsonAsync<CreateStudentResponse>();

        Assert.NotNull(createdStudent);

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/students/{createdStudent.Id}",
            new UpdateStudentRequest(
                "",
                "bad-email"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task Delete_ExistingStudent_Returns204()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/students",
            new CreateStudentRequest(
                "Ana Pop",
                $"delete-{Guid.NewGuid()}@mail.com"));

        var createdStudent =
            await createResponse.Content.ReadFromJsonAsync<CreateStudentResponse>();

        Assert.NotNull(createdStudent);

        var response =
            await _client.DeleteAsync($"/api/v1/students/{createdStudent.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
    
    [Fact]
    public async Task Delete_ExistingStudent_ThenGetReturns404()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/students",
            new CreateStudentRequest(
                "Ana Pop",
                $"delete-get-{Guid.NewGuid()}@mail.com"));

        var createdStudent =
            await createResponse.Content.ReadFromJsonAsync<CreateStudentResponse>();

        Assert.NotNull(createdStudent);

        var deleteResponse =
            await _client.DeleteAsync($"/api/v1/students/{createdStudent.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse =
            await _client.GetAsync($"/api/v1/students/{createdStudent.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
    
    [Fact]
    public async Task Delete_MissingStudent_Returns404()
    {
        var response =
            await _client.DeleteAsync($"/api/v1/students/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}