namespace InternHub.Api.Features.Offers.CreateOffer;

public record CreateOfferRequest(
    string Title,
    string CompanyName,
    string Requirements,
    DateTime Deadline,
    int MaximumAvailableSlots);