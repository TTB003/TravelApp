namespace TravelApp.Models.Contracts;

public class Story
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = "en";
}
