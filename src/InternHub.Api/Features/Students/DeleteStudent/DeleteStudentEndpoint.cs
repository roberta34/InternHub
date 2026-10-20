using InternHub.Domain.Students;
using Microsoft.AspNetCore.Http.HttpResults;

namespace InternHub.Features.Students.DeleteStudent;

public static class DeleteStudentEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/students/{id:guid}", Handle);

    private static Results<NoContent, NotFound> Handle(
        Guid id,
        IStudentRepository repository)
    {
        return repository.Delete(id)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }
}