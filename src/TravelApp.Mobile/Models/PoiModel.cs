namespace TravelApp.Models;

public class PoiModel
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Subtitle { get; set; }
    public required string ImageUrl { get; set; }
    public required string Location { get; set; }
    public required string Distance { get; set; }
    public required string Duration { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Description { get; set; }
    public string? Provider { get; set; }
    public string? Credit { get; set; }
    public string? SpeechText { get; set; }
    public string? QrImageUrl { get; set; }
}
