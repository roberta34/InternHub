using InternHub.Domain.Students;
using InternHub.Features.Students.CreateStudent;
using InternHub.Features.Students.GetStudentById;
using InternHub.Features.Students.GetStudents;
using InternHub.Features.Students.UpdateStudent;
using InternHub.Features.Students.DeleteStudent;
using InternHub.Infrastructure.Repositories;

using InternHub.Api.Domain.Offers;
using InternHub.Api.Infrastructure.Repositories;
using InternHub.Api.Features.Offers.CreateOffer;
using InternHub.Api.Features.Offers.GetOfferById;
using InternHub.Api.Features.Offers.GetOffers;
using InternHub.Api.Features.Offers.UpdateOffer;
using InternHub.Api.Features.Offers.DeleteOffer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<
    IOfferRepository,
    InMemoryOfferRepository>();

builder.Services.AddSingleton<
    IStudentRepository,
    InMemoryStudentRepository>();

var app = builder.Build();

var v1 = app.MapGroup("/api/v1");

// Offer endpoints
CreateOfferEndpoint.Map(v1);
GetOfferByIdEndpoint.Map(v1);
GetOffersEndpoint.Map(v1);
UpdateOfferEndpoint.Map(v1);
DeleteOfferEndpoint.Map(v1);

// Student endpoints
CreateStudentEndpoint.Map(v1);
GetStudentByIdEndpoint.Map(v1);
GetStudentsEndpoint.Map(v1);
UpdateStudentEndpoint.Map(v1);
DeleteStudentEndpoint.Map(v1);

app.MapGet("/", () => "Hello World!");

app.Run();

public partial class Program
{
}