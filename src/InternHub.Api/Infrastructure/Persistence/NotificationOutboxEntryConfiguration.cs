using InternHub.Api.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InternHub.Api.Infrastructure.Persistence;

public sealed class NotificationOutboxEntryConfiguration
    : IEntityTypeConfiguration<NotificationOutboxEntry>
{
    public void Configure(EntityTypeBuilder<NotificationOutboxEntry> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(n => n.CandidacyId)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(n => n.OfferId)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(n => n.StudentId)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(n => n.Kind)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(n => n.OccurredAtUtc)
            .HasConversion(
                value => value,
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
            .IsRequired();

        builder.HasIndex(n => new { n.CandidacyId, n.Kind })
            .IsUnique();

        builder.HasIndex(n => new { n.OfferId, n.OccurredAtUtc });
    }
}
