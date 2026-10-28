using InternHub.Api.Features.Students.CreateStudent;
using InternHub.Api.Features.Students.GetStudentById;
using InternHub.Api.Features.Students.GetStudents;

namespace InternHub.Api.Features.Students;

public static class StudentFeaturesExtensions
{
    public static IServiceCollection AddStudentFeatures(this IServiceCollection services)
    {
        services.AddScoped<ICreateStudentHandler, CreateStudentHandler>();
        services.AddScoped<IGetStudentByIdHandler, GetStudentByIdHandler>();
        services.AddScoped<IGetStudentsHandler, GetStudentsHandler>();
        return services;
    }
}
