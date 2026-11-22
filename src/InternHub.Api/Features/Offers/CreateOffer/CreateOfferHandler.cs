using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Offers.CreateOffer;

public sealed class CreateOfferHandler(IOfferRepository offers) : ICreateOfferHandler
{
    public Result<CreateOfferResponse> Handle(CreateOfferCommand command)
    {
        if (!command.RecruiterId.HasValue || command.RecruiterId == Guid.Empty)
            return Result<CreateOfferResponse>.Unauthorized("Recruiter authentication is required.");

        var offerResult = Offer.Create(
            command.Title,
            command.CompanyName,
            command.Requirements,
            command.Deadline,
            command.MaximumAvailableSlots,
            command.RecruiterId);

        if (offerResult.Status != ResultStatus.Success)
            return Result<CreateOfferResponse>.Validation(offerResult.Error!);

        var offer = offerResult.Value!;

        if (!offers.Add(offer))
            return Result<CreateOfferResponse>.Conflict(
                $"Offer {offer.Id} already exists.");

        return Result<CreateOfferResponse>.Success(
            new CreateOfferResponse(
                offer.Id,
                offer.Title,
                offer.CompanyName,
                offer.Requirements,
                offer.Deadline,
                offer.MaximumAvailableSlots,
                offer.IsClosed,
                offer.Version));
    }
}