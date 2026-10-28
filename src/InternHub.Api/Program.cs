using InternHub.Domain.Students;
using InternHub.Infrastructure.Repositories;

using InternHub.Api.Domain.Offers;
using InternHub.Api.Infrastructure.Repositories;
using InternHub.Api.Features.Shared;
using InternHub.Api.Features.Students;
using InternHub.Api.Features.Offers.CreateOffer;
using InternHub.Api.Features.Offers.GetOfferById;
using InternHub.Api.Features.Offers.GetOffers;
using InternHub.Api.Features.Offers.UpdateOffer;
using InternHub.Api.Features.Offers.DeleteOffer;
using InternHub.Api.Features.Students.GetStudentById;
using InternHub.Api.Features.Students.GetStudents;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<
    IOfferRepository,
    InMemoryOfferRepository>();

builder.Services.AddSingleton<
    IStudentRepository,
    InMemoryStudentRepository>();
builder.Services.AddStudentFeatures();

var app = builder.Build();

var v1 = app.MapV1();

// Offer endpoints
CreateOfferEndpoint.Map(v1);
GetOfferByIdEndpoint.Map(v1);
GetOffersEndpoint.Map(v1);
UpdateOfferEndpoint.Map(v1);
DeleteOfferEndpoint.Map(v1);

// Student endpoints
InternHub.Api.Features.Students.CreateStudent.Endpoint.Map(v1);
InternHub.Api.Features.Students.GetStudentById.Endpoint.Map(v1);
InternHub.Api.Features.Students.GetStudents.Endpoint.Map(v1);
InternHub.Api.Features.Students.UpdateStudent.Endpoint.Map(v1);
InternHub.Api.Features.Students.DeleteStudent.Endpoint.Map(v1);

app.MapGet("/", () => "Hello World!");

app.Run();

public partial class Program
{
}