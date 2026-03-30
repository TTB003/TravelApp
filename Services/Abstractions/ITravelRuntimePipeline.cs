using TravelApp.Models.Contracts;

namespace TravelApp.Services.Abstractions;

public interface ITravelRuntimePipeline
{
    Task StartAsync(IEnumerable<PoiDto> pois, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
