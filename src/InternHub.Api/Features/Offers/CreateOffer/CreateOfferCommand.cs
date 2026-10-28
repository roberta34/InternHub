namespace InternHub.Api.Features.Offers.CreateOffer;

public sealed record CreateOfferCommand(
    string Title,
    string CompanyName,
    string Requirements,
    DateTime Deadline,
    int MaximumAvailableSlots);
