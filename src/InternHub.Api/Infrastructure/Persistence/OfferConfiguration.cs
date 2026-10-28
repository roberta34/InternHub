using InternHub.Api.Domain.Offers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InternHub.Api.Infrastructure.Persistence;

public sealed class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(o => o.Title)
            .IsRequired();

        builder.Property(o => o.CompanyName)
            .IsRequired();

        builder.Property(o => o.Requirements)
            .IsRequired();

        builder.Property(o => o.Deadline)
            .IsRequired();

        builder.Property(o => o.MaximumAvailableSlots)
            .IsRequired();

        builder.Property(o => o.IsClosed)
            .IsRequired();

        builder.Property(o => o.Version)
            .IsConcurrencyToken();

        builder.HasMany(o => o.InternshipCandidacies)
            .WithOne()
            .HasForeignKey(c => c.OfferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.InternshipCandidacies)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
