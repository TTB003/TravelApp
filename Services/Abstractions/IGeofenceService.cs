using TravelApp.Models.Contracts;
using TravelApp.Models.Runtime;

namespace TravelApp.Services.Abstractions;

public interface IGeofenceService
{
    event EventHandler<GeofenceTransitionEvent>? TransitionOccurred;
    event EventHandler<GeofenceTransitionEvent>? Entered;
    event EventHandler<GeofenceTransitionEvent>? Exited;

    void SetPois(IEnumerable<PoiDto> pois);
}
