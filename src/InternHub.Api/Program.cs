using InternHub.Api.Domain.Offers;
using InternHub.Api.Infrastructure.Repositories;
using InternHub.Api.Features.Offers.CreateOffer;
using InternHub.Api.Features.Offers.GetOfferById;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IOfferRepository, InMemoryOfferRepository>();

var app = builder.Build();

CreateOfferEndpoint.Map(app);
GetOfferByIdEndpoint.Map(app);
app.MapGet("/", () => "Hello World!");

app.Run();

public partial class Program { }