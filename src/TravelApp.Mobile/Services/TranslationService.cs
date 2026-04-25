using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace TravelApp.Mobile.Services;

public interface ITranslationService
{
    Task<string> TranslateAsync(string text, string targetLanguage, string sourceLanguage = "auto");
}

public class TranslationService : ITranslationService
{
    private readonly HttpClient _httpClient;
    private const string ApiUrl = "https://libretranslate.de/translate"; // Hoặc IP server LibreTranslate của bạn
    private readonly string _apiUrl;
    public TranslationService(HttpClient httpClient, AppConfig config)
    {
        _httpClient = httpClient;
        
        // Sử dụng AdminHost từ config để đồng bộ với server cục bộ
        var host = config.AdminHost?.TrimEnd('/') ?? "192.168.100.164";
        _apiUrl = $"{host}:5000/translate";
    }

    public async Task<string> TranslateAsync(string text, string targetLanguage, string sourceLanguage = "auto")
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        
        // Android Emulator cần trỏ về 10.0.2.2 nếu bạn chạy Docker trên localhost
        string finalUrl = DeviceInfo.Platform == DevicePlatform.Android && _apiUrl.Contains("localhost") 
            ? _apiUrl.Replace("localhost", "10.0.2.2") 
            : _apiUrl;

        try
        {
            // Chuẩn hóa mã ngôn ngữ
            var target = targetLanguage.Split('-')[0];
            var source = sourceLanguage.Split('-')[0];

            var response = await _httpClient.PostAsJsonAsync(finalUrl, new
            {
                q = text,
                source = source,
                target = target,
                format = "text"
            });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TranslationResponse>();
                return result?.TranslatedText ?? text;
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Translation Error: {ex.Message}"); }

        return text; // Trả về text gốc nếu lỗi
    }

    private class TranslationResponse
    {
        [JsonPropertyName("translatedText")]
        public string TranslatedText { get; set; }
    }
}