using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelApp.Application.Abstractions.Pois;
using TravelApp.Application.Dtos.Pois;

namespace TravelApp.Api.Controllers;

[ApiController]
[Route("api/pois")]
[AllowAnonymous]
public class PoisController : ControllerBase
{
    private readonly IPoiQueryService _poiQueryService;

    public PoisController(IPoiQueryService poiQueryService)
    {
        _poiQueryService = poiQueryService;
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

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertPoiRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _poiQueryService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id, lang = result.LanguageCode }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Return generic error for client but include message to help admin debugging
            return BadRequest(new { message = ex.Message });
        }
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
        try
        {
            var deleted = await _poiQueryService.DeleteAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
