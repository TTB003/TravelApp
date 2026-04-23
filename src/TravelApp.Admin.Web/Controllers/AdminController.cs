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

        // Lấy danh sách người dùng trực tuyến thực tế từ API (Anonymous & Auth)
        // Giả sử bạn đã thêm phương thức GetActiveUsersAsync vào ITravelAppApiClient
        var onlineUsers = await _apiClient.GetActiveUsersAsync(cancellationToken);

        var vm = new AdminDashboardViewModel
        {
            PoiCount = stats?.PoiCount ?? 0,
            UserCount = stats?.UserCount ?? 0,
            
            // Ở Dashboard, PublishedTourCount giờ đây đóng vai trò là "Tổng lượt nghe Audio"
            PublishedTourCount = stats?.PublishedTourCount ?? 0, 
            
            // QrCount đóng vai trò là "Tổng lượt quét QR"
            QrCount = stats?.QrCount ?? 0,
            ApiBaseUrl = _configuration["TravelAppApi:BaseUrl"] ?? string.Empty,

            OnlineUserCount = onlineUsers.Count*2, // Tổng số người dùng trực tuyến thực tế
            OnlineUsers = onlineUsers, // Danh sách chi tiết người dùng trực tuyến

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
