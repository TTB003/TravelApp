namespace TravelApp.Domain.Entities;

public class PoiStory
{
    public int Id { get; set; }
    public int PoiId { get; set; }
    public string LanguageCode { get; set; } = "en";
    public string Title { get; set; } = string.Empty;
    // Content is legacy name; use Description for TTS content
    public string Content { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
    public string Description { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Poi Poi { get; set; } = null!;
}
