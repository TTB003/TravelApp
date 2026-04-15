using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TravelApp.Admin.Web.Services;
using TravelApp.Application.Dtos.Pois;

namespace TravelApp.Admin.Web.Controllers;

[AllowAnonymous]
public class PublicController : Controller
{
    private readonly ITravelAppApiClient _apiClient;
    private readonly TravelAppApiOptions _options;

    public PublicController(ITravelAppApiClient apiClient, IOptions<TravelAppApiOptions> options)
    {
        _apiClient = apiClient;
        _options = options.Value;
    }

    [HttpGet]
    [Route("public/poi/detail/{id:int}")]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        // Fetch POI server-side to avoid client CORS/SSL issues and to render instantly
        PoiMobileDto? poi = null;
        try
        {
            poi = await _apiClient.GetPoiAsync(id, "vi", cancellationToken);
        }
        catch
        {
            poi = null;
        }

        ViewData["ApiBaseUrl"] = _options.BaseUrl?.TrimEnd('/');
        return View("~/Views/Public/Detail.cshtml", poi as object);
    }
}
