using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelApp.Admin.Web.Models;
using TravelApp.Admin.Web.Services;

namespace TravelApp.Admin.Web.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly ITravelAppApiClient _apiClient;
    private readonly IConfiguration _configuration;

    public AdminController(ITravelAppApiClient apiClient, IConfiguration configuration)
    {
        _apiClient = apiClient;
        _configuration = configuration;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        // 1. Lấy thông tin thống kê tổng quát (Số user, Tổng lượt nghe, Tổng lượt quét)
        // Giả sử API trả về object: { PoiCount, UserCount, PublishedTourCount, QrCount }
        var stats = await _apiClient.GetDashboardStatsAsync(cancellationToken);
        
        // 2. Lấy danh sách Top POI (đã được API sắp xếp theo độ hot)
        // Endpoint này trả về List<PoiStatResult> (Id, Title, Category, QrScans, AudioPlays)
        var topPois = await _apiClient.GetPoiStatsAsync(cancellationToken);

        var vm = new AdminDashboardViewModel
        {
            PoiCount = stats?.PoiCount ?? 0,
            UserCount = stats?.UserCount ?? 0,
            
            // Ở Dashboard, PublishedTourCount giờ đây đóng vai trò là "Tổng lượt nghe Audio"
            PublishedTourCount = stats?.PublishedTourCount ?? 0, 
            
            // QrCount đóng vai trò là "Tổng lượt quét QR"
            QrCount = stats?.QrCount ?? 0,
            ApiBaseUrl = _configuration["TravelAppApi:BaseUrl"] ?? string.Empty,

            // Map dữ liệu thống kê chi tiết vào RecentPois để hiển thị bảng xếp hạng
            RecentPois = topPois.Select(x => new DashboardPoiSummary
            {
                Id = x.Id,
                Title = x.Title,
                Category = x.Category,
                AudioPlays = x.AudioPlays,
                QrScans = x.QrScans
            }).ToList()
        };

        return View(vm);
    }
}
