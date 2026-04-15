using TravelApp.Application.Dtos.Shops;

namespace TravelApp.Application.Abstractions.Shops;

public interface IShopService
{
    Task<ShopDto> CreateShopAsync(Guid ownerId, CreateShopRequestDto request, CancellationToken cancellationToken = default);
    Task<ShopDto?> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);
}
