using System.Net.Http.Json;
using TravelApp.Models.Contracts;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Api;

public class AuthApiClient : ApiClientBase, IAuthApiClient
{
    private readonly ITokenStore _tokenStore;

    public AuthApiClient(IHttpClientFactory httpClientFactory, ApiClientOptions options, ITokenStore tokenStore)
        : base(httpClientFactory, options, tokenStore)
    {
        _tokenStore = tokenStore;
    }

    public async Task<AuthResultDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("api/auth/login", request, JsonOptions, cancellationToken);
        var result = await ReadAsAsync<AuthResultDto>(response, cancellationToken);

        PersistToken(result);

        return result;
    }

    public async Task<AuthResultDto?> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("api/auth/register", request, JsonOptions, cancellationToken);
        var result = await ReadAsAsync<AuthResultDto>(response, cancellationToken);

        PersistToken(result);

        return result;
    }

    public async Task<AuthResultDto?> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("api/auth/refresh", request, JsonOptions, cancellationToken);
        var result = await ReadAsAsync<AuthResultDto>(response, cancellationToken);

        PersistToken(result);

        return result;
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        _tokenStore.AccessToken = null;
        _tokenStore.RefreshToken = null;
        _tokenStore.ExpiresAtUtc = null;
        _tokenStore.TokenType = "Bearer";
        return Task.CompletedTask;
    }

    private void PersistToken(AuthResultDto? result)
    {
        if (result is null)
            return;

        _tokenStore.AccessToken = result.AccessToken;
        _tokenStore.RefreshToken = result.RefreshToken;
        _tokenStore.ExpiresAtUtc = result.ExpiresAtUtc;
        _tokenStore.TokenType = string.IsNullOrWhiteSpace(result.TokenType) ? "Bearer" : result.TokenType;
    }
}
