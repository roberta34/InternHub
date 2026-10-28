using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InternHub.Api.Features.Students.CreateStudent;
using InternHub.Api.Features.Students.UpdateStudent;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.Students.UpdateStudent;

public sealed class UpdateStudentEndpointTests
{
    [Fact]
    public async Task Given_ExistingStudent_When_UpdatingStudent_Then_ReturnsNoContentAndPersistsUpdatedFields()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var originalEmail = $"before-{Guid.NewGuid()}@mail.com";
        var updatedEmail = $"after-{Guid.NewGuid()}@mail.com";
        using var createResponse = await client.PostAsJsonAsync(
            "/api/v1/students",
            new CreateStudentRequest("Ana Pop", originalEmail));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateStudentResponse>();
        created.Should().NotBeNull();

        // Act
        using var response = await client.PutAsJsonAsync(
            $"/api/v1/students/{created!.Id}",
            new UpdateStudentRequest("Ana Popescu", updatedEmail));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Content.Headers.ContentLength.Should().Be(0);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        var persisted = await db.Students.AsNoTracking().SingleAsync(s => s.Id == created.Id);
        persisted.FullName.Should().Be("Ana Popescu");
        persisted.Email.Should().Be(updatedEmail);
    }

    [Fact]
    public async Task Given_InvalidUpdateData_When_UpdatingStudent_Then_ReturnsBadRequestAndPreservesOriginalFields()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var originalEmail = $"preserve-{Guid.NewGuid()}@mail.com";
        using var createResponse = await client.PostAsJsonAsync(
            "/api/v1/students",
            new CreateStudentRequest("Ana Pop", originalEmail));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateStudentResponse>();
        created.Should().NotBeNull();

        // Act
        using var response = await client.PutAsJsonAsync(
            $"/api/v1/students/{created!.Id}",
            new UpdateStudentRequest("", "invalid-email"));
        var error = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        error.Should().Contain("Full name is required.");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        var persisted = await db.Students.AsNoTracking().SingleAsync(s => s.Id == created.Id);
        persisted.FullName.Should().Be("Ana Pop");
        persisted.Email.Should().Be(originalEmail);
    }

    [Fact]
    public async Task Given_MissingStudent_When_UpdatingStudent_Then_ReturnsNotFound()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var studentId = Guid.NewGuid();

        // Act
        using var response = await client.PutAsJsonAsync(
            $"/api/v1/students/{studentId}",
            new UpdateStudentRequest("Ana Pop", $"missing-{Guid.NewGuid()}@mail.com"));
        var error = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        error.Should().Contain(studentId.ToString());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await db.Students.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Given_EmailUsedByAnotherStudent_When_UpdatingStudent_Then_ReturnsConflictAndPreservesBothStudents()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var firstEmail = $"first-{Guid.NewGuid()}@mail.com";
        var secondEmail = $"second-{Guid.NewGuid()}@mail.com";
        var firstResponse = await client.PostAsJsonAsync(
            "/api/v1/students",
            new CreateStudentRequest("Ana Pop", firstEmail));
        var secondResponse = await client.PostAsJsonAsync(
            "/api/v1/students",
            new CreateStudentRequest("Ion Pop", secondEmail));
        var first = await firstResponse.Content.ReadFromJsonAsync<CreateStudentResponse>();
        var second = await secondResponse.Content.ReadFromJsonAsync<CreateStudentResponse>();
        first.Should().NotBeNull();
        second.Should().NotBeNull();

        // Act
        using var response = await client.PutAsJsonAsync(
            $"/api/v1/students/{second!.Id}",
            new UpdateStudentRequest("Ion Pop", firstEmail));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Contain("already exists");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        var persisted = await db.Students.AsNoTracking().OrderBy(s => s.FullName).ToListAsync();
        persisted.Should().HaveCount(2);
        persisted.Should().ContainSingle(s => s.FullName == "Ana Pop" && s.Email == firstEmail);
        persisted.Should().ContainSingle(s => s.FullName == "Ion Pop" && s.Email == secondEmail);
    }
}
