using System.Text.Json;

namespace TravelApp.Domain.Entities;

public enum PoiEventType
{
    PoiView = 0,
    PoiPlay = 1,
    TourView = 2,
    TourPlay = 3,
    QrScan = 4
}

public class PoiEvent
{
    public int Id { get; set; }
    public PoiEventType EventType { get; set; }
    // optional references
    public int? PoiId { get; set; }
    public int? TourId { get; set; }
    public Guid? UserId { get; set; }

    // metadata as JSON for extensibility (e.g., device, language)
    public string? MetadataJson { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public void SetMetadata(object? metadata)
    {
        MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata);
    }
}
