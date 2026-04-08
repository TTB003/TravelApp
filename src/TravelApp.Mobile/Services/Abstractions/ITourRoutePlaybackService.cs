using TravelApp.Models.Contracts;
using TravelApp.Models.Runtime;

namespace TravelApp.Services.Abstractions;

public interface ITourRoutePlaybackService
{
    event EventHandler<TourRoutePlaybackChangedEventArgs>? ActiveWaypointChanged;

    TourRouteDto? CurrentRoute { get; }
    TourRouteWaypointDto? CurrentWaypoint { get; }

    Task StartAsync(TourRouteDto route, int? preferredPoiId = null, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task SelectWaypointAsync(int poiId, CancellationToken cancellationToken = default);
}
