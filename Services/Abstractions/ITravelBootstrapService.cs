namespace TravelApp.Services.Abstractions;

public interface ITravelBootstrapService
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
