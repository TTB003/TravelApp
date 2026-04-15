using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelApp.Domain.Entities;

namespace TravelApp.Infrastructure.Persistence.Configurations;

public class ShopImageConfiguration : IEntityTypeConfiguration<ShopImage>
{
    public void Configure(EntityTypeBuilder<ShopImage> builder)
    {
        builder.ToTable("ShopImages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName).HasMaxLength(512).IsRequired();
        builder.Property(x => x.Url).HasMaxLength(1024).IsRequired();

        builder.HasOne(x => x.Shop)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.ShopId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
