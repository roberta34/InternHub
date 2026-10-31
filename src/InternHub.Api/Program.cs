using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.Shared;
using InternHub.Api.Features.Students;
using InternHub.Api.Features.Offers;
using InternHub.Api.Features.Students.GetStudentById;
using InternHub.Api.Features.Students.GetStudents;
using InternHub.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Discover Sprint 3 modules once; each module needs a public parameterless constructor.
var featureModules = typeof(IFeatureModule).Assembly
    .GetTypes()
    .Where(type => type.IsClass
                   && !type.IsAbstract
                   && typeof(IFeatureModule).IsAssignableFrom(type))
    .OrderBy(type => type.FullName, StringComparer.Ordinal)
    .Select(type => (IFeatureModule)(Activator.CreateInstance(type)
        ?? throw new InvalidOperationException($"Cannot create feature module {type.FullName}.")))
    .ToArray();

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddStudentFeatures();
builder.Services.AddOfferFeatures();

// New modules register their handlers before the application is built.
foreach (var module in featureModules)
{
    module.RegisterServices(builder.Services);
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
    db.Database.EnsureCreated();
}

var v1 = app.MapV1();

// Existing Offer endpoints remain mapped exactly once.
InternHub.Api.Features.Offers.CreateOffer.Endpoint.Map(v1);
InternHub.Api.Features.Offers.GetOfferById.Endpoint.Map(v1);
InternHub.Api.Features.Offers.GetOffers.Endpoint.Map(v1);
InternHub.Api.Features.Offers.UpdateOffer.Endpoint.Map(v1);
InternHub.Api.Features.Offers.DeleteOffer.Endpoint.Map(v1);

// Existing Student endpoints remain mapped exactly once.
InternHub.Api.Features.Students.CreateStudent.Endpoint.Map(v1);
InternHub.Api.Features.Students.GetStudentById.Endpoint.Map(v1);
InternHub.Api.Features.Students.GetStudents.Endpoint.Map(v1);
InternHub.Api.Features.Students.UpdateStudent.Endpoint.Map(v1);
InternHub.Api.Features.Students.DeleteStudent.Endpoint.Map(v1);

// Only the endpoints belonging to new modules are mapped here.
foreach (var module in featureModules)
{
    module.MapEndpoints(v1);
}

app.MapGet("/", () => "Hello World!");

app.Run();

public partial class Program
{
}
