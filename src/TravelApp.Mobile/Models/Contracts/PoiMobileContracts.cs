namespace TravelApp.Models.Contracts;

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

public class PoiAudioMobileDto
{
    public int Id { get; set; }
    public string LanguageCode { get; set; } = "en";
    public string? AudioUrl { get; set; }
    public string? Transcript { get; set; }
    public bool IsGenerated { get; set; }
}

public class PoiStoryDto
{
    public int Id { get; set; }
    public string LanguageCode { get; set; } = "en";
    public string Title { get; set; } = string.Empty;
    // Content holds original text; Description used for TTS-ready content on mobile
    public string Content { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
    public string Description { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}

public class PagedResultDto<T>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public IReadOnlyList<T> Items { get; set; } = [];
}
