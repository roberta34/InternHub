using InternHub.Api.Domain.InternshipCandidacies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InternHub.Api.Infrastructure.Persistence;

public sealed class InternshipCandidacyConfiguration
    : IEntityTypeConfiguration<InternshipCandidacy>
{
    public void Configure(EntityTypeBuilder<InternshipCandidacy> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(c => c.StudentId)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(c => c.OfferId)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(c => c.ApplicationDate)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.HasIndex(c => new { c.StudentId, c.OfferId })
            .IsUnique()
            .HasFilter(
                "[Status] IN ('Submitted', 'Shortlisted', 'Accepted', 'Waitlisted')");
    }
}
