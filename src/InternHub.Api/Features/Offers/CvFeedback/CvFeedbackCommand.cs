namespace InternHub.Api.Features.Offers.CvFeedback;

public sealed record CvFeedbackCommand(Guid OfferId, string? CvText);
