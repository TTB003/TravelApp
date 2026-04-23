using TravelApp.Application.Dtos.Pois;
using TravelApp.Application.Dtos.Users;
using TravelApp.Application.Dtos.Tours;
using TravelApp.Admin.Web.Models;

namespace TravelApp.Admin.Web.Services;

public interface ITravelAppApiClient
{
    // No-op patch: trigger update (interface unchanged)
    Task<IReadOnlyList<UserAdminDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<UserAdminDto?> GetUserAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoleAdminDto>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<UserAdminDto?> CreateUserAsync(UpsertUserRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> UpdateUserAsync(Guid id, UpsertUserRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PoiMobileDto>> GetPoisAsync(string? languageCode = null, CancellationToken cancellationToken = default);
    Task<PoiMobileDto?> GetPoiAsync(int id, string? languageCode = null, CancellationToken cancellationToken = default);
    Task<PoiMobileDto> CreatePoiAsync(UpsertPoiRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> UpdatePoiAsync(int id, UpsertPoiRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeletePoiAsync(int id, CancellationToken cancellationToken = default);

    Task<TravelApp.Application.Dtos.Metrics.MetricsOverviewDto?> GetMetricsOverviewAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TravelApp.Application.Dtos.Metrics.EventAdminDto>> GetRecentEventsAsync(int limit = 50, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TourAdminDto>> GetToursAsync(CancellationToken cancellationToken = default);
    Task<TourAdminDto?> GetTourAsync(int id, CancellationToken cancellationToken = default);
    Task<TourAdminDto> CreateTourAsync(UpsertTourRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> UpdateTourAsync(int id, UpsertTourRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteTourAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<object>> GetTopPoisAsync(int limit = 10, CancellationToken cancellationToken = default);
    
    Task<DashboardStatsDto?> GetDashboardStatsAsync(CancellationToken cancellationToken = default);
    Task<List<PoiStatDto>> GetPoiStatsAsync(CancellationToken cancellationToken = default);
    Task<List<OnlineUserDisplayDto>> GetActiveUsersAsync(CancellationToken ct);
}
