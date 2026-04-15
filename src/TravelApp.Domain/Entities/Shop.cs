namespace TravelApp.Domain.Entities;

public class Shop
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public User? Owner { get; set; }
    public ICollection<ShopImage> Images { get; set; } = new List<ShopImage>();
}
