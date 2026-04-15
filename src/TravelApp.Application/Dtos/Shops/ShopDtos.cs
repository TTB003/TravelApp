namespace TravelApp.Application.Dtos.Shops;

public sealed class CreateShopRequestDto
{
    public string Address { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class ShopDto
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public IReadOnlyList<ShopImageDto> Images { get; set; } = [];
}

public sealed class ShopImageDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
