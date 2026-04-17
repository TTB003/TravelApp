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

    public PoisController(ITravelAppApiClient apiClient)
    {
        _apiClient = apiClient;
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
        return View(poi);
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

        var baseUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host.Value;
        return View(ToEditorModel(poi, baseUrl));
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

        return RedirectToAction(nameof(Index));
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
        var qrContent = BuildPoiQrContent(poi.Id, baseUrl);
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
            SpeechText = model.SpeechText,
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
            SpeechTexts = model.SpeechTexts.Select(x => new UpsertPoiSpeechTextDto
            {
                LanguageCode = x.LanguageCode,
                Text = x.Text
            }).Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToList()
        };
    }

    private static string BuildPoiQrContent(int poiId, string baseUrl)
    {
        // Ensure no trailing slash
        if (baseUrl.EndsWith('/')) baseUrl = baseUrl.TrimEnd('/');
        return $"{baseUrl}/Public/Poi/{poiId}";
    }

    private static string BuildQrImageUrl(string qrContent)
    {
        return $"https://quickchart.io/qr?size=260&text={Uri.EscapeDataString(qrContent)}";
    }
    // No-op to ensure file is treated as changed/saved after signature updates
    private const string _noOpFileSaved = "";
}
