namespace TravelApp.Application.Dtos.Pois;

public class PoiMobileDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = "en";
    public string PrimaryLanguage { get; set; } = "en";
    public string ImageUrl { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? DistanceMeters { get; set; }
    public double GeofenceRadiusMeters { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<PoiAudioMobileDto> AudioAssets { get; set; } = [];
    public List<PoiStoryDto> Stories { get; set; } = new();
}

public class PoiQueryRequestDto
{
    public string? LanguageCode { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? RadiusMeters { get; set; }
}

public class PoiStoryDto
{
    public int Id { get; set; }
    public string LanguageCode { get; set; } = "en";
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}

public class PagedResultDto<T>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public IReadOnlyList<T> Items { get; set; } = [];
}

public class PoiAudioMobileDto
{
    public int Id { get; set; }
    public string LanguageCode { get; set; } = "en";
    public string? AudioUrl { get; set; }
    public string? Transcript { get; set; }
    public bool IsGenerated { get; set; }
}

public class UpsertPoiRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Location { get; set; }
    public string? ImageUrl { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double GeofenceRadiusMeters { get; set; } = 100;
    public string PrimaryLanguage { get; set; } = "en";
    public List<UpsertPoiLocalizationDto> Localizations { get; set; } = [];
    public List<UpsertPoiAudioDto> AudioAssets { get; set; } = [];
}

public class UpsertPoiLocalizationDto
{
    public string LanguageCode { get; set; } = "en";
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
}

public class UpsertPoiAudioDto
{
    public string LanguageCode { get; set; } = "en";
    public string? AudioUrl { get; set; }
    public string? Transcript { get; set; }
    public bool IsGenerated { get; set; }
}
