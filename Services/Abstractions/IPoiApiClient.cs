using TravelApp.Models.Contracts;

namespace TravelApp.Services.Abstractions;

public interface IPoiApiClient
{
    Task<IReadOnlyList<PoiDto>> GetAllAsync(string? languageCode = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PoiDto>> GetNearbyAsync(NearbyPoiQueryDto query, string? languageCode = null, CancellationToken cancellationToken = default);
    Task<PoiDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PoiDto?> CreateAsync(UpsertPoiRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, UpsertPoiRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
