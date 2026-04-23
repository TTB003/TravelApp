namespace TravelApp.Admin.Web.Models;

/// <summary>
/// DTO dùng để hiển thị thông tin người dùng trực tuyến trên Dashboard.
/// </summary>
public class OnlineUserDisplayDto
{
    public string Name { get; set; } = string.Empty;
    public string ClientType { get; set; } = string.Empty; // Ví dụ: "Mobile", "Web"
}