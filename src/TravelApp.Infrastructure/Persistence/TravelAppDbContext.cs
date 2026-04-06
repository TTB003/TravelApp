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
    public DbSet<PoiStory> PoiStories => Set<PoiStory>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TravelAppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
