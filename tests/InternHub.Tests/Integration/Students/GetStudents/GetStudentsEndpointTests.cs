using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InternHub.Api.Features.Students.CreateStudent;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Features.Students.GetStudents;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.Students.GetStudents;

public sealed class GetStudentsEndpointTests
{
    [Fact]
    public async Task Given_EmptyDatabase_When_GettingStudents_Then_ReturnsOkAndEmptyArray()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/api/v1/students");
        var students = await response.Content.ReadFromJsonAsync<List<GetStudentsResponse>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        students.Should().NotBeNull();
        students.Should().BeEmpty();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await db.Students.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Given_ExistingStudents_When_GettingStudents_Then_ReturnsAllStudentFields()
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
        using var response = await client.GetAsync("/api/v1/students");
        var students = await response.Content.ReadFromJsonAsync<List<GetStudentsResponse>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        students.Should().BeEquivalentTo(
            new[]
            {
                new GetStudentsResponse(first!.Id, "Ana Pop", firstEmail),
                new GetStudentsResponse(second!.Id, "Ion Pop", secondEmail)
            },
            options => options.WithoutStrictOrdering());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        var persisted = await db.Students.AsNoTracking().ToListAsync();
        persisted.Should().HaveCount(2);
        persisted.Select(student => student.Email)
            .Should().BeEquivalentTo(new[] { firstEmail, secondEmail });
    }
}
