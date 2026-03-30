using Microsoft.Maui.Devices.Sensors;
using Microsoft.Extensions.Logging;
using TravelApp.Models.Runtime;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public class MauiLocationProvider : ILocationProvider
{
    private readonly ILogger<MauiLocationProvider> _logger;

    public MauiLocationProvider(ILogger<MauiLocationProvider> logger)
    {
        _logger = logger;
    }

    public async Task<LocationSample?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var lastKnown = await Geolocation.Default.GetLastKnownLocationAsync();
            if (lastKnown is not null)
            {
                return new LocationSample(lastKnown.Latitude, lastKnown.Longitude, DateTimeOffset.UtcNow);
            }

            var locationRequest = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5));
            var location = await Geolocation.Default.GetLocationAsync(locationRequest, cancellationToken);
            if (location is null)
            {
                _logger.LogDebug("GPS: no location available from platform provider.");
                return null;
            }

            return new LocationSample(location.Latitude, location.Longitude, DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GPS: failed to fetch location sample.");
            return null;
        }
    }
}
