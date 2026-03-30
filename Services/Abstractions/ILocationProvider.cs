using TravelApp.Models.Runtime;

namespace TravelApp.Services.Abstractions;

public interface ILocationProvider
{
    Task<LocationSample?> GetCurrentLocationAsync(CancellationToken cancellationToken = default);
}
