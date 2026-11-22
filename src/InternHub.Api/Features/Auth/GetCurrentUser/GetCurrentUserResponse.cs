using System.Text.Json.Serialization;

namespace InternHub.Api.Features.Auth.GetCurrentUser;

public sealed record GetCurrentUserResponse(
    Guid AccountId,
    string Role,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? StudentId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? RecruiterId);
