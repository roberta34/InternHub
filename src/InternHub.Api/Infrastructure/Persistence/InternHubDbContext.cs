using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Domain.Students;
using Microsoft.EntityFrameworkCore;

namespace InternHub.Api.Infrastructure.Persistence;

public sealed class InternHubDbContext(DbContextOptions<InternHubDbContext> options)
    : DbContext(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<InternshipCandidacy> InternshipCandidacies => Set<InternshipCandidacy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new StudentConfiguration());
        modelBuilder.ApplyConfiguration(new OfferConfiguration());
        modelBuilder.ApplyConfiguration(new InternshipCandidacyConfiguration());
    }
}
