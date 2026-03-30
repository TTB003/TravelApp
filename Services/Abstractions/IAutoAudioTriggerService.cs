using TravelApp.Models.Runtime;

namespace TravelApp.Services.Abstractions;

public interface IAutoAudioTriggerService
{
    event EventHandler<AudioTriggerRequest>? AudioTriggerRequested;
}
