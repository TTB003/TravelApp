using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelApp.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class SeedFoodTours : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Insert POI for HCM Food Tour
        migrationBuilder.InsertData(
            table: "POI",
            columns: new[] { "Id", "Title", "Subtitle", "Description", "Category", "Location", "ImageUrl", "Latitude", "Longitude", "GeofenceRadiusMeters", "Duration", "Provider", "Credit", "PrimaryLanguage", "CreatedAtUtc", "UpdatedAtUtc" },
            values: new object[,]
            {
                // HCM - Cho Ben Thanh
                {
                    1,
                    "Chợ Bến Thành",
                    "Food Tour HCM - Starting Point",
                    "Điểm khởi đầu của tour ẩm thực HCM. Chợ Bến Thành là một trong những chợ truyền thống nổi tiếng nhất Sài Gòn với đa dạng hàng hóa và đặc biệt là các quán ăn địa phương.",
                    "Food Tour",
                    "Chợ Bến Thành, Quận 1, TPHCM",
                    "https://placehold.co/800x600/png?text=Ben+Thanh+Market",
                    10.7725,
                    106.6992,
                    150d,
                    "45 min",
                    "TravelApp",
                    "TravelApp placeholder",
                    "vi",
                    new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    null
                },
                // HCM - Pho Vinh Khanh Q4
                {
                    2,
                    "Phở Vĩnh Khánh",
                    "Food Tour HCM - Pho Experience",
                    "Quán phở nổi tiếng với nước dùng được ninh từ 12h, phục vụ phở bò ngon nhất Quận 4. Được nhiều du khách lựa chọn trong tour ẩm thực.",
                    "Food Tour",
                    "Phố Vĩnh Khánh, Quận 4, TPHCM",
                    "https://placehold.co/800x600/png?text=Pho+Vinh+Khanh",
                    10.7660,
                    106.7090,
                    100d,
                    "30 min",
                    "TravelApp",
                    "TravelApp placeholder",
                    "vi",
                    new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    null
                },
                // HCM - Ben Bach Dang
                {
                    3,
                    "Bến Bạch Đằng",
                    "Food Tour HCM - Ending Point",
                    "Kết thúc tour tại bến Bạch Đằng. Thưởng thức các đặc sản Sài Gòn và tận hưởng không khí bình minh trên bến sông.",
                    "Food Tour",
                    "Bến Bạch Đằng, Quận 1, TPHCM",
                    "https://placehold.co/800x600/png?text=Bach+Dang+Wharf",
                    10.7558,
                    106.7062,
                    150d,
                    "30 min",
                    "TravelApp",
                    "TravelApp placeholder",
                    "vi",
                    new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    null
                },
                // Hanoi - Chua 1 Cot
                {
                    4,
                    "Chùa Một Cột",
                    "Food Tour Hanoi - Starting Point",
                    "Điểm khởi đầu của tour ẩm thực Hà Nội. Chùa Một Cột là một di tích lịch sử quan trọng, nằm gần khu phố cổ Hà Nội.",
                    "Food Tour",
                    "Chùa Một Cột, Quận Ba Đình, Hà Nội",
                    "https://placehold.co/800x600/png?text=One+Pillar+Pagoda",
                    21.0294,
                    105.8352,
                    150d,
                    "45 min",
                    "TravelApp",
                    "TravelApp placeholder",
                    "vi",
                    new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    null
                },
                // Hanoi - Pho Hang Xanh
                {
                    5,
                    "Phố Hàng Xanh",
                    "Food Tour Hanoi - Local Cuisine",
                    "Phố Hàng Xanh là một trong những phố cổ nổi tiếng của Hà Nội với các quán ăn truyền thống. Nơi đây bán các đặc sản ẩm thực Hà Nội như bánh mỳ, chả cá, etc.",
                    "Food Tour",
                    "Phố Hàng Xanh, Quận Hoàn Kiếm, Hà Nội",
                    "https://placehold.co/800x600/png?text=Hang+Xanh+Street",
                    21.0285,
                    105.8489,
                    100d,
                    "45 min",
                    "TravelApp",
                    "TravelApp placeholder",
                    "vi",
                    new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    null
                },
                // Hanoi - Pho Hang Dau
                {
                    6,
                    "Phố Hàng Dâu",
                    "Food Tour Hanoi - Ending Point",
                    "Kết thúc tour tại phố Hàng Dâu. Nơi đây nổi tiếng với các cửa hàng bán lụa truyền thống và các quán ăn địa phương.",
                    "Food Tour",
                    "Phố Hàng Dâu, Quận Hoàn Kiếm, Hà Nội",
                    "https://placehold.co/800x600/png?text=Hang+Dau+Street",
                    21.0273,
                    105.8506,
                    150d,
                    "30 min",
                    "TravelApp",
                    "TravelApp placeholder",
                    "vi",
                    new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    null
                }
            }
        );

        // Insert Localizations - Vietnamese
        migrationBuilder.InsertData(
            table: "POI_Localization",
            columns: new[] { "Id", "PoiId", "LanguageCode", "Title", "Subtitle", "Description" },
            values: new object[,]
            {
                // HCM Localizations - Vietnamese
                {
                    1, 1, "vi",
                    "Chợ Bến Thành",
                    "Tour Ẩm Thực HCM - Điểm Khởi Đầu",
                    "Điểm khởi đầu của tour ẩm thực HCM. Chợ Bến Thành là một trong những chợ truyền thống nổi tiếng nhất Sài Gòn với đa dạng hàng hóa và đặc biệt là các quán ăn địa phương."
                },
                {
                    2, 2, "vi",
                    "Phở Vĩnh Khánh",
                    "Tour Ẩm Thực HCM - Trải Nghiệm Phở",
                    "Quán phở nổi tiếng với nước dùng được ninh từ 12h, phục vụ phở bò ngon nhất Quận 4. Được nhiều du khách lựa chọn trong tour ẩm thực."
                },
                {
                    3, 3, "vi",
                    "Bến Bạch Đằng",
                    "Tour Ẩm Thực HCM - Điểm Kết Thúc",
                    "Kết thúc tour tại bến Bạch Đằng. Thưởng thức các đặc sản Sài Gòn và tận hưởng không khí bình minh trên bến sông."
                },
                // Hanoi Localizations - Vietnamese
                {
                    4, 4, "vi",
                    "Chùa Một Cột",
                    "Tour Ẩm Thực Hà Nội - Điểm Khởi Đầu",
                    "Điểm khởi đầu của tour ẩm thực Hà Nội. Chùa Một Cột là một di tích lịch sử quan trọng, nằm gần khu phố cổ Hà Nội."
                },
                {
                    5, 5, "vi",
                    "Phố Hàng Xanh",
                    "Tour Ẩm Thực Hà Nội - Ẩm Thực Địa Phương",
                    "Phố Hàng Xanh là một trong những phố cổ nổi tiếng của Hà Nội với các quán ăn truyền thống. Nơi đây bán các đặc sản ẩm thực Hà Nội như bánh mỳ, chả cá, etc."
                },
                {
                    6, 6, "vi",
                    "Phố Hàng Dâu",
                    "Tour Ẩm Thực Hà Nội - Điểm Kết Thúc",
                    "Kết thúc tour tại phố Hàng Dâu. Nơi đây nổi tiếng với các cửa hàng bán lụa truyền thống và các quán ăn địa phương."
                }
            }
        );

        // Insert Audio assets - English
        migrationBuilder.InsertData(
            table: "Audio",
            columns: new[] { "Id", "PoiId", "LanguageCode", "AudioUrl", "Transcript", "IsGenerated", "CreatedAtUtc" },
            values: new object[,]
            {
                {
                    1, 1, "en",
                    "https://travel-app-audios.blob.core.windows.net/audio/hcm-cho-ben-thanh-en.mp3",
                    "Welcome to Ben Thanh Market, the heart of Saigon shopping. This market has been serving locals and tourists since 1914.",
                    false,
                    new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
                },
                {
                    2, 2, "en",
                    "https://travel-app-audios.blob.core.windows.net/audio/hcm-pho-vinh-khanh-en.mp3",
                    "This is Pho Vinh Khanh, famous for their 12-hour broth. The beef pho here is a must-try local specialty.",
                    false,
                    new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
                },
                {
                    3, 3, "en",
                    "https://travel-app-audios.blob.core.windows.net/audio/hcm-ben-bach-dang-en.mp3",
                    "Welcome to Bach Dang Wharf. Enjoy Saigon's sunset and local delicacies at this historic riverside location.",
                    false,
                    new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
                },
                {
                    4, 4, "en",
                    "https://travel-app-audios.blob.core.windows.net/audio/hanoi-chua-mot-cot-en.mp3",
                    "We start our Hanoi food tour at the One Pillar Pagoda, a historic Buddhist temple near the old town.",
                    false,
                    new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
                },
                {
                    5, 5, "en",
                    "https://travel-app-audios.blob.core.windows.net/audio/hanoi-hang-xanh-en.mp3",
                    "Hang Xanh Street is one of Hanoi's famous old streets. Try local specialties like banh mi and cha ca here.",
                    false,
                    new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
                },
                {
                    6, 6, "en",
                    "https://travel-app-audios.blob.core.windows.net/audio/hanoi-hang-dau-en.mp3",
                    "We end our tour at Hang Dau Street, known for its traditional silk shops and local eateries.",
                    false,
                    new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
                }
            }
        );

        // Insert Audio assets - Vietnamese
        migrationBuilder.InsertData(
            table: "Audio",
            columns: new[] { "Id", "PoiId", "LanguageCode", "AudioUrl", "Transcript", "IsGenerated", "CreatedAtUtc" },
            values: new object[,]
            {
                {
                    7, 1, "vi",
                    "https://travel-app-audios.blob.core.windows.net/audio/hcm-cho-ben-thanh-vi.mp3",
                    "Chào mừng đến Chợ Bến Thành, trái tim mua sắm của Sài Gòn. Chợ này đã phục vụ người dân và du khách từ năm 1914.",
                    false,
                    new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
                },
                {
                    8, 2, "vi",
                    "https://travel-app-audios.blob.core.windows.net/audio/hcm-pho-vinh-khanh-vi.mp3",
                    "Đây là Phở Vĩnh Khánh, nổi tiếng với nước dùng được ninh 12 tiếng. Phở bò ở đây là đặc sản địa phương không thể bỏ qua.",
                    false,
                    new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
                },
                {
                    9, 3, "vi",
                    "https://travel-app-audios.blob.core.windows.net/audio/hcm-ben-bach-dang-vi.mp3",
                    "Chào mừng đến Bến Bạch Đằng. Hãy tận hưởng hoàng hôn Sài Gòn và các đặc sản địa phương tại địa điểm lịch sử này.",
                    false,
                    new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
                },
                {
                    10, 4, "vi",
                    "https://travel-app-audios.blob.core.windows.net/audio/hanoi-chua-mot-cot-vi.mp3",
                    "Chúng ta bắt đầu tour ẩm thực Hà Nội tại Chùa Một Cột, một ngôi chùa Phật giáo lịch sử gần khu phố cổ.",
                    false,
                    new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
                },
                {
                    11, 5, "vi",
                    "https://travel-app-audios.blob.core.windows.net/audio/hanoi-hang-xanh-vi.mp3",
                    "Phố Hàng Xanh là một trong những phố cổ nổi tiếng của Hà Nội. Hãy thử các đặc sản địa phương như bánh mỳ và chả cá ở đây.",
                    false,
                    new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
                },
                {
                    12, 6, "vi",
                    "https://travel-app-audios.blob.core.windows.net/audio/hanoi-hang-dau-vi.mp3",
                    "Chúng ta kết thúc tour tại Phố Hàng Dâu, nổi tiếng với các cửa hàng bán lụa truyền thống và các quán ăn địa phương.",
                    false,
                    new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
                }
            }
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteData(
            table: "Audio",
            keyColumn: "Id",
            keyValue: 1);
        migrationBuilder.DeleteData(
            table: "Audio",
            keyColumn: "Id",
            keyValue: 2);
        migrationBuilder.DeleteData(
            table: "Audio",
            keyColumn: "Id",
            keyValue: 3);
        migrationBuilder.DeleteData(
            table: "Audio",
            keyColumn: "Id",
            keyValue: 4);
        migrationBuilder.DeleteData(
            table: "Audio",
            keyColumn: "Id",
            keyValue: 5);
        migrationBuilder.DeleteData(
            table: "Audio",
            keyColumn: "Id",
            keyValue: 6);
        migrationBuilder.DeleteData(
            table: "Audio",
            keyColumn: "Id",
            keyValue: 7);
        migrationBuilder.DeleteData(
            table: "Audio",
            keyColumn: "Id",
            keyValue: 8);
        migrationBuilder.DeleteData(
            table: "Audio",
            keyColumn: "Id",
            keyValue: 9);
        migrationBuilder.DeleteData(
            table: "Audio",
            keyColumn: "Id",
            keyValue: 10);
        migrationBuilder.DeleteData(
            table: "Audio",
            keyColumn: "Id",
            keyValue: 11);
        migrationBuilder.DeleteData(
            table: "Audio",
            keyColumn: "Id",
            keyValue: 12);

        migrationBuilder.DeleteData(
            table: "POI_Localization",
            keyColumn: "Id",
            keyValue: 1);
        migrationBuilder.DeleteData(
            table: "POI_Localization",
            keyColumn: "Id",
            keyValue: 2);
        migrationBuilder.DeleteData(
            table: "POI_Localization",
            keyColumn: "Id",
            keyValue: 3);
        migrationBuilder.DeleteData(
            table: "POI_Localization",
            keyColumn: "Id",
            keyValue: 4);
        migrationBuilder.DeleteData(
            table: "POI_Localization",
            keyColumn: "Id",
            keyValue: 5);
        migrationBuilder.DeleteData(
            table: "POI_Localization",
            keyColumn: "Id",
            keyValue: 6);

        migrationBuilder.DeleteData(
            table: "POI",
            keyColumn: "Id",
            keyValue: 1);
        migrationBuilder.DeleteData(
            table: "POI",
            keyColumn: "Id",
            keyValue: 2);
        migrationBuilder.DeleteData(
            table: "POI",
            keyColumn: "Id",
            keyValue: 3);
        migrationBuilder.DeleteData(
            table: "POI",
            keyColumn: "Id",
            keyValue: 4);
        migrationBuilder.DeleteData(
            table: "POI",
            keyColumn: "Id",
            keyValue: 5);
        migrationBuilder.DeleteData(
            table: "POI",
            keyColumn: "Id",
            keyValue: 6);
    }
}
