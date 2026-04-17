using System.Threading;
using System.Threading.Tasks;
using TravelApp.Application.Dtos.Pois;

namespace TravelApp.Admin.Web.Services;

public interface IPoiApiService
{
    Task<PoiMobileDto?> GetPoiAsync(int id, string? language = "vi", CancellationToken cancellationToken = default);
}
