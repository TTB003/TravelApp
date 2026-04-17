using System.Globalization;
using System.Windows.Input;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TravelApp.Mobile.Services
{
    public class LocalizationManager : INotifyPropertyChanged
    {
        // Singleton Instance để XAML có thể Bind tới
        public static LocalizationManager Instance { get; } = new LocalizationManager();

        public event PropertyChangedEventHandler PropertyChanged;

        private const string LanguageKey = "SelectedLanguage";

        // Đổi thành Instance property để Bind từ XAML dễ dàng hơn
        public ICommand ToggleLanguageCommand => new Command(ToggleLanguage);
        public ICommand ChangeLanguageCommand => new Command(async () => await ShowLanguagePicker());

        private static readonly string[] SupportedLangs = 
        { 
            "vi-VN", "en-US", "fr-FR", "ja-JP", "ko-KR", "zh-CN", "de-DE", "es-ES" 
        };

        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
        {
            ["vi-VN"] = new() { 
                ["Explore"] = "Khám phá", ["Map"] = "Bản đồ", ["Search"] = "Tìm kiếm",
                ["Profile"] = "Hồ sơ của tôi", ["History"] = "Lịch sử", ["Bookmarks"] = "Đã lưu",
                ["AroundMe"] = "Xung quanh tôi", ["PoiMap"] = "Bản đồ POI", ["MapView"] = "Xem bản đồ",
                ["NoStories"] = "Không có câu chuyện nào", ["Start"] = "Bắt đầu", ["Featured"] = "Nổi bật",
                ["Login"] = "Đăng nhập", ["Logout"] = "Đăng xuất", ["Purchases"] = "Thư viện âm thanh",
                ["Debug"] = "Bảng điều khiển Debug"
            },
            ["en-US"] = new() { 
                ["Explore"] = "Explore", ["Map"] = "Map", ["Search"] = "Search",
                ["Profile"] = "My Profile", ["History"] = "History", ["Bookmarks"] = "Bookmarks",
                ["AroundMe"] = "Around me", ["PoiMap"] = "POI Map", ["MapView"] = "Map view",
                ["NoStories"] = "No Stories available", ["Start"] = "Start", ["Featured"] = "Featured",
                ["Login"] = "Sign In", ["Logout"] = "Sign Out", ["Purchases"] = "Audio Library",
                ["Debug"] = "Debug Console"
            },
            ["fr-FR"] = new() { 
                ["Explore"] = "Explorer", ["Map"] = "Carte", ["Search"] = "Chercher",
                ["Profile"] = "Mon Profil", ["History"] = "Histoire", ["Bookmarks"] = "Signets",
                ["AroundMe"] = "Autour de moi", ["PoiMap"] = "Carte POI", ["MapView"] = "Vue carte",
                ["NoStories"] = "Aucune histoire", ["Start"] = "Commencer", ["Featured"] = "Vedette",
                ["Login"] = "Connexion", ["Logout"] = "Déconnexion", ["Purchases"] = "Bibliothèque audio",
                ["Debug"] = "Console de débogage"
            },
            ["ja-JP"] = new() { 
                ["Explore"] = "探索", ["Map"] = "地図", ["Search"] = "検索",
                ["Profile"] = "マイプロフィール", ["History"] = "履歴", ["Bookmarks"] = "ブックマーク",
                ["AroundMe"] = "周辺", ["PoiMap"] = "POIマップ", ["MapView"] = "マップ表示",
                ["NoStories"] = "ストーリーなし", ["Start"] = "開始", ["Featured"] = "おすすめ",
                ["Login"] = "ログイン", ["Logout"] = "ログアウト", ["Purchases"] = "オーディオライブラリ",
                ["Debug"] = "デバッグコンソール"
            }
        };

        public void Init()
        {
            // Lấy ngôn ngữ đã lưu hoặc mặc định là Tiếng Việt
            var lang = Preferences.Get(LanguageKey, "vi-VN");
            SetLanguage(lang);
        }

        public void SetLanguage(string languageCode)
        {
            var culture = new CultureInfo(languageCode);
            
            // Thiết lập ngôn ngữ hệ thống cho luồng hiện tại
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // Lưu lựa chọn của người dùng vào bộ nhớ máy
            Preferences.Set(LanguageKey, languageCode);
            
            // Thông báo cho UI cập nhật các thuộc tính Binding
            OnPropertyChanged(null); 

            if (Application.Current?.MainPage != null)
            {
                // Buộc ứng dụng phải nạp lại AppShell mới để cập nhật menu và tiêu đề
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try {
                        // Việc gán lại MainPage sẽ kích hoạt XAML của AppShell chạy lại từ đầu
                        Application.Current.MainPage = MauiProgram.Services.GetRequiredService<AppShell>();
                    } catch (Exception ex) {
                        System.Diagnostics.Debug.WriteLine($"Error reloading UI: {ex.Message}");
                    }
                });
            }
        }

        public void ToggleLanguage()
        {
            // Xoay vòng qua danh sách ngôn ngữ hỗ trợ
            int currentIndex = System.Array.IndexOf(SupportedLangs, CurrentLanguage);
            int nextIndex = (currentIndex + 1) % SupportedLangs.Length;
            SetLanguage(SupportedLangs[nextIndex]);
        }

        private async Task ShowLanguagePicker()
        {
            // Hiển thị danh sách cho người dùng chọn thay vì xoay vòng
            string result = await Shell.Current.DisplayActionSheet(
                "Chọn ngôn ngữ / Select Language", 
                "Hủy", null, 
                "Tiếng Việt 🇻🇳", "English 🇺🇸", "Français 🇫🇷", "日本語 🇯🇵");
            
            switch (result)
            {
                case "Tiếng Việt 🇻🇳": SetLanguage("vi-VN"); break;
                case "English 🇺🇸": SetLanguage("en-US"); break;
                case "Français 🇫🇷": SetLanguage("fr-FR"); break;
                case "日本語 🇯🇵": SetLanguage("ja-JP"); break;
            }
        }

        public string CurrentLanguage => Preferences.Get(LanguageKey, "vi-VN");
        
        public string CurrentLanguageFlag => CurrentLanguage switch
        {
            "vi-VN" => "🇻🇳",
            "en-US" => "🇺🇸",
            "fr-FR" => "🇫🇷",
            "ja-JP" => "🇯🇵",
            "ko-KR" => "🇰🇷",
            "zh-CN" => "🇨🇳",
            "de-DE" => "🇩🇪",
            "es-ES" => "🇪🇸",
            _ => "🌐"
        };

        public string Get(string key)
        {
            if (Translations.TryGetValue(CurrentLanguage, out var langDict) && langDict.TryGetValue(key, out var value))
                return value;
            
            // An toàn hơn: Nếu không tìm thấy trong ngôn ngữ hiện tại, thử tiếng Anh, nếu không thấy nữa thì trả về chính cái Key
            if (Translations["en-US"].TryGetValue(key, out var enValue)) return enValue;
            return key; 
        }

        // Các thuộc tính mà XAML của bạn đang yêu cầu
        public string ExploreTitle => Get("Explore");
        public string MapTitle => Get("Map");
        public string SearchPlaceholder => Get("Search");
        public string ProfileText => Get("Profile");
        public string HistoryText => Get("History");
        public string BookmarksText => Get("Bookmarks");
        public string AroundMeText => Get("AroundMe");
        public string PoiMapText => Get("PoiMap");
        public string MapViewText => Get("MapView");
        public string NoStoriesText => Get("NoStories");
        public string StartText => Get("Start");
        public string FeaturedText => Get("Featured");
        public string DebugMenuText => Get("Debug");

        // Thêm các thuộc tính cho Menu
        public string PurchasesMenuText => Get("Purchases");
        
        // Đối với Login/Logout, bạn có thể thêm logic kiểm tra trạng thái đăng nhập ở đây nếu muốn
        // Ở đây tôi giả định trả về "Đăng nhập/Sign In" để menu hiện chữ lại đã
        public string AuthMenuText => Get("Login"); 


        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Logic thông minh: Tự động lấy bản dịch phù hợp, nếu Admin chưa nhập 
        /// thì trả về ngôn ngữ gốc để App không bị lỗi hiển thị.
        /// </summary>
        public static string GetLocalizedValue(IEnumerable<dynamic> localizations, string propertyName)
        {
            if (localizations == null) return string.Empty;

            // 1. Tìm bản dịch đúng ngôn ngữ hiện tại
            var match = localizations.FirstOrDefault(l => l.LanguageCode == Instance.CurrentLanguage);
            if (match != null)
            {
                var value = match.GetType().GetProperty(propertyName)?.GetValue(match, null) as string;
                if (!string.IsNullOrEmpty(value)) return value;
            }

            // 2. Nếu Admin chưa nhập ngôn ngữ đó, lấy đại ngôn ngữ đầu tiên có trong danh sách (thường là tiếng gốc)
            var fallback = localizations.FirstOrDefault();
            return fallback?.GetType().GetProperty(propertyName)?.GetValue(fallback, null) as string ?? string.Empty;
        }
    }
}