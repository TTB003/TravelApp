namespace TravelApp.Domain.Entities;

public class Poi
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Location { get; set; }
    public string? ImageUrl { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double GeofenceRadiusMeters { get; set; } = 100;
    public string PrimaryLanguage { get; set; } = "en";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public ICollection<PoiLocalization> Localizations { get; set; } = new List<PoiLocalization>();
    public ICollection<PoiAudio> AudioAssets { get; set; } = new List<PoiAudio>();
    public ICollection<PoiStory> Stories { get; set; } = new List<PoiStory>();
}
