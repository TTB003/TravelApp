using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Audio;
using Microsoft.Maui.Media;
using System.Linq;
using Microsoft.Maui.Media;
using Microsoft.Extensions.Logging;

namespace TravelApp.Mobile.Services.Runtime;

public class TtsPlaybackService
{
    private readonly ILogger<TtsPlaybackService> _logger;

    public TtsPlaybackService(ILogger<TtsPlaybackService> logger)
    {
        _logger = logger;
    }

    // Play the given text using the appropriate locale for the language code.
    public async Task SpeakAsync(string text, string languageCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        try
        {
            Locale? locale = null;
            try
            {
                var locales = await TextToSpeech.Default.GetLocalesAsync();
                // Try exact match on language code
                locale = locales.FirstOrDefault(l => string.Equals(l.Language, languageCode, StringComparison.OrdinalIgnoreCase))
                         ?? locales.FirstOrDefault(l => l.Language.StartsWith(languageCode, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                // ignore locale retrieval errors and fall back to default
            }

            var options = new SpeechOptions();
            if (locale is not null)
            {
                options.Locale = locale;
            }

            await TextToSpeech.Default.SpeakAsync(text, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TTS playback failed");
        }
    }
}
