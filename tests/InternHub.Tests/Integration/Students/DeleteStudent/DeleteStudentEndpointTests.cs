using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InternHub.Api.Features.Students.CreateStudent;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.Students.DeleteStudent;

public sealed class DeleteStudentEndpointTests
{
    [Fact]
    public async Task Given_ExistingStudent_When_DeletingStudent_Then_ReturnsNoContentAndRemovesStudent()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        using var createResponse = await client.PostAsJsonAsync(
            "/api/v1/students",
            new CreateStudentRequest("Ana Pop", $"delete-{Guid.NewGuid()}@mail.com"));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateStudentResponse>();
        created.Should().NotBeNull();

        // Act
        using var response = await client.DeleteAsync($"/api/v1/students/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Content.Headers.ContentLength.Should().Be(0);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await db.Students.AsNoTracking().AnyAsync(s => s.Id == created.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task Given_MissingStudent_When_DeletingStudent_Then_ReturnsNotFoundAndPreservesDatabase()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var studentId = Guid.NewGuid();

        // Act
        using var response = await client.DeleteAsync($"/api/v1/students/{studentId}");
        var error = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        error.Should().Contain(studentId.ToString());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await db.Students.AsNoTracking().CountAsync()).Should().Be(0);
    }
}
