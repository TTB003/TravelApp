using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelApp.Application.Dtos.Metrics;
using TravelApp.Application.Abstractions.Persistence;
using TravelApp.Domain.Entities;

namespace TravelApp.Api.Controllers;

[ApiController]
[Route("api/metrics")]
[AllowAnonymous]
public class MetricsController : ControllerBase
{
    private readonly ITravelAppDbContext _db;

    public MetricsController(ITravelAppDbContext db)
    {
        _db = db;
    }

    [HttpPost("events")]
    public async Task<IActionResult> IngestEvent([FromBody] IngestEventRequestDto request, CancellationToken cancellationToken)
    {
        var ev = new PoiEvent
        {
            EventType = Enum.Parse<PoiEventType>(request.EventType.ToString()),
            PoiId = request.PoiId,
            TourId = request.TourId,
            UserId = request.UserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        ev.SetMetadata(request.Metadata);

        _db.PoiEvents.Add(ev);
        await _db.SaveChangesAsync(cancellationToken);

        return Accepted(new { id = ev.Id });
    }

    [HttpGet("admin/overview")]
    [Authorize(Roles = "Owner,Admin,SuperAdmin")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var q = _db.PoiEvents.AsNoTracking();

        var totalPoiViews = await q.CountAsync(x => x.EventType == PoiEventType.PoiView, cancellationToken);
        var totalPoiPlays = await q.CountAsync(x => x.EventType == PoiEventType.PoiPlay, cancellationToken);
        var totalTourViews = await q.CountAsync(x => x.EventType == PoiEventType.TourView, cancellationToken);
        var totalTourPlays = await q.CountAsync(x => x.EventType == PoiEventType.TourPlay, cancellationToken);
        var totalQrScans = await q.CountAsync(x => x.EventType == PoiEventType.QrScan, cancellationToken);

        var overview = new MetricsOverviewDto(totalPoiViews, totalPoiPlays, totalTourViews, totalTourPlays, totalQrScans);
        return Ok(overview);
    }

    [HttpGet("admin/top-pois")]
    [Authorize(Roles = "Owner,Admin,SuperAdmin")]
    public async Task<IActionResult> GetTopPois(int limit = 10, CancellationToken cancellationToken = default)
    {
        var top = await _db.PoiEvents.AsNoTracking()
            .Where(x => x.EventType == PoiEventType.PoiPlay)
            .GroupBy(x => x.PoiId)
            .Where(g => g.Key.HasValue)
            .Select(g => new { PoiId = g.Key.Value, Count = g.LongCount() })
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return Ok(top);
    }

    [HttpGet("admin/recent")]
    [Authorize(Roles = "Owner,Admin,SuperAdmin")]
    public async Task<IActionResult> GetRecent(int limit = 50, CancellationToken cancellationToken = default)
    {
        var items = await _db.PoiEvents.AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(limit)
            .Select(x => new EventAdminDto(x.Id, (PoiEventTypeDto)x.EventType, x.PoiId, x.TourId, x.UserId, x.MetadataJson, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }
}
