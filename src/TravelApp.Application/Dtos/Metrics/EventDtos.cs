using System.Text.Json;

namespace TravelApp.Application.Dtos.Metrics;

public enum PoiEventTypeDto
{
    PoiView = 0,
    PoiPlay = 1,
    TourView = 2,
    TourPlay = 3,
    QrScan = 4
}

public record IngestEventRequestDto
(
    PoiEventTypeDto EventType,
    int? PoiId,
    int? TourId,
    Guid? UserId,
    object? Metadata
);

public record EventAdminDto
(
    int Id,
    PoiEventTypeDto EventType,
    int? PoiId,
    int? TourId,
    Guid? UserId,
    string? MetadataJson,
    DateTimeOffset CreatedAtUtc
);

public record MetricsOverviewDto
(
    long TotalPoiViews,
    long TotalPoiPlays,
    long TotalTourViews,
    long TotalTourPlays,
    long TotalQrScans
);
