using TravelApp.Models.Contracts;

namespace TravelApp.Models.Runtime;

public enum GeofenceTransitionType
{
    Enter,
    Exit
}

public sealed record GeofenceTransitionEvent(
    PoiDto Poi,
    GeofenceTransitionType Transition,
    LocationSample UserLocation,
    double DistanceMeters,
    DateTimeOffset OccurredAtUtc);
