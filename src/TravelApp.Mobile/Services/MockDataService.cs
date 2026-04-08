using TravelApp.Models;

namespace TravelApp.Services;

public class MockDataService
{
    private const string HcmTourImageUrl = "https://placehold.co/1200x800/png?text=HCM+Food+Tour";
    private const string HanoiTourImageUrl = "https://placehold.co/1200x800/png?text=Hanoi+Food+Tour";
    private const string BenThanhImageUrl = "https://placehold.co/800x600/png?text=Ben+Thanh+Market";
    private const string PhoVinhKhanhImageUrl = "https://placehold.co/800x600/png?text=Pho+Vinh+Khanh";
    private const string BachDangImageUrl = "https://placehold.co/800x600/png?text=Bach+Dang+Wharf";
    private const string ChuaMotCotImageUrl = "https://placehold.co/800x600/png?text=One+Pillar+Pagoda";
    private const string HangXanhImageUrl = "https://placehold.co/800x600/png?text=Hang+Xanh+Street";
    private const string HangDauImageUrl = "https://placehold.co/800x600/png?text=Hang+Dau+Street";

    /// <summary>
    /// HCM Food Tour - Starting point and first waypoint
    /// </summary>
    public static List<PoiModel> GetForYouData()
    {
        return new List<PoiModel>
        {
            new PoiModel
            {
                Id = 1,
                Title = "Chợ Bến Thành",
                Subtitle = "Food Tour HCM - Starting Point",
                ImageUrl = BenThanhImageUrl,
                Location = "Chợ Bến Thành, Quận 1, TPHCM",
                Distance = "0 km",
                Duration = "45 min",
                Provider = "TravelApp",
                Description = "Điểm khởi đầu của tour ẩm thực HCM. Chợ Bến Thành là một trong những chợ truyền thống nổi tiếng nhất Sài Gòn với đa dạng hàng hóa và đặc biệt là các quán ăn địa phương.",
                SpeechText = "Điểm khởi đầu của tour ẩm thực HCM. Chợ Bến Thành là một trong những chợ truyền thống nổi tiếng nhất Sài Gòn với đa dạng hàng hóa và đặc biệt là các quán ăn địa phương.",
                Credit = "TravelApp placeholder"
            },
            new PoiModel
            {
                Id = 2,
                Title = "Phở Vĩnh Khánh",
                Subtitle = "Food Tour HCM - Pho Experience",
                ImageUrl = PhoVinhKhanhImageUrl,
                Location = "Phố Vĩnh Khánh, Quận 4, TPHCM",
                Distance = "0.9 km",
                Duration = "30 min",
                Provider = "TravelApp",
                Description = "Quán phở nổi tiếng với nước dùng được ninh từ 12h, phục vụ phở bò ngon nhất Quận 4.",
                SpeechText = "Quán phở nổi tiếng với nước dùng được ninh từ 12h, phục vụ phở bò ngon nhất Quận 4.",
                Credit = "TravelApp placeholder"
            },
            new PoiModel
            {
                Id = 3,
                Title = "Bến Bạch Đằng",
                Subtitle = "Food Tour HCM - Ending Point",
                ImageUrl = BachDangImageUrl,
                Location = "Bến Bạch Đằng, Quận 1, TPHCM",
                Distance = "1.8 km",
                Duration = "30 min",
                Provider = "TravelApp",
                Description = "Kết thúc tour tại bến Bạch Đằng. Thưởng thức các đặc sản Sài Gòn và tận hưởng không khí ven sông.",
                SpeechText = "Kết thúc tour tại bến Bạch Đằng. Thưởng thức các đặc sản Sài Gòn và tận hưởng không khí ven sông.",
                Credit = "TravelApp placeholder"
            }
        };
    }

    /// <summary>
    /// Hanoi Food Tour - Starting point and first waypoint
    /// </summary>
    public static List<PoiModel> GetEditorsChoiceData()
    {
        return new List<PoiModel>
        {
            new PoiModel
            {
                Id = 4,
                Title = "Chùa Một Cột",
                Subtitle = "Food Tour Hanoi - Starting Point",
                ImageUrl = ChuaMotCotImageUrl,
                Location = "Chùa Một Cột, Quận Ba Đình, Hà Nội",
                Distance = "0 km",
                Duration = "45 min",
                Provider = "TravelApp",
                Description = "Điểm khởi đầu của tour ẩm thực Hà Nội. Chùa Một Cột là một di tích lịch sử quan trọng, nằm gần khu phố cổ Hà Nội.",
                SpeechText = "Điểm khởi đầu của tour ẩm thực Hà Nội. Chùa Một Cột là một di tích lịch sử quan trọng, nằm gần khu phố cổ Hà Nội.",
                Credit = "TravelApp placeholder"
            },
            new PoiModel
            {
                Id = 5,
                Title = "Phố Hàng Xanh",
                Subtitle = "Food Tour Hanoi - Local Cuisine",
                ImageUrl = HangXanhImageUrl,
                Location = "Phố Hàng Xanh, Quận Hoàn Kiếm, Hà Nội",
                Distance = "0.3 km",
                Duration = "45 min",
                Provider = "TravelApp",
                Description = "Phố Hàng Xanh là một trong những phố cổ nổi tiếng của Hà Nội với các quán ăn truyền thống.",
                SpeechText = "Phố Hàng Xanh là một trong những phố cổ nổi tiếng của Hà Nội với các quán ăn truyền thống.",
                Credit = "TravelApp placeholder"
            },
            new PoiModel
            {
                Id = 6,
                Title = "Phố Hàng Dâu",
                Subtitle = "Food Tour Hanoi - Ending Point",
                ImageUrl = HangDauImageUrl,
                Location = "Phố Hàng Dâu, Quận Hoàn Kiếm, Hà Nội",
                Distance = "0.8 km",
                Duration = "30 min",
                Provider = "TravelApp",
                Description = "Kết thúc tour tại phố Hàng Dâu. Nơi đây nổi tiếng với các cửa hàng bán lụa truyền thống và các quán ăn địa phương.",
                SpeechText = "Kết thúc tour tại phố Hàng Dâu. Nơi đây nổi tiếng với các cửa hàng bán lụa truyền thống và các quán ăn địa phương.",
                Credit = "TravelApp placeholder"
            }
        };
    }

    public static List<PoiModel> GetAllTourData()
    {
        return GetForYouData().Concat(GetEditorsChoiceData()).ToList();
    }

    public static PoiModel? GetById(int id)
    {
        return GetForYouData().Concat(GetEditorsChoiceData()).FirstOrDefault(x => x.Id == id);
    }
}
