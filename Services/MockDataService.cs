using TravelApp.Models;

namespace TravelApp.Services;

public class MockDataService
{
    public static List<PoiModel> GetForYouData()
    {
        return new List<PoiModel>
        {
            new PoiModel
            {
                Id = 1,
                Title = "Royal London: Buckingham Palace and Pall Mall",
                Subtitle = "Walking tour",
                ImageUrl = "https://images.unsplash.com/photo-1529655683826-aba9b3e77383?w=1200&h=800&fit=crop",
                Location = "London, United Kingdom",
                Rating = 4.8,
                ReviewCount = 120,
                Price = "99.000Đ",
                Distance = "3 km | 2 mi",
                Duration = "1 h 40 min",
                Provider = "Cityzeum",
                Description = "Saint James’ Park is the nearest underground station to the starting point of the tour. This tour can be done any day of the week.",
                Credit = "Photo \"Buckingham Palace from gardens\" by Diliff under CC BY-SA 3.0"
            },
            new PoiModel
            {
                Id = 2,
                Title = "The Jade Emperor Adventure",
                Subtitle = "Bike tour",
                ImageUrl = "https://images.unsplash.com/photo-1518611505868-d7380b0f0d00?w=500&h=400&fit=crop",
                Location = "Da Nang, Vietnam",
                Rating = 4.5,
                ReviewCount = 23,
                Price = "$25",
                Distance = "5 km | 3 mi",
                Duration = "45 min",
                Provider = "TravelApp",
                Description = "A short but energetic route through iconic streets with local stories and food stops."
            },
            new PoiModel
            {
                Id = 3,
                Title = "Hanoi Old Quarter Walk",
                Subtitle = "Walking tour",
                ImageUrl = "https://images.unsplash.com/photo-1508615039623-a25605d2b022?w=500&h=400&fit=crop",
                Location = "Hanoi, Vietnam",
                Rating = 4.8,
                ReviewCount = 156,
                Price = "$15",
                Distance = "2 km | 1.2 mi",
                Duration = "1 h 20 min",
                Provider = "TravelApp",
                Description = "Explore hidden alleys, architecture, and local culture in the heart of Hanoi."
            }
        };
    }

    public static List<PoiModel> GetEditorsChoiceData()
    {
        return new List<PoiModel>
        {
            new PoiModel
            {
                Id = 4,
                Title = "Mountain Hiking",
                Subtitle = "Adventure tour",
                ImageUrl = "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=500&h=400&fit=crop",
                Location = "Sapa, Vietnam",
                Rating = 5.0,
                ReviewCount = 89,
                Price = "Free",
                Distance = "10 km | 6.2 mi",
                Duration = "2 h 00 min",
                Provider = "TravelApp",
                Description = "Guided trek with panoramic views and village visits."
            },
            new PoiModel
            {
                Id = 5,
                Title = "Beach Resort",
                Subtitle = "Beach tour",
                ImageUrl = "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=500&h=400&fit=crop",
                Location = "Phu Quoc, Vietnam",
                Rating = 4.7,
                ReviewCount = 234,
                Price = "$40",
                Distance = "8 km | 5 mi",
                Duration = "4 h 00 min",
                Provider = "TravelApp",
                Description = "Relaxed coastline route with premium beach viewpoints."
            },
            new PoiModel
            {
                Id = 6,
                Title = "Cultural Heritage",
                Subtitle = "Historical tour",
                ImageUrl = "https://images.unsplash.com/photo-1518684913413-3b40b3a1b91d?w=500&h=400&fit=crop",
                Location = "Hoi An, Vietnam",
                Rating = 4.9,
                ReviewCount = 412,
                Price = "$20",
                Distance = "4 km | 2.5 mi",
                Duration = "3 h 00 min",
                Provider = "TravelApp",
                Description = "Discover historical landmarks and stories across the ancient town."
            }
        };
    }

    public static PoiModel? GetById(int id)
    {
        return GetForYouData().Concat(GetEditorsChoiceData()).FirstOrDefault(x => x.Id == id);
    }
}
