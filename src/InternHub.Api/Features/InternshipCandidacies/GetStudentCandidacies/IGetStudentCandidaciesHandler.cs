using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.GetStudentCandidacies;

public interface IGetStudentCandidaciesHandler
{
    Result<IReadOnlyList<GetStudentCandidaciesResponse>> Handle(GetStudentCandidaciesQuery query);
}
