using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;
using TravelApp.Application.Dtos.Tours;
using TravelApp.Admin.Web.Models.Tours;
using TravelApp.Admin.Web.Services;

namespace TravelApp.Admin.Web.Controllers;

[Authorize]
public class ToursController : Controller
{
    private readonly ITravelAppApiClient _apiClient;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ToursController(ITravelAppApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await _apiClient.GetToursAsync(cancellationToken);
        return View(model);
    }

    [Authorize(Roles = "Owner,Admin,SuperAdmin")]
    public async Task<IActionResult> Heatmap(CancellationToken cancellationToken)
    {
        // 1. Lấy danh sách POIs để có tọa độ
        var pois = await _apiClient.GetPoisAsync("vi", cancellationToken);
        
        // 2. Lấy số liệu thống kê (Xếp hạng địa điểm phổ biến) từ Dashboard API
        var stats = await _apiClient.GetPoiStatsAsync(cancellationToken);

        // 3. Gộp dữ liệu: Gán VisitCount cho từng POI dựa trên số lượt Audio + QR
        var heatmapData = pois.Select(p => {
            var stat = stats.FirstOrDefault(s => s.Id == p.Id);
            var visitCount = (stat?.AudioPlays ?? 0) + (stat?.QrScans ?? 0);
            return new { p.Id, p.Title, p.Latitude, p.Longitude, VisitCount = (double)visitCount };
        }).ToList();

        return View(heatmapData);
    }

    [Authorize(Roles = "Owner,Admin,SuperAdmin")]
    public async Task<IActionResult> Create(int? anchorPoiId, CancellationToken cancellationToken)
    {
        var model = await BuildEditorModelAsync((TourAdminDto?)null, anchorPoiId, cancellationToken);
        return View(model);
    }

