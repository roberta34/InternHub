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

// Offer endpoints
CreateOfferEndpoint.Map(app);
GetOfferByIdEndpoint.Map(app);
GetOffersEndpoint.Map(app);
UpdateOfferEndpoint.Map(app);
DeleteOfferEndpoint.Map(app);

// Student endpoints
CreateStudentEndpoint.Map(app);
GetStudentByIdEndpoint.Map(app);
GetStudentsEndpoint.Map(app);
UpdateStudentEndpoint.Map(app);
DeleteStudentEndpoint.Map(app);

app.MapGet("/", () => "Hello World!");

app.Run();

public partial class Program
{
}