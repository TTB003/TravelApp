using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelApp.Domain.Entities;

namespace TravelApp.Infrastructure.Persistence.Configurations;

public class PoiStoryConfiguration : IEntityTypeConfiguration<PoiStory>
{
    public void Configure(EntityTypeBuilder<PoiStory> builder)
    {
        // Use plural table name to match DbSet naming and migrations
        builder.ToTable("PoiStories");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.PoiId);
        builder.Property(x => x.LanguageCode).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Title).IsRequired();
        // Description may be long text; explicitly map to nvarchar(max)
        builder.Property(x => x.Description).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(x => x.AudioUrl).HasMaxLength(1024);

        // Configure relationship to Poi
        builder.HasOne(x => x.Poi)
            .WithMany(p => p.Stories)
            .HasForeignKey(x => x.PoiId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
