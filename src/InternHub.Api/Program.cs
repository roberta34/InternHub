using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.Shared;
using InternHub.Api.Features.Students;
using InternHub.Api.Features.Offers;
using InternHub.Api.Features.Students.GetStudentById;
using InternHub.Api.Features.Students.GetStudents;
using InternHub.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddStudentFeatures();
builder.Services.AddOfferFeatures();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
    db.Database.EnsureCreated();
}

var v1 = app.MapV1();

// Offer endpoints
InternHub.Api.Features.Offers.CreateOffer.Endpoint.Map(v1);
InternHub.Api.Features.Offers.GetOfferById.Endpoint.Map(v1);
InternHub.Api.Features.Offers.GetOffers.Endpoint.Map(v1);
InternHub.Api.Features.Offers.UpdateOffer.Endpoint.Map(v1);
InternHub.Api.Features.Offers.DeleteOffer.Endpoint.Map(v1);

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