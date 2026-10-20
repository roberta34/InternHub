using InternHub.Domain.Students;
using InternHub.Features.Students.CreateStudent;
using InternHub.Features.Students.GetStudentById;
using InternHub.Infrastructure.Repositories;
using InternHub.Api.Features.Students.DeleteStudent;
using InternHub.Features.Students.GetStudents;
using InternHub.Features.Students.UpdateStudent;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Infrastructure.Repositories;
using InternHub.Api.Features.Offers.CreateOffer;
using InternHub.Api.Features.Offers.GetOfferById;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IOfferRepository, InMemoryOfferRepository>();


builder.Services.AddSingleton<IStudentRepository, InMemoryStudentRepository>();

var app = builder.Build();


CreateOfferEndpoint.Map(app);
GetOfferByIdEndpoint.Map(app);
app.MapGet("/", () => "Hello World!");
CreateStudentEndpoint.Map(app);
GetStudentByIdEndpoint.Map(app);
GetStudentsEndpoint.Map(app);
UpdateStudentEndpoint.Map(app);
DeleteStudentEndpoint.Map(app);

app.Run();

public partial class Program
{
}