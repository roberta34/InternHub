using InternHub.Domain.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InternHub.Api.Infrastructure.Persistence;

public sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(s => s.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.HasIndex(s => s.Email)
            .IsUnique();

        builder.HasMany(s => s.InternshipCandidacies)
            .WithOne()
            .HasForeignKey(c => c.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.InternshipCandidacies)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
