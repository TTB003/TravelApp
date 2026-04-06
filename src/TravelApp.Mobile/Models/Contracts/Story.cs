namespace TravelApp.Models.Contracts;

public class Story
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = "en";
    public string? AudioUrl { get; set; }
    public string Description { get; set; } = string.Empty;
}
