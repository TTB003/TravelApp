namespace TravelApp.Models.Contracts;

public record LoginRequestDto(string Email, string Password);

public record RegisterRequestDto(string Email, string Password, string FullName);

public record RefreshTokenRequestDto(string RefreshToken);

public record AuthResultDto(
    string AccessToken,
    string? RefreshToken,
    DateTimeOffset? ExpiresAtUtc,
    string TokenType = "Bearer",
    string? UserId = null,
    IReadOnlyList<string>? Roles = null);
