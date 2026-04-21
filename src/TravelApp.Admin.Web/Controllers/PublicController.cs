using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelApp.Admin.Web.Services;
using TravelApp.Application.Dtos.Pois;

namespace TravelApp.Admin.Web.Controllers;

[AllowAnonymous] // Quan trọng: Cho phép truy cập trang login mà không cần đăng nhập trước
public class PublicController : Controller
{
    private readonly ITravelAppApiClient _apiClient;

    public PublicController(ITravelAppApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public IActionResult Index()
    {
        return RedirectToAction("Explore");
    }

    public async Task<IActionResult> Explore(CancellationToken cancellationToken)
    {
        var currentLang = Request.Cookies["SelectedLanguage"] ?? "vi";
        var pois = await _apiClient.GetPoisAsync(currentLang, cancellationToken);
        return View(pois);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var currentLang = Request.Cookies["SelectedLanguage"] ?? "vi";
        var poi = await _apiClient.GetPoiAsync(id, currentLang, cancellationToken);
        if (poi == null) 
        {
            // Nếu không thấy POI, quay lại trang Explore thay vì hiện 404
            return RedirectToAction("Explore");
        }
        return View(poi);
    }

    public async Task<IActionResult> TourMap(int tourId, int? poiId, CancellationToken cancellationToken)
    {
        var currentLang = Request.Cookies["SelectedLanguage"] ?? "vi";
        var tour = await _apiClient.GetTourAsync(tourId, cancellationToken);
        
        // Lấy danh sách POI đầy đủ để hiển thị tọa độ trên bản đồ Leaflet
        var allPois = await _apiClient.GetPoisAsync(currentLang, cancellationToken);
        List<PoiMobileDto> tourPois = new();
        
        if (tour != null && tour.Pois != null && tour.Pois.Any())
        {
            // TRƯỜNG HỢP 1: Có Tour chính thức được định nghĩa trong Admin
            ViewBag.TourName = tour.Name;
            var tourPoiIds = tour.Pois.OrderBy(x => x.SortOrder).Select(x => x.PoiId).ToList();
            tourPois = allPois.Where(p => tourPoiIds.Contains(p.Id))
                              .OrderBy(p => tourPoiIds.IndexOf(p.Id))
                              .ToList();
        }
        else
        {
            // TRƯỜNG HỢP 2: Không có Tour chính thức, nhóm các POI theo Category (Food Tour)
            var fallbackPoi = allPois?.FirstOrDefault(p => p.Id == tourId);
            if (fallbackPoi != null && !string.IsNullOrEmpty(fallbackPoi.Category))
            {
                ViewBag.TourName = fallbackPoi.Category;
                // Lấy tất cả các POI thuộc cùng danh mục (ví dụ: "Ho Chi Minh Food Tour")
                tourPois = allPois.Where(p => p.Category == fallbackPoi.Category).ToList();
            }
            else if (fallbackPoi != null)
            {
                ViewBag.TourName = fallbackPoi.Title;
                tourPois = new List<PoiMobileDto> { fallbackPoi };
            }
        }

        return View(tourPois);
    }

    public IActionResult Login()
    {
        return View(); // Trả về Views/Public/Login.cshtml
    }

    public IActionResult Profile()
    {
        return View(); // Trả về Views/Public/Profile.cshtml
    }

    [HttpPost]
    public IActionResult SetLanguage(string lang)
    {
        Response.Cookies.Append("SelectedLanguage", lang, new Microsoft.AspNetCore.Http.CookieOptions 
        { Expires = DateTimeOffset.UtcNow.AddYears(1), Path = "/" });
        return Ok();
    }
}