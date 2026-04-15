using Microsoft.EntityFrameworkCore;
using TravelApp.Application.Abstractions.Persistence;
using TravelApp.Domain.Entities;

namespace TravelApp.Infrastructure.Persistence;

public class TravelAppDbContext : DbContext, ITravelAppDbContext
{
    public TravelAppDbContext(DbContextOptions<TravelAppDbContext> options) : base(options)
    {
    }

    public DbSet<Poi> Pois => Set<Poi>();
    public DbSet<PoiLocalization> PoiLocalizations => Set<PoiLocalization>();
    public DbSet<PoiAudio> PoiAudios => Set<PoiAudio>();
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<TourPoi> TourPois => Set<TourPoi>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<ShopImage> ShopImages => Set<ShopImage>();
    public DbSet<PoiEvent> PoiEvents => Set<PoiEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TravelAppDbContext).Assembly);
        // Apply explicit configuration for Shop entities if needed
        base.OnModelCreating(modelBuilder);
    }
}
