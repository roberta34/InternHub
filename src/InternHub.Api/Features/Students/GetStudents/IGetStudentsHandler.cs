using InternHub.Api.Features.Shared;
using InternHub.Features.Students.GetStudents;

namespace InternHub.Api.Features.Students.GetStudents;

public interface IGetStudentsHandler
{
    Result<IReadOnlyList<GetStudentsResponse>> Handle(GetStudentsQuery query);
}
