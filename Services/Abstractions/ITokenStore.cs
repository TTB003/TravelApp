namespace TravelApp.Services.Abstractions;

public interface ITokenStore
{
    string? AccessToken { get; set; }
    string? RefreshToken { get; set; }
    DateTimeOffset? ExpiresAtUtc { get; set; }
    string TokenType { get; set; }
}
