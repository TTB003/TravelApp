using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelApp.Domain.Entities;

namespace TravelApp.Infrastructure.Persistence.Configurations;

public class TourConfiguration : IEntityTypeConfiguration<Tour>
{
    public void Configure(EntityTypeBuilder<Tour> builder)
    {
        builder.ToTable("Tours");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AnchorPoiId)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(x => x.CoverImageUrl)
            .HasMaxLength(1024);

        builder.Property(x => x.PrimaryLanguage)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.AnchorPoiId).IsUnique();

        builder.HasOne(x => x.AnchorPoi)
            .WithMany()
            .HasForeignKey(x => x.AnchorPoiId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new Tour
            {
                Id = 1,
                AnchorPoiId = 1,
                Name = "HCM Food Tour",
                Description = "Tour ẩm thực Sài Gòn với các điểm dừng được sắp xếp theo lộ trình thật.",
                CoverImageUrl = "https://placehold.co/1200x800/png?text=HCM+Food+Tour",
                PrimaryLanguage = "vi",
                IsPublished = true,
                CreatedAtUtc = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new Tour
            {
                Id = 2,
                AnchorPoiId = 4,
                Name = "Hanoi Food Tour",
                Description = "Tour ẩm thực Hà Nội với các mốc waypoint, bản đồ và audio tự động.",
                CoverImageUrl = "https://placehold.co/1200x800/png?text=Hanoi+Food+Tour",
                PrimaryLanguage = "vi",
                IsPublished = true,
                CreatedAtUtc = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
            });
    }
}
