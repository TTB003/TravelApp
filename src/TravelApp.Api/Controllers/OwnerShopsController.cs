using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TravelApp.Application.Abstractions.Shops;
using TravelApp.Application.Dtos.Shops;

namespace TravelApp.Api.Controllers;

[ApiController]
[Route("api/owner/shops")]
public class OwnerShopsController : ControllerBase
{
    private readonly IShopService _shopService;

    public OwnerShopsController(IShopService shopService)
    {
        _shopService = shopService;
    }

    [Authorize(Roles = "Owner")]
    [HttpPost]
    public async Task<IActionResult> CreateShop([FromBody] CreateShopRequestDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var ownerId))
            return Unauthorized();

        var shop = await _shopService.CreateShopAsync(ownerId, request);
        return CreatedAtAction(nameof(GetMine), new { id = shop.Id }, shop);
    }
    [Authorize(Roles = "Owner")]
    [HttpGet("me")]
    public async Task<IActionResult> GetMine()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var ownerId))
            return Unauthorized();

        var shop = await _shopService.GetByOwnerAsync(ownerId);
        return shop is null ? NotFound() : Ok(shop);
    }

    // Public endpoints to browse shops without authentication
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var shops = await _shopService.GetAllAsync();
        return Ok(shops);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var shop = await _shopService.GetByIdAsync(id);
        return shop is null ? NotFound() : Ok(shop);
    }
}
