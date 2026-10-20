using InternHub.Domain.Students;
using InternHub.Features.Students.CreateStudent;
using InternHub.Features.Students.GetStudentById;
using InternHub.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IStudentRepository, InMemoryStudentRepository>();

var app = builder.Build();

CreateStudentEndpoint.Map(app);
GetStudentByIdEndpoint.Map(app);

app.Run();

public partial class Program { }
