namespace TravelApp.Models;

public class PoiModel
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Subtitle { get; set; }
    public required string ImageUrl { get; set; }
    public required string Location { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public required string Price { get; set; }
    public required string Distance { get; set; }
    public required string Duration { get; set; }
    public string? Description { get; set; }
    public string? Provider { get; set; }
    public string? Credit { get; set; }
}
