using InternHub.Api.Features.Students.CreateStudent;
using InternHub.Api.Features.Students.GetStudentById;
using InternHub.Api.Features.Students.GetStudents;
using InternHub.Api.Features.Students.UpdateStudent;
using InternHub.Api.Features.Students.DeleteStudent;

namespace InternHub.Api.Features.Students;

public static class StudentFeaturesExtensions
{
    public static IServiceCollection AddStudentFeatures(this IServiceCollection services)
    {
        services.AddScoped<ICreateStudentHandler, CreateStudentHandler>();
        services.AddScoped<IGetStudentByIdHandler, GetStudentByIdHandler>();
        services.AddScoped<IGetStudentsHandler, GetStudentsHandler>();
        services.AddScoped<IUpdateStudentHandler, UpdateStudentHandler>();
        services.AddScoped<IDeleteStudentHandler, DeleteStudentHandler>();
        return services;
    }
}
