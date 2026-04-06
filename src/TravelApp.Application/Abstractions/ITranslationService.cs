using System.Threading;

namespace TravelApp.Application.Abstractions;

public interface ITranslationService
{
    Task<string?> TranslateTextAsync(string text, string targetLanguage, CancellationToken cancellationToken = default);
}
