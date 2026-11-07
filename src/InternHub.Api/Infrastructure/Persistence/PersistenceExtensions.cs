using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.AtomicSubmission;
using InternHub.Domain.Students;
using Microsoft.EntityFrameworkCore;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Infrastructure.Persistence.AtomicSubmission;

namespace InternHub.Api.Infrastructure.Persistence;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("InternHub")
            ?? "Data Source=internhub.db";

        services.AddDbContext<InternHubDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IStudentRepository, EfCoreStudentRepository>();
        services.AddScoped<IOfferRepository, EfCoreOfferRepository>();
        services.AddScoped<IInternshipCandidacyRepository, EfCoreInternshipCandidacyRepository>();
        services.AddScoped<IAtomicCandidacySubmission, EfCoreAtomicCandidacySubmission>();
        return services;
    }
}
