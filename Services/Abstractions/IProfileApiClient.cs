using TravelApp.Models.Contracts;

namespace TravelApp.Services.Abstractions;

public interface IProfileApiClient
{
    Task<ProfileDto?> GetMyProfileAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateMyProfileAsync(UpdateProfileRequestDto request, CancellationToken cancellationToken = default);
}
