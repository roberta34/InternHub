using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InternHub.Api.Features.Students.CreateStudent;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.Students.CreateStudent;

public sealed class CreateStudentEndpointTests
{
    [Fact]
    public async Task Given_ValidStudent_When_CreatingStudent_Then_ReturnsCreatedStudentAndPersistsIt()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var email = $"create-{Guid.NewGuid()}@mail.com";
        var request = new CreateStudentRequest("Ana Pop", email);

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/students", request);
        var created = await response.Content.ReadFromJsonAsync<CreateStudentResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().MatchRegex(
            @"^/api/v1/students/[0-9a-fA-F-]{36}$");
        created.Should().NotBeNull();
        created!.Id.Should().NotBe(Guid.Empty);
        created.FullName.Should().Be("Ana Pop");
        created.Email.Should().Be(email);
        response.Headers.Location!.ToString().Should().EndWith(created.Id.ToString());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        var persisted = await db.Students.AsNoTracking()
            .SingleAsync(student => student.Id == created.Id);
        persisted.FullName.Should().Be("Ana Pop");
        persisted.Email.Should().Be(email);
    }

    [Fact]
    public async Task Given_InvalidStudent_When_CreatingStudent_Then_ReturnsBadRequestAndDoesNotPersist()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var request = new CreateStudentRequest("", "invalid-email");

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/students", request);
        var error = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        error.Should().Contain("Full name is required.");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await db.Students.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Given_ExistingStudentEmail_When_CreatingDuplicateStudent_Then_ReturnsConflictAndPreservesOriginalStudent()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var email = $"duplicate-{Guid.NewGuid()}@mail.com";
        var request = new CreateStudentRequest("Ana Pop", email);
        var firstResponse = await client.PostAsJsonAsync("/api/v1/students", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        using var secondResponse = await client.PostAsJsonAsync(
            "/api/v1/students",
            new CreateStudentRequest("Ion Pop", email));
        var error = await secondResponse.Content.ReadAsStringAsync();

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        error.Should().Contain("already exists");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await db.Students.AsNoTracking().CountAsync()).Should().Be(1);
        var original = await db.Students.AsNoTracking().SingleAsync();
        original.FullName.Should().Be("Ana Pop");
        original.Email.Should().Be(email);
    }
}
