using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InternHub.Api.Features.Students.CreateStudent;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Features.Students.GetStudentById;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.Students.GetStudentById;

public sealed class GetStudentByIdEndpointTests
{
    [Fact]
    public async Task Given_ExistingStudent_When_GettingStudentById_Then_ReturnsCorrectStudentFields()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var email = $"get-by-id-{Guid.NewGuid()}@mail.com";
        using var createResponse = await client.PostAsJsonAsync(
            "/api/v1/students",
            new CreateStudentRequest("Ana Pop", email));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateStudentResponse>();
        created.Should().NotBeNull();

        // Act
        using var response = await client.GetAsync($"/api/v1/students/{created!.Id}");
        var student = await response.Content.ReadFromJsonAsync<GetStudentByIdResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        student.Should().BeEquivalentTo(
            new GetStudentByIdResponse(created.Id, "Ana Pop", email));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        var persisted = await db.Students.AsNoTracking().SingleAsync(s => s.Id == created.Id);
        persisted.FullName.Should().Be("Ana Pop");
        persisted.Email.Should().Be(email);
    }

    [Fact]
    public async Task Given_MissingStudent_When_GettingStudentById_Then_ReturnsNotFound()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var studentId = Guid.NewGuid();

        // Act
        using var response = await client.GetAsync($"/api/v1/students/{studentId}");
        var error = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        error.Should().Contain(studentId.ToString());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await db.Students.AsNoTracking().CountAsync()).Should().Be(0);
    }
}