    [Authorize(Roles = "Owner,Admin,SuperAdmin")]
    public async Task<IActionResult> AttachPoi(int poiId, CancellationToken cancellationToken)
    {
        var model = await BuildAttachPoiModelAsync(poiId, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Owner,Admin,SuperAdmin")]
    public async Task<IActionResult> AttachPoi(AttachPoiToToursViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model = await BuildAttachPoiModelAsync(model.PoiId, cancellationToken, model.SelectedTourIds);
            return View(model);
        }

        var selectedTourIds = model.SelectedTourIds.Where(x => x > 0).Distinct().ToHashSet();
        var anyFailure = false;
        foreach (var tourId in selectedTourIds)
        {
            var tour = await _apiClient.GetTourAsync(tourId, cancellationToken);
            if (tour is null)
            {
                continue;
            }

            if (tour.Pois.Any(x => x.PoiId == model.PoiId))
            {
                continue;
            }

            var nextSortOrder = tour.Pois.Count == 0 ? 1 : tour.Pois.Max(x => x.SortOrder) + 1;
            tour.Pois.Add(new TourPoiAdminDto
            {
                PoiId = model.PoiId,
                PoiTitle = model.PoiTitle,
                SortOrder = nextSortOrder,
                DistanceFromPreviousMeters = 0
            });

            var request = new UpsertTourRequestDto
            {
                AnchorPoiId = tour.AnchorPoiId,
                Name = tour.Name,
                Title = tour.Title,
                Subtitle = tour.Subtitle,
                Description = tour.Description,
                Location = tour.Location,
                Latitude = tour.Latitude,
                Longitude = tour.Longitude,
                Category = tour.Category,
                ImageUrl = tour.ImageUrl,
                CoverImageUrl = tour.CoverImageUrl,
                PrimaryLanguage = tour.PrimaryLanguage,
                IsPublished = tour.IsPublished,
                Pois = tour.Pois.Select(x => new TourPoiRequestDto
                {
                    PoiId = x.PoiId,
                    SortOrder = x.SortOrder,
                    DistanceFromPreviousMeters = x.DistanceFromPreviousMeters
                }).OrderBy(x => x.SortOrder).ToList(),
                AudioAssets = tour.AudioAssets,
                SpeechTexts = tour.SpeechTexts
            };

            var success = await _apiClient.UpdateTourAsync(tourId, request, cancellationToken);
            if (!success)
            {
                anyFailure = true;
                TempData["ErrorMessage"] = $"Không thể cập nhật tour #{tourId}.";
            }
        }

        if (!anyFailure)
        {
            TempData["SuccessMessage"] = "Đã thêm POI vào các tour đã chọn.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Owner,Admin,SuperAdmin")]
    public async Task<IActionResult> Create(TourEditorViewModel model, CancellationToken cancellationToken)
    {
        model = await BuildTourEditorModelFromFormAsync(model, cancellationToken);
        ValidateMinimumPois(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var request = ToRequest(model);
        var created = await _apiClient.CreateTourAsync(request, cancellationToken);
        if (created is null || created.Id <= 0)
        {
            ModelState.AddModelError(string.Empty, "Không thể tạo tour. Vui lòng kiểm tra kết nối API và thử lại.");
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Owner,Admin,SuperAdmin")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var existing = await _apiClient.GetTourAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        var model = await BuildEditorModelAsync(existing, null, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Owner,Admin,SuperAdmin")]
    public async Task<IActionResult> Edit(int id, TourEditorViewModel model, CancellationToken cancellationToken)
    {
        model = await BuildTourEditorModelFromFormAsync(model, cancellationToken);
        ValidateMinimumPois(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var request = ToRequest(model);
        var updated = await _apiClient.UpdateTourAsync(id, request, cancellationToken);
        if (!updated)
        {
            ModelState.AddModelError(string.Empty, "Không thể cập nhật tour. Vui lòng thử lại.");
            return View(model);
        }

        return RedirectToAction(nameof(Index)); 
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Owner,Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _apiClient.DeleteTourAsync(id, cancellationToken);
        if (!deleted)
        {
            TempData["ErrorMessage"] = "Không thể xóa tour.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<TourEditorViewModel> BuildEditorModelAsync(TourAdminDto? source, int? preferredAnchorPoiId, CancellationToken cancellationToken)
    {
        var pois = await _apiClient.GetPoisAsync("vi", cancellationToken);
        var availablePois = new List<SelectListItem>
        {
            new("-- Tạo POI mới --", "0")
        };
        availablePois.AddRange(pois.Select(x => new SelectListItem(x.Title, x.Id.ToString())));

        var resolvedAnchorPoiId = source?.AnchorPoiId
            ?? (preferredAnchorPoiId.HasValue && preferredAnchorPoiId.Value > 0 ? preferredAnchorPoiId.Value : pois.FirstOrDefault()?.Id ?? 0);
        var anchorPoi = pois.FirstOrDefault(x => x.Id == resolvedAnchorPoiId) ?? pois.FirstOrDefault();

        var model = new TourEditorViewModel
        {
            Id = source?.Id,
            Name = source?.Name ?? string.Empty,
            Title = source?.Title ?? string.Empty,
            Subtitle = source?.Subtitle,
            Description = source?.Description ?? string.Empty,
            CoverImageUrl = source?.CoverImageUrl,
            PrimaryLanguage = source?.PrimaryLanguage ?? "vi",
            IsPublished = source?.IsPublished ?? true,
            AnchorPoiId = resolvedAnchorPoiId,
            Location = source?.Location,
            Latitude = source?.Latitude,
            Longitude = source?.Longitude,
            Category = source?.Category,
            ImageUrl = source?.ImageUrl,
            AvailablePois = availablePois,
            Pois = source?.Pois.Select(x => new TourPoiEditorInput
            {
                PoiId = x.PoiId,
                SortOrder = x.SortOrder,
                DistanceFromPreviousMeters = x.DistanceFromPreviousMeters
            }).ToList() ?? [new(), new(), new()],
            AudioAssets = source?.AudioAssets.Select(x => new TourAudioEditorInput
            {
                LanguageCode = x.LanguageCode,
                AudioUrl = x.AudioUrl,
                Transcript = x.Transcript
            }).ToList() ?? [new(), new(), new()],
            SpeechTexts = source?.SpeechTexts.Select(x => new TourSpeechTextEditorInput
            {
                LanguageCode = x.LanguageCode,
                Text = x.Text
            }).ToList() ?? [new(), new(), new()]
        };

        ApplyAnchorPoiDetails(model, anchorPoi);
        model.AnchorPoiDetailsJson = JsonSerializer.Serialize(pois.Select(x => new
        {
            x.Id,
            x.Title,
            x.Subtitle,
            x.Description,
            x.Location,
            x.Latitude,
            x.Longitude,
            x.Category,
            x.ImageUrl
        }), JsonOptions);

        EnsureMinimumRows(model);
        return model;
    }

    private async Task<TourEditorViewModel> BuildTourEditorModelFromFormAsync(TourEditorViewModel? source, CancellationToken cancellationToken)
    {
        var pois = await _apiClient.GetPoisAsync("vi", cancellationToken);
        var availablePois = new List<SelectListItem>
        {
            new("-- Tạo POI mới --", "0")
        };
        availablePois.AddRange(pois.Select(x => new SelectListItem(x.Title, x.Id.ToString())));

        var model = source ?? new TourEditorViewModel();
        model.AvailablePois = availablePois;

        var anchorPoi = pois.FirstOrDefault(x => x.Id == model.AnchorPoiId) ?? pois.FirstOrDefault();
        ApplyAnchorPoiDetails(model, anchorPoi);
        model.AnchorPoiDetailsJson = JsonSerializer.Serialize(pois.Select(x => new
        {
            x.Id,
            x.Title,
            x.Subtitle,
            x.Description,
            x.Location,
            x.Latitude,
            x.Longitude,
            x.Category,
            x.ImageUrl
        }), JsonOptions);

        EnsureMinimumRows(model);
        return model;
    }

    private static void EnsureMinimumRows(TourEditorViewModel model)
    {
        while (model.Pois.Count < 1)
        {
            model.Pois.Add(new TourPoiEditorInput());
        }

        while (model.AudioAssets.Count < 3)
        {
            model.AudioAssets.Add(new TourAudioEditorInput());
        }

        while (model.SpeechTexts.Count < 3)
        {
            model.SpeechTexts.Add(new TourSpeechTextEditorInput());
        }
    }

    private void ValidateMinimumPois(TourEditorViewModel model)
    {
        var validPoiCount = model.Pois.Count(x => x.PoiId > 0);
        if (validPoiCount < 2)
        {
            ModelState.AddModelError(nameof(model.Pois), "Tour phải có tối thiểu 2 POI.");
        }
    }

    private async Task<AttachPoiToToursViewModel> BuildAttachPoiModelAsync(int poiId, CancellationToken cancellationToken, IEnumerable<int>? selectedTourIds = null)
    {
        var poi = await _apiClient.GetPoiAsync(poiId, "vi", cancellationToken);
        if (poi is null)
        {
            throw new InvalidOperationException("POI not found.");
        }

        var tours = await _apiClient.GetToursAsync(cancellationToken);
        var selected = selectedTourIds?.Where(x => x > 0).ToHashSet() ?? [];

        return new AttachPoiToToursViewModel
        {
            PoiId = poi.Id,
            PoiTitle = poi.Title,
            PoiSubtitle = poi.Subtitle,
            PoiLocation = poi.Location,
            AvailableTours = tours
                .OrderByDescending(x => x.IsPublished)
                .ThenBy(x => x.Id)
                .Select(x => new AttachPoiTourItemViewModel
                {
                    TourId = x.Id,
                    TourName = x.Name,
                    TourDescription = x.Description,
                    IsPublished = x.IsPublished,
                    PoiCount = x.Pois.Count,
                    IsSelected = selected.Contains(x.Id)
                })
                .ToList()
        };
    }

    private static void ApplyAnchorPoiDetails(TourEditorViewModel model, TravelApp.Application.Dtos.Pois.PoiMobileDto? anchorPoi)
    {
        if (anchorPoi is null)
        {
            return;
        }

        model.AnchorPoiId = anchorPoi.Id;
        model.Title = anchorPoi.Title;
        model.Subtitle = anchorPoi.Subtitle;
        model.Description = anchorPoi.Description ?? string.Empty;
        model.Location = anchorPoi.Location;
        model.Latitude = anchorPoi.Latitude;
        model.Longitude = anchorPoi.Longitude;
        model.Category = anchorPoi.Category;
        model.ImageUrl = anchorPoi.ImageUrl;
        model.CoverImageUrl = string.IsNullOrWhiteSpace(anchorPoi.ImageUrl) ? model.CoverImageUrl : anchorPoi.ImageUrl;
    }

    private static UpsertTourRequestDto ToRequest(TourEditorViewModel model)
    {
        return new UpsertTourRequestDto
        {
            AnchorPoiId = model.AnchorPoiId,
            Name = model.Name,
            Title = model.Title,
            Subtitle = model.Subtitle,
            Description = model.Description,
            Location = model.Location,
            Latitude = model.Latitude ?? 0,
            Longitude = model.Longitude ?? 0,
            Category = model.Category,
            ImageUrl = model.ImageUrl,
            CoverImageUrl = model.CoverImageUrl,
            PrimaryLanguage = model.PrimaryLanguage,
            IsPublished = model.IsPublished,
            Pois = model.Pois.Select(x => new TourPoiRequestDto
            {
                PoiId = x.PoiId,
                SortOrder = x.SortOrder,
                DistanceFromPreviousMeters = x.DistanceFromPreviousMeters
            }).Where(x => x.PoiId > 0).ToList(),
            AudioAssets = model.AudioAssets.Select(x => new TourAudioAssetDto
            {
                LanguageCode = x.LanguageCode,
                AudioUrl = x.AudioUrl,
                Transcript = x.Transcript,
                IsGenerated = false
            }).ToList(),
            SpeechTexts = model.SpeechTexts.Select(x => new TourSpeechTextDto
            {
                LanguageCode = x.LanguageCode,
                Text = x.Text
            }).Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToList()
        };
    }
}
