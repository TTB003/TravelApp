using TravelApp.Models.Contracts;

namespace TravelApp.Models.Runtime;

public sealed record AudioTriggerRequest(
    PoiDto Poi,
    LocationSample UserLocation,
    string LanguageCode,
    DateTimeOffset TriggeredAtUtc);
