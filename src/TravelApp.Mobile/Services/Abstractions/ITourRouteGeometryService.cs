using Microsoft.Maui.Devices.Sensors;
using TravelApp.Models.Contracts;
using TravelApp.Models.Runtime;

namespace TravelApp.Services.Abstractions;

public interface ITourRouteGeometryService
{
    Task<RouteGeometryResult> GetRoadPathAsync(TourRouteDto route, string? languageCode = null, CancellationToken cancellationToken = default);
}
