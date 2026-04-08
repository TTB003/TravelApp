using Microsoft.EntityFrameworkCore;
using TravelApp.Domain.Entities;
using TravelApp.Infrastructure.Persistence;

public static class ProgramStartupHelpers
{
    private const string HcmTourImageUrl = "https://placehold.co/1200x800/png?text=HCM+Food+Tour";
    private const string HanoiTourImageUrl = "https://placehold.co/1200x800/png?text=Hanoi+Food+Tour";
    private const string BenThanhImageUrl = "https://placehold.co/800x600/png?text=Ben+Thanh+Market";
    private const string PhoVinhKhanhImageUrl = "https://placehold.co/800x600/png?text=Pho+Vinh+Khanh";
    private const string BachDangImageUrl = "https://placehold.co/800x600/png?text=Bach+Dang+Wharf";
    private const string ChuaMotCotImageUrl = "https://placehold.co/800x600/png?text=One+Pillar+Pagoda";
    private const string HangXanhImageUrl = "https://placehold.co/800x600/png?text=Hang+Xanh+Street";
    private const string HangDauImageUrl = "https://placehold.co/800x600/png?text=Hang+Dau+Street";

    public static async Task EnsureSeedPoisAsync(TravelAppDbContext dbContext)
    {
        var seedPois = new[]
        {
            new { Id = 1, Title = "Chợ Bến Thành", Subtitle = "Food Tour HCM - Starting Point", Description = "Điểm khởi đầu của tour ẩm thực HCM. Chợ Bến Thành là một trong những chợ truyền thống nổi tiếng nhất Sài Gòn với đa dạng hàng hóa và đặc biệt là các quán ăn địa phương.", Category = "Food Tour", Location = "Chợ Bến Thành, Quận 1, TPHCM", ImageUrl = BenThanhImageUrl, Latitude = 10.7725, Longitude = 106.6992, GeofenceRadiusMeters = 150d, Duration = "45 min", Provider = "TravelApp", Credit = "TravelApp placeholder", PrimaryLanguage = "vi", SpeechText = (string?)null },
            new { Id = 2, Title = "Phở Vĩnh Khánh", Subtitle = "Food Tour HCM - Pho Experience", Description = "Quán phở nổi tiếng với nước dùng được ninh từ 12h, phục vụ phở bò ngon nhất Quận 4. Được nhiều du khách lựa chọn trong tour ẩm thực.", Category = "Food Tour", Location = "Phố Vĩnh Khánh, Quận 4, TPHCM", ImageUrl = PhoVinhKhanhImageUrl, Latitude = 10.7660, Longitude = 106.7090, GeofenceRadiusMeters = 100d, Duration = "30 min", Provider = "TravelApp", Credit = "TravelApp placeholder", PrimaryLanguage = "vi", SpeechText = (string?)null },
            new { Id = 3, Title = "Bến Bạch Đằng", Subtitle = "Food Tour HCM - Ending Point", Description = "Kết thúc tour tại bến Bạch Đằng. Thưởng thức các đặc sản Sài Gòn và tận hưởng không khí bình minh trên bến sông.", Category = "Food Tour", Location = "Bến Bạch Đằng, Quận 1, TPHCM", ImageUrl = BachDangImageUrl, Latitude = 10.7558, Longitude = 106.7062, GeofenceRadiusMeters = 150d, Duration = "30 min", Provider = "TravelApp", Credit = "TravelApp placeholder", PrimaryLanguage = "vi", SpeechText = (string?)null },
            new { Id = 4, Title = "Chùa Một Cột", Subtitle = "Food Tour Hanoi - Starting Point", Description = "Điểm khởi đầu của tour ẩm thực Hà Nội. Chùa Một Cột là một di tích lịch sử quan trọng, nằm gần khu phố cổ Hà Nội.", Category = "Food Tour", Location = "Chùa Một Cột, Quận Ba Đình, Hà Nội", ImageUrl = ChuaMotCotImageUrl, Latitude = 21.0294, Longitude = 105.8352, GeofenceRadiusMeters = 150d, Duration = "45 min", Provider = "TravelApp", Credit = "TravelApp placeholder", PrimaryLanguage = "vi", SpeechText = (string?)null },
            new { Id = 5, Title = "Phố Hàng Xanh", Subtitle = "Food Tour Hanoi - Local Cuisine", Description = "Phố Hàng Xanh là một trong những phố cổ nổi tiếng của Hà Nội với các quán ăn truyền thống. Nơi đây bán các đặc sản ẩm thực Hà Nội như bánh mỳ, chả cá, etc.", Category = "Food Tour", Location = "Phố Hàng Xanh, Quận Hoàn Kiếm, Hà Nội", ImageUrl = HangXanhImageUrl, Latitude = 21.0285, Longitude = 105.8489, GeofenceRadiusMeters = 100d, Duration = "45 min", Provider = "TravelApp", Credit = "TravelApp placeholder", PrimaryLanguage = "vi", SpeechText = (string?)null },
            new { Id = 6, Title = "Phố Hàng Dâu", Subtitle = "Food Tour Hanoi - Ending Point", Description = "Kết thúc tour tại phố Hàng Dâu. Nơi đây nổi tiếng với các cửa hàng bán lụa truyền thống và các quán ăn địa phương.", Category = "Food Tour", Location = "Phố Hàng Dâu, Quận Hoàn Kiếm, Hà Nội", ImageUrl = HangDauImageUrl, Latitude = 21.0273, Longitude = 105.8506, GeofenceRadiusMeters = 150d, Duration = "30 min", Provider = "TravelApp", Credit = "TravelApp placeholder", PrimaryLanguage = "vi", SpeechText = (string?)null },
        };

        var seedTours = new[]
        {
            new { Id = 1, AnchorPoiId = 1, Name = "HCM Food Tour", Description = "Tour ẩm thực Sài Gòn với các điểm dừng được sắp xếp theo lộ trình thật.", CoverImageUrl = HcmTourImageUrl, PrimaryLanguage = "vi" },
            new { Id = 2, AnchorPoiId = 4, Name = "Hanoi Food Tour", Description = "Tour ẩm thực Hà Nội với các mốc waypoint, bản đồ và audio tự động.", CoverImageUrl = HanoiTourImageUrl, PrimaryLanguage = "vi" },
        };

        foreach (var seedPoi in seedPois)
        {
            var poi = await dbContext.Pois.FirstOrDefaultAsync(x => x.Id == seedPoi.Id);
            if (poi is null)
            {
                continue;
            }

            poi.Title = seedPoi.Title;
            poi.Subtitle = seedPoi.Subtitle;
            poi.Description = seedPoi.Description;
            poi.Category = seedPoi.Category;
            poi.Location = seedPoi.Location;
            poi.ImageUrl = seedPoi.ImageUrl;
            poi.Latitude = seedPoi.Latitude;
            poi.Longitude = seedPoi.Longitude;
            poi.GeofenceRadiusMeters = seedPoi.GeofenceRadiusMeters;
            poi.Duration = seedPoi.Duration;
            poi.Provider = seedPoi.Provider;
            poi.Credit = seedPoi.Credit;
            poi.PrimaryLanguage = seedPoi.PrimaryLanguage;
            poi.SpeechText = seedPoi.SpeechText;
        }

        foreach (var seedTour in seedTours)
        {
            var tour = await dbContext.Tours.FirstOrDefaultAsync(x => x.Id == seedTour.Id);
            if (tour is null)
            {
                continue;
            }

            tour.AnchorPoiId = seedTour.AnchorPoiId;
            tour.Name = seedTour.Name;
            tour.Description = seedTour.Description;
            tour.CoverImageUrl = seedTour.CoverImageUrl;
            tour.PrimaryLanguage = seedTour.PrimaryLanguage;
            tour.IsPublished = true;
            tour.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        var existingPoiIds = await dbContext.Pois
            .AsNoTracking()
            .Select(x => x.Id)
            .ToHashSetAsync();

        var seedAudios = new[]
        {
            new { Id = 1, PoiId = 1, LanguageCode = "en", AudioUrl = "https://travel-app-audios.blob.core.windows.net/audio/hcm-cho-ben-thanh-en.mp3", Transcript = "Welcome to Ben Thanh Market, the heart of Saigon shopping. This market has been serving locals and tourists since 1914.", IsGenerated = false },
            new { Id = 2, PoiId = 2, LanguageCode = "en", AudioUrl = "https://travel-app-audios.blob.core.windows.net/audio/hcm-pho-vinh-khanh-en.mp3", Transcript = "This is Pho Vinh Khanh, famous for their 12-hour broth. The beef pho here is a must-try local specialty.", IsGenerated = false },
            new { Id = 3, PoiId = 3, LanguageCode = "en", AudioUrl = "https://travel-app-audios.blob.core.windows.net/audio/hcm-ben-bach-dang-en.mp3", Transcript = "Welcome to Bach Dang Wharf. Enjoy Saigon's sunset and local delicacies at this historic riverside location.", IsGenerated = false },
            new { Id = 4, PoiId = 4, LanguageCode = "en", AudioUrl = "https://travel-app-audios.blob.core.windows.net/audio/hanoi-chua-mot-cot-en.mp3", Transcript = "We start our Hanoi food tour at the One Pillar Pagoda, a historic Buddhist temple near the old town.", IsGenerated = false },
            new { Id = 5, PoiId = 5, LanguageCode = "en", AudioUrl = "https://travel-app-audios.blob.core.windows.net/audio/hanoi-hang-xanh-en.mp3", Transcript = "Hang Xanh Street is one of Hanoi's famous old streets. Try local specialties like banh mi and cha ca here.", IsGenerated = false },
            new { Id = 6, PoiId = 6, LanguageCode = "en", AudioUrl = "https://travel-app-audios.blob.core.windows.net/audio/hanoi-hang-dau-en.mp3", Transcript = "We end our tour at Hang Dau Street, known for its traditional silk shops and local eateries.", IsGenerated = false },
            new { Id = 7, PoiId = 1, LanguageCode = "vi", AudioUrl = "https://travel-app-audios.blob.core.windows.net/audio/hcm-cho-ben-thanh-vi.mp3", Transcript = "Chào mừng đến Chợ Bến Thành, trái tim mua sắm của Sài Gòn. Chợ này đã phục vụ người dân và du khách từ năm 1914.", IsGenerated = false },
            new { Id = 8, PoiId = 2, LanguageCode = "vi", AudioUrl = "https://travel-app-audios.blob.core.windows.net/audio/hcm-pho-vinh-khanh-vi.mp3", Transcript = "Đây là Phở Vĩnh Khánh, nổi tiếng với nước dùng được ninh 12 tiếng. Phở bò ở đây là đặc sản địa phương không thể bỏ qua.", IsGenerated = false },
            new { Id = 9, PoiId = 3, LanguageCode = "vi", AudioUrl = "https://travel-app-audios.blob.core.windows.net/audio/hcm-ben-bach-dang-vi.mp3", Transcript = "Chào mừng đến Bến Bạch Đằng. Hãy tận hưởng hoàng hôn Sài Gòn và các đặc sản địa phương tại địa điểm lịch sử này.", IsGenerated = false },
            new { Id = 10, PoiId = 4, LanguageCode = "vi", AudioUrl = "https://travel-app-audios.blob.core.windows.net/audio/hanoi-chua-mot-cot-vi.mp3", Transcript = "Chúng ta bắt đầu tour ẩm thực Hà Nội tại Chùa Một Cột, một ngôi chùa Phật giáo lịch sử gần khu phố cổ.", IsGenerated = false },
            new { Id = 11, PoiId = 5, LanguageCode = "vi", AudioUrl = "https://travel-app-audios.blob.core.windows.net/audio/hanoi-hang-xanh-vi.mp3", Transcript = "Phố Hàng Xanh là một trong những phố cổ nổi tiếng của Hà Nội. Hãy thử các đặc sản địa phương như bánh mỳ và chả cá ở đây.", IsGenerated = false },
            new { Id = 12, PoiId = 6, LanguageCode = "vi", AudioUrl = "https://travel-app-audios.blob.core.windows.net/audio/hanoi-hang-dau-vi.mp3", Transcript = "Chúng ta kết thúc tour tại Phố Hàng Dâu, nổi tiếng với các cửa hàng bán lụa truyền thống và các quán ăn địa phương.", IsGenerated = false },
        };

        foreach (var seedAudio in seedAudios)
        {
            if (!existingPoiIds.Contains(seedAudio.PoiId))
            {
                continue;
            }

            var audio = await dbContext.PoiAudios
                .FirstOrDefaultAsync(x => x.PoiId == seedAudio.PoiId && x.LanguageCode == seedAudio.LanguageCode);

            if (audio is null)
            {
                dbContext.PoiAudios.Add(new PoiAudio
                {
                    PoiId = seedAudio.PoiId,
                    LanguageCode = seedAudio.LanguageCode,
                    AudioUrl = seedAudio.AudioUrl,
                    Transcript = seedAudio.Transcript,
                    IsGenerated = seedAudio.IsGenerated,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
                continue;
            }

            audio.LanguageCode = seedAudio.LanguageCode;
            audio.AudioUrl = seedAudio.AudioUrl;
            audio.Transcript = seedAudio.Transcript;
            audio.IsGenerated = seedAudio.IsGenerated;
        }

        await dbContext.SaveChangesAsync();
    }

    public static async Task EnsureDemoLoginUsersAsync(TravelAppDbContext dbContext)
    {
        var demoUsers = new[]
        {
            new { Email = "demo@example.com", UserName = "demo_user", Password = "Demo@123456", Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440001") },
            new { Email = "khanh@example.com", UserName = "khanh_user", Password = "Khanh@123456", Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440002") },
            new { Email = "guest@example.com", UserName = "guest_user", Password = "Guest@123456", Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440003") },
        };

        foreach (var demoUser in demoUsers)
        {
            var existingUser = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == demoUser.Email);
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(demoUser.Password);

            if (existingUser is null)
            {
                dbContext.Users.Add(new User
                {
                    Id = demoUser.Id,
                    UserName = demoUser.UserName,
                    Email = demoUser.Email,
                    PasswordHash = passwordHash,
                    IsActive = true,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            }
            else
            {
                existingUser.UserName = demoUser.UserName;
                existingUser.PasswordHash = passwordHash;
                existingUser.IsActive = true;
            }
        }

        await dbContext.SaveChangesAsync();
    }
}
