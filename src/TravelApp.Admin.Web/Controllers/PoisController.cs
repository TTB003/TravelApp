using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelApp.Admin.Web.Models.Pois;
using TravelApp.Admin.Web.Services;
using TravelApp.Application.Dtos.Pois;

namespace TravelApp.Admin.Web.Controllers;

[Authorize]
public class PoisController : Controller
{
    private readonly ITravelAppApiClient _apiClient;
    private readonly IConfiguration _configuration;

    public PoisController(ITravelAppApiClient apiClient, IConfiguration configuration)
    {
        _apiClient = apiClient;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpGet("admin")]
    public IActionResult AdminEntry()
    {
        // Chỉ kiểm tra đăng nhập của Admin (Scheme mặc định)
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Index));
        }
        return RedirectToAction("Login", "Auth", new { returnUrl = "/admin" });
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await _apiClient.GetPoisAsync("vi", cancellationToken);
        return View(model);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var poi = await _apiClient.GetPoiAsync(id, "vi", cancellationToken);
        if (poi is null)
        {
            return NotFound();
        }
        var publicWebBaseUrl = GetPublicWebBaseUrl();
        var model = ToEditorModel(poi, publicWebBaseUrl);
        return View(model);
    }

    [Authorize(Roles = "Owner,Admin,SuperAdmin")]
    public IActionResult Create()
    {
        return View(CreateEmptyModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Owner,Admin,SuperAdmin")]
    public async Task<IActionResult> Create(PoiEditorViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            EnsureMinimumRows(model);
            return View(model);
        }

        var request = ToRequest(model);
        try
        {
            var result = await _apiClient.CreatePoiAsync(request, cancellationToken);
            if (result is null || result.Id <= 0)
            {
                EnsureMinimumRows(model);
                ModelState.AddModelError(string.Empty, "Không thể tạo POI. Vui lòng kiểm tra kết nối API và thử lại.");
                return View(model);
            }

            return RedirectToAction(nameof(Edit), new { id = result.Id });
        }
        catch (InvalidOperationException ex)
        {
            EnsureMinimumRows(model);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (Exception)
        {
            EnsureMinimumRows(model);
            ModelState.AddModelError(string.Empty, "Không thể tạo POI do lỗi phía server. Vui lòng thử lại sau.");
            return View(model);
        }
    }

    [Authorize(Roles = "Owner,Admin,SuperAdmin")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var poi = await _apiClient.GetPoiAsync(id, "vi", cancellationToken);
        if (poi is null)
        {
            return NotFound();
        }

        // Xác định Host của Web UI để QR code trỏ về đúng trang detail công khai
        var publicWebBaseUrl = GetPublicWebBaseUrl();
        return View(ToEditorModel(poi, publicWebBaseUrl));
    }

    private string GetPublicWebBaseUrl()
    {
        var requestHost = HttpContext.Request.Host.Host;
        var requestPort = HttpContext.Request.Host.Port ?? _configuration.GetValue<int>("TravelAppWeb:Port", 7020);

        // Nếu đang chạy localhost, cố gắng lấy IP thật từ cấu hình API để điện thoại có thể quét được
        if (requestHost.Contains("localhost") || requestHost == "127.0.0.1")
        {
            var apiBase = _configuration["TravelAppApi:BaseUrl"] ?? "";
            var ip = apiBase.Replace("http://", "").Replace("https://", "").Split(':')[0].Split('/')[0].Trim();
            if (!string.IsNullOrEmpty(ip) && ip != "localhost" && ip != "127.0.0.1")
            {
                requestHost = ip;
            }
        }

        // Luôn dùng http cho mạng nội bộ để tránh lỗi SSL Certificate trên điện thoại
        return $"http://{requestHost}:{requestPort}";
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Owner,Admin,SuperAdmin")]
    public async Task<IActionResult> Edit(int id, PoiEditorViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            EnsureMinimumRows(model);
            return View(model);
        }

        var updated = await _apiClient.UpdatePoiAsync(id, ToRequest(model), cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

        // Sau khi lưu, quay lại trang Edit để xem QR Code mới sinh ra
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Owner,Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _apiClient.DeletePoiAsync(id, cancellationToken);
        if (!deleted)
        {
            TempData["ErrorMessage"] = "Không thể xóa POI này vì nó đang được dùng trong Tour.";
        }

        return RedirectToAction(nameof(Index));
    }

    private PoiEditorViewModel ToEditorModel(PoiMobileDto poi, string baseUrl)
    {
        var apiBaseUrl = _configuration["TravelAppApi:BaseUrl"]?.TrimEnd('/');
        var qrContent = BuildPoiQrContent(poi.Id, apiBaseUrl ?? string.Empty, baseUrl);
        var model = new PoiEditorViewModel
        {
            Id = poi.Id,
            Title = poi.Title,
            Subtitle = poi.Subtitle,
            Description = poi.Description,
            Category = poi.Category,
            Location = poi.Location,
            ImageUrl = poi.ImageUrl,
            Latitude = poi.Latitude,
            Longitude = poi.Longitude,
            GeofenceRadiusMeters = poi.GeofenceRadiusMeters,
            PrimaryLanguage = poi.PrimaryLanguage,
            SpeechText = poi.SpeechText,
            SpeechTextLanguageCode = poi.SpeechTextLanguageCode ?? poi.LanguageCode,
            Localizations = poi.Localizations.Count > 0
                ? poi.Localizations.Select(x => new PoiLocalizationEditorInput
                {
                    LanguageCode = x.LanguageCode,
                    Title = x.Title,
                    Subtitle = x.Subtitle,
                    Description = x.Description
                }).ToList()
                : [new() { LanguageCode = poi.LanguageCode, Title = poi.Title, Subtitle = poi.Subtitle, Description = poi.Description }],
            AudioAssets = poi.AudioAssets.Count > 0
                ? poi.AudioAssets.Select(x => new PoiAudioEditorInput
                {
                    LanguageCode = x.LanguageCode,
                    AudioUrl = x.AudioUrl,
                    Transcript = x.Transcript
                }).ToList()
                : [new()],
            SpeechTexts = poi.SpeechTexts.Count > 0
                ? poi.SpeechTexts.Select(x => new PoiSpeechTextEditorInput
                {
                    LanguageCode = x.LanguageCode,
                    Text = x.Text
                }).ToList()
                : [new()]
        };

        model.QrContent = qrContent;
        model.QrImageUrl = BuildQrImageUrl(qrContent);

        EnsureMinimumRows(model);
        return model;
    }

    private static PoiEditorViewModel CreateEmptyModel()
    {
        var model = new PoiEditorViewModel { PrimaryLanguage = "vi", SpeechTextLanguageCode = "vi" };
        EnsureMinimumRows(model);
        return model;
    }

    private static void EnsureMinimumRows(PoiEditorViewModel model)
    {
        while (model.Localizations.Count < 1)
        {
            model.Localizations.Add(new PoiLocalizationEditorInput());
        }

        while (model.AudioAssets.Count < 1)
        {
            model.AudioAssets.Add(new PoiAudioEditorInput());
        }

        while (model.SpeechTexts.Count < 1)
        {
            model.SpeechTexts.Add(new PoiSpeechTextEditorInput());
        }
    }

    private static UpsertPoiRequestDto ToRequest(PoiEditorViewModel model)
    {
        // Chuẩn bị danh sách SpeechTexts từ model
        var speechTexts = model.SpeechTexts.Select(x => new UpsertPoiSpeechTextDto
        {
            LanguageCode = x.LanguageCode,
            Text = x.Text
        }).Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToList();

        // Logic đồng bộ: Nếu trong danh sách có nội dung cho ngôn ngữ đang chọn làm chính,
        // chúng ta sẽ cập nhật trường SpeechText (singular) theo nội dung đó.
        var mainSpeechText = model.SpeechText;
        var matchingItem = speechTexts.FirstOrDefault(x => string.Equals(x.LanguageCode, model.SpeechTextLanguageCode, StringComparison.OrdinalIgnoreCase));
        if (matchingItem != null)
        {
            mainSpeechText = matchingItem.Text;
        }

        return new UpsertPoiRequestDto
        {
            Title = model.Title,
            Subtitle = model.Subtitle,
            Description = model.Description,
            Category = model.Category,
            Location = model.Location,
            ImageUrl = model.ImageUrl,
            Latitude = model.Latitude,
            Longitude = model.Longitude,
            GeofenceRadiusMeters = model.GeofenceRadiusMeters,
            PrimaryLanguage = model.PrimaryLanguage,
            SpeechText = mainSpeechText,
            SpeechTextLanguageCode = model.SpeechTextLanguageCode,
            Localizations = model.Localizations.Select(x => new UpsertPoiLocalizationDto
            {
                LanguageCode = x.LanguageCode,
                Title = x.Title,
                Subtitle = x.Subtitle,
                Description = x.Description
            }).Where(x => !string.IsNullOrWhiteSpace(x.Title)).ToList(),
            AudioAssets = model.AudioAssets.Select(x => new UpsertPoiAudioDto
            {
                LanguageCode = x.LanguageCode,
                AudioUrl = x.AudioUrl,
                Transcript = x.Transcript,
                IsGenerated = false
            }).Where(x => !string.IsNullOrWhiteSpace(x.AudioUrl) || !string.IsNullOrWhiteSpace(x.Transcript)).ToList(),
            SpeechTexts = speechTexts
        };
    }

    private static string BuildPoiQrContent(int poiId, string apiBaseUrl, string publicWebBaseUrl)
    {
        if (publicWebBaseUrl.EndsWith('/')) publicWebBaseUrl = publicWebBaseUrl.TrimEnd('/');
        
        // Tạo link redirect qua API để tính lượt quét
        var redirectUrl = $"{publicWebBaseUrl}/Public/Details/{poiId}";
        return $"{apiBaseUrl}/api/pois/{poiId}/qr-track?redirectUrl={Uri.EscapeDataString(redirectUrl)}";
    }

    private static string BuildQrImageUrl(string qrContent)
    {
        return $"https://quickchart.io/qr?size=260&text={Uri.EscapeDataString(qrContent)}";
    }
    // No-op to ensure file is treated as changed/saved after signature updates
    private const string _noOpFileSaved = "";
}
