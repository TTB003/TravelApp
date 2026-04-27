using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
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
        
        // Lấy danh sách Tour để hiển thị ở trang Explore
        ViewBag.Tours = await _apiClient.GetToursAsync(cancellationToken);

        // Lấy dữ liệu Top 5 POI thực tế từ API Metrics (giống Admin)
        ViewBag.TopPois = await _apiClient.GetPoiStatsAsync(cancellationToken);

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

    [HttpGet]
    public IActionResult Login()
    {
        return View(); // Trả về Views/Public/Login.cshtml
    }

    [HttpPost]
    public async Task<IActionResult> Login(string UserName, string Password, bool RememberMe)
    {
        if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(Password)) return BadRequest();

        // 1. GỌI API ĐỂ LẤY TOKEN THỰC TẾ
        var accessToken = await _apiClient.LoginAsync(UserName, Password);
        if (string.IsNullOrEmpty(accessToken))
        {
            return Unauthorized();
        }

        // Tạo danh tính cho người dùng Web
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, UserName),
            // Quan trọng: API thường nhìn vào ClaimTypes.NameIdentifier hoặc Name để xác định User
            new(ClaimTypes.NameIdentifier, UserName), 
            new(ClaimTypes.Email, UserName), 
            new(ClaimTypes.Role, UserName.Contains("owner") ? "Owner" : "User")
        };

        var identity = new ClaimsIdentity(claims, "PublicAuthScheme");
        var principal = new ClaimsPrincipal(identity);

        // ĐĂNG NHẬP VÀO SCHEME RIÊNG CỦA WEB
        await HttpContext.SignInAsync("PublicAuthScheme", principal, new AuthenticationProperties 
        { 
            IsPersistent = RememberMe 
        });

        // 2. TRẢ VỀ JSON CHỨA TOKEN THẬT
        return Ok(new { token = accessToken });
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("PublicAuthScheme");
        return RedirectToAction("Explore");
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