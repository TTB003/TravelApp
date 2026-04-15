using Microsoft.EntityFrameworkCore;
using TravelApp.Application.Abstractions.Persistence;
using TravelApp.Application.Abstractions.Shops;
using TravelApp.Application.Dtos.Shops;
using TravelApp.Domain.Entities;

namespace TravelApp.Infrastructure.Services.Shops;

public class ShopService : IShopService
{
    private readonly ITravelAppDbContext _dbContext;

    public ShopService(ITravelAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ShopDto> CreateShopAsync(Guid ownerId, CreateShopRequestDto request, CancellationToken cancellationToken = default)
    {
        var shop = new Shop
        {
            OwnerId = ownerId,
            Address = request.Address,
            Description = request.Description,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.Shops.Add(shop);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ShopDto
        {
            Id = shop.Id,
            OwnerId = shop.OwnerId,
            Address = shop.Address,
            Description = shop.Description,
            CreatedAtUtc = shop.CreatedAtUtc,
            Images = shop.Images.Select(i => new ShopImageDto { Id = i.Id, FileName = i.FileName, Url = i.Url }).ToList()
        };
    }

    public async Task<ShopDto?> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        var shop = await _dbContext.Shops
            .AsNoTracking()
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.OwnerId == ownerId, cancellationToken);

        if (shop is null)
            return null;

        return new ShopDto
        {
            Id = shop.Id,
            OwnerId = shop.OwnerId,
            Address = shop.Address,
            Description = shop.Description,
            CreatedAtUtc = shop.CreatedAtUtc,
            Images = shop.Images.Select(i => new ShopImageDto { Id = i.Id, FileName = i.FileName, Url = i.Url }).ToList()
        };
    }
}
