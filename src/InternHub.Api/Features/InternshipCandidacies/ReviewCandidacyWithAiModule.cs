using InternHub.Api.Features.InternshipCandidacies.ReviewCandidacyWithAi;
using InternHub.Api.Features.RecruiterReview;
using InternHub.Api.Features.Shared;
using InternHub.Api.Infrastructure.AI.RecruiterReview;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Api.Features.InternshipCandidacies;

/// <summary>
/// Recruiter AI review of a candidacy. IRecruiterEvaluationService and AiExecutionTrace are
/// registered once by CvFeedbackModule (AddRecruiterEvaluation); they are only consumed here.
/// </summary>
public sealed class ReviewCandidacyWithAiModule : IFeatureModule
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IRecruiterCandidacyReviewService, RecruiterReviewEvaluationAdapter>();
        services.AddScoped<IReviewCandidacyWithAiHandler, ReviewCandidacyWithAiHandler>();
    }

    public void MapEndpoints(RouteGroupBuilder group)
    {
        ReviewCandidacyWithAi.Endpoint.Map(group);
    }
}
