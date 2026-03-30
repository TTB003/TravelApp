using System.Net.Http.Json;
using TravelApp.Models.Contracts;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Api;

public class ProfileApiClient : ApiClientBase, IProfileApiClient
{
    public ProfileApiClient(IHttpClientFactory httpClientFactory, ApiClientOptions options, ITokenStore tokenStore)
        : base(httpClientFactory, options, tokenStore)
    {
    }

    public async Task<ProfileDto?> GetMyProfileAsync(CancellationToken cancellationToken = default)
    {
        var client = CreateClient(authorized: true);
        var response = await client.GetAsync("api/profile/me", cancellationToken);
        return await ReadAsAsync<ProfileDto>(response, cancellationToken);
    }

    public async Task<bool> UpdateMyProfileAsync(UpdateProfileRequestDto request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(authorized: true);
        var response = await client.PutAsJsonAsync("api/profile/me", request, JsonOptions, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
