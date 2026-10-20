namespace InternHub.Features.Students.GetStudentById;


public sealed record GetStudentByIdResponse(
    Guid Id, string FullName, string Email);
