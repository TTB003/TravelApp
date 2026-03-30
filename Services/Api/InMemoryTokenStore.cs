using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Api;

public class InMemoryTokenStore : ITokenStore
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public string TokenType { get; set; } = "Bearer";
}
