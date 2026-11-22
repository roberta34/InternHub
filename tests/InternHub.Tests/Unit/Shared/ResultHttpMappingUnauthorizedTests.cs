using FluentAssertions;
using InternHub.Api.Features.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Unit.Shared;

public sealed class ResultHttpMappingUnauthorizedTests
{
    [Fact]
    public async Task Given_UnauthorizedResult_When_MappingFailure_Then_Returns401()
    {
        // Arrange
        var result = Result<string>.Unauthorized("Unauthorized.");
        using var services = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        var httpResult = result.ToFailure();

        // Act
        await httpResult.ExecuteAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }
}
