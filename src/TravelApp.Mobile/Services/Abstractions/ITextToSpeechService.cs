using System.Threading;

namespace TravelApp.Services.Abstractions;

public interface ITextToSpeechService
{
    Task PlayTextAsync(string text, string languageCode, CancellationToken cancellationToken = default);
}
