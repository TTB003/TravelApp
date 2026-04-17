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

    [Route("")]
    [Route("Public")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var pois = await _apiClient.GetPoisAsync("vi", cancellationToken);
        ViewData["ApiBaseUrl"] = _options.BaseUrl?.TrimEnd('/');
        return View(pois);
    }

    [HttpGet]
    public IActionResult Scanner()
    {
        // Hiển thị giao diện quét mã QR giống QrScannerPage.cs trên Mobile
        return View();
    }

    [HttpGet]
    [Route("Public/Poi/{id:int}")]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        PoiMobileDto? poi = null;
        try
        {
            poi = await _apiClient.GetPoiAsync(id, "vi", cancellationToken);
        }
        catch
        {
            poi = null;
        }

        // Đảm bảo API Port 5001 được truyền xuống để phát Audio
        var apiBaseUrl = _options.BaseUrl?.TrimEnd('/') ?? "http://localhost:5001";
        ViewData["ApiBaseUrl"] = apiBaseUrl;
        return View("~/Views/Public/Detail.cshtml", poi as object);
    }
}
