using InternHub.Api.Features.Shared;
using InternHub.Features.Students.GetStudentById;

namespace InternHub.Api.Features.Students.GetStudentById;

public interface IGetStudentByIdHandler
{
    Result<GetStudentByIdResponse> Handle(GetStudentByIdQuery query);
}
