using TravelApp.Models.Runtime;

namespace TravelApp.Services.Abstractions;

public interface ILocationTrackerService
{
    event EventHandler<LocationSample>? LocationChanged;

    LocationSample? CurrentLocation { get; }

    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
