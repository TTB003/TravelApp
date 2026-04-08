using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelApp.Domain.Entities;

namespace TravelApp.Infrastructure.Persistence.Configurations;

public class PoiConfiguration : IEntityTypeConfiguration<Poi>
{
    public void Configure(EntityTypeBuilder<Poi> builder)
    {
        builder.ToTable("POI");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Subtitle)
            .HasMaxLength(512);

        builder.Property(x => x.Description)
            .HasMaxLength(4000);

        builder.Property(x => x.Category)
            .HasMaxLength(128);

        builder.Property(x => x.Location)
            .HasMaxLength(512);

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(1024);

        builder.Property(x => x.Duration)
            .HasMaxLength(100);

        builder.Property(x => x.Provider)
            .HasMaxLength(256);

        builder.Property(x => x.Credit)
            .HasMaxLength(1024);

        builder.Property(x => x.SpeechText)
            .HasMaxLength(4000);

        builder.Property(x => x.PrimaryLanguage)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.GeofenceRadiusMeters)
            .HasDefaultValue(100d);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasMany(x => x.Localizations)
            .WithOne(x => x.Poi)
            .HasForeignKey(x => x.PoiId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.AudioAssets)
            .WithOne(x => x.Poi)
            .HasForeignKey(x => x.PoiId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
