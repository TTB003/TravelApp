using Microsoft.AspNetCore.Mvc;
using TravelApp.Application.Abstractions.Pois;
using TravelApp.Application.Dtos.Pois;

namespace TravelApp.Api.Controllers;

[ApiController]
[Route("api/pois")]
public class PoisController : ControllerBase
{
    private readonly IPoiQueryService _poiQueryService;
    private readonly TravelApp.Application.Abstractions.ITranslationService _translationService;

    public PoisController(IPoiQueryService poiQueryService, TravelApp.Application.Abstractions.ITranslationService translationService)
    {
        _poiQueryService = poiQueryService;
        _translationService = translationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery(Name = "lang")] string? languageCode,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery(Name = "lat")] double? latitude = null,
        [FromQuery(Name = "lng")] double? longitude = null,
        [FromQuery(Name = "radius")] double? radiusMeters = null,
        CancellationToken cancellationToken = default)
    {
        var query = new PoiQueryRequestDto
        {
            LanguageCode = languageCode,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Latitude = latitude,
            Longitude = longitude,
            RadiusMeters = radiusMeters
        };

        var result = await _poiQueryService.GetAllAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, [FromQuery(Name = "lang")] string? languageCode, CancellationToken cancellationToken)
    {
        var result = await _poiQueryService.GetByIdAsync(id, languageCode, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        // If client requested a language (lang) and it's not Vietnamese, translate Title and Description on-the-fly
        var target = string.IsNullOrWhiteSpace(languageCode) ? null : languageCode.Trim().ToLowerInvariant();
        try
        {
            if (!string.IsNullOrWhiteSpace(target) && !string.Equals(target, "vi", StringComparison.OrdinalIgnoreCase))
            {
                // translate Title and Description from Vietnamese to target language if present
                if (!string.IsNullOrWhiteSpace(result.Title))
                {
                    var t = await _translationService.TranslateTextAsync(result.Title, target, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(t)) result.Title = t;
                }

                if (!string.IsNullOrWhiteSpace(result.Description))
                {
                    var t = await _translationService.TranslateTextAsync(result.Description, target, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(t)) result.Description = t;
                }

                // Also translate stories descriptions and titles on-the-fly
                if (result.Stories is not null)
                {
                    foreach (var s in result.Stories)
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(s.Title))
                            {
                                var tt = await _translationService.TranslateTextAsync(s.Title, target, cancellationToken);
                                if (!string.IsNullOrWhiteSpace(tt)) s.Title = tt;
                            }

                            if (!string.IsNullOrWhiteSpace(s.Description))
                            {
                                var tt = await _translationService.TranslateTextAsync(s.Description, target, cancellationToken);
                                if (!string.IsNullOrWhiteSpace(tt)) s.Description = tt;
                            }
                        }
                        catch
                        {
                            // swallow per-story translation errors and continue
                        }
                    }
                }
            }
        }
        catch
        {
            // translation failed — return original Vietnamese data as fallback
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertPoiRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _poiQueryService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id, lang = result.LanguageCode }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertPoiRequestDto request, CancellationToken cancellationToken)
    {
        var updated = await _poiQueryService.UpdateAsync(id, request, cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _poiQueryService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
