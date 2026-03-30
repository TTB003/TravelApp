using TravelApp.Models.Contracts;

namespace TravelApp.Services.Abstractions;

public interface IAuthApiClient
{
    Task<AuthResultDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResultDto?> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResultDto?> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
}
