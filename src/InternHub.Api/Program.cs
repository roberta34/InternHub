using InternHub.Domain.Students;
using InternHub.Features.Students.UpdateStudent;
using InternHub.Features.Students.DeleteStudent;
using InternHub.Infrastructure.Repositories;

using InternHub.Api.Domain.Offers;
using InternHub.Api.Infrastructure.Repositories;
using InternHub.Api.Features.Shared;
using InternHub.Api.Features.Students;
using InternHub.Api.Features.Offers;
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
builder.Services.AddOfferFeatures();

var app = builder.Build();

var v1 = app.MapV1();

// Offer endpoints
InternHub.Api.Features.Offers.CreateOffer.Endpoint.Map(v1);
InternHub.Api.Features.Offers.GetOfferById.Endpoint.Map(v1);
InternHub.Api.Features.Offers.GetOffers.Endpoint.Map(v1);
UpdateOfferEndpoint.Map(v1);
DeleteOfferEndpoint.Map(v1);

// Student endpoints
InternHub.Api.Features.Students.CreateStudent.Endpoint.Map(v1);
InternHub.Api.Features.Students.GetStudentById.Endpoint.Map(v1);
InternHub.Api.Features.Students.GetStudents.Endpoint.Map(v1);
UpdateStudentEndpoint.Map(v1);
DeleteStudentEndpoint.Map(v1);

app.MapGet("/", () => "Hello World!");

app.Run();

public partial class Program
{
}