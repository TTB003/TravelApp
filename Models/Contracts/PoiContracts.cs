namespace TravelApp.Models.Contracts;

public record PoiLocalizationDto(string LanguageCode, string Title, string? Subtitle, string? Description);

public record PoiAudioDto(string LanguageCode, string? AudioUrl, string? Transcript, bool IsGenerated = false);

public class PoiDto
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string Subtitle { get; set; } = string.Empty;
    public required string ImageUrl { get; set; }
    public required string Location { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? GeofenceRadiusMeters { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public string Price { get; set; } = string.Empty;
    public string Distance { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Provider { get; set; }
    public string? Credit { get; set; }
    public string? Category { get; set; }
    public string? PrimaryLanguage { get; set; }
    public IReadOnlyList<PoiLocalizationDto> Localizations { get; set; } = [];
    public IReadOnlyList<PoiAudioDto> AudioAssets { get; set; } = [];
}

public record NearbyPoiQueryDto(double Latitude, double Longitude, double RadiusMeters = 500);

public record UpsertPoiRequestDto(
    string Title,
    string? Subtitle,
    string ImageUrl,
    string Location,
    double Latitude,
    double Longitude,
    double? GeofenceRadiusMeters,
    string? Description,
    string? Category,
    string? PrimaryLanguage,
    IReadOnlyList<PoiLocalizationDto>? Localizations,
    IReadOnlyList<PoiAudioDto>? AudioAssets);
