using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelApp.Domain.Entities;

namespace TravelApp.Infrastructure.Persistence.Configurations;

public class PoiStoryConfiguration : IEntityTypeConfiguration<PoiStory>
{
    public void Configure(EntityTypeBuilder<PoiStory> builder)
    {
        builder.ToTable("PoiStory");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.PoiId);
        builder.Property(x => x.LanguageCode).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.Content).IsRequired();
    }
}
