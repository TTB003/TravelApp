using System.Globalization;
using System.Windows.Input;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TravelApp.Services;

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
            "vi", "en", "fr", "ja" 
        };

        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
        {
            ["vi"] = new() { 
                ["Explore"] = "Khám phá", ["Map"] = "Bản đồ", ["Search"] = "Tìm kiếm",
                ["Profile"] = "Hồ sơ của tôi", ["History"] = "Lịch sử", ["Bookmarks"] = "Đã lưu",
                ["AroundMe"] = "Xung quanh tôi", ["PoiMap"] = "Bản đồ POI", ["MapView"] = "Xem bản đồ", ["Hi"] = "Xin chào", ["WelcomeBack"] = "Chào mừng bạn quay lại",
                ["NoStories"] = "Không có câu chuyện nào", ["Start"] = "Bắt đầu", ["Featured"] = "Nổi bật",
                ["Login"] = "Đăng nhập", ["Logout"] = "Đăng xuất", ["Purchases"] = "Thư viện âm thanh",
                ["Debug"] = "Bảng điều khiển Debug", ["ViewTour"] = "XEM LỘ TRÌNH TOUR", ["Stop"] = "Điểm dừng",
                ["LiveRoute"] = "Lộ trình Tour", ["NowPlaying"] = "Đang phát", ["Stops"] = "điểm dừng", ["Open"] = "Mở", ["Downloads"] = "Tải xuống",
                ["Heatmap"] = "Bản đồ nhiệt",
                ["ShowHeatmap"] = "Xem vùng phổ biến",
                ["TrendingTitle"] = "🔥 Xu hướng hiện tại",
                ["AudioGuide"] = "Thuyết minh",
                ["NowPlayingAudio"] = "Đang phát thuyết minh",
                ["Saved"] = "Đã lưu",
                ["EditSpeechTextOwner"] = "CHỈNH SỬA THUYẾT MINH (OWNER)",
                ["ChooseLanguageDesc"] = "Chọn ngôn ngữ khác từ nút phía trên để thêm bản dịch mới.",
                ["SaveContent"] = "LƯU NỘI DUNG",
                ["Saving"] = "Đang lưu...",
                ["ScanToShare"] = "QUÉT ĐỂ XEM CHI TIẾT & CHIA SẺ",
                ["OrScanOtherPois"] = "Hoặc quét để xem các POI khác",
                ["ChooseTtsLanguage"] = "Chọn ngôn ngữ TTS",
                ["AuthSubtitle"] = "Thưởng thức các tour thuyết minh miễn phí và tích lũy điểm thưởng.",
                ["EmailPlaceholder"] = "Nhập địa chỉ e-mail *",
                ["PasswordPlaceholder"] = "Mật khẩu *",
                ["ShowText"] = "Hiện",
                ["HideText"] = "Ẩn",
                ["QuickLoginOwner"] = "🚀 ĐĂNG NHẬP NHANH (OWNER)",
                ["ForgotPassword"] = "Quên mật khẩu?",
                ["DemoCredentials"] = "Tài khoản demo: demo@example.com / Demo@123456",
                ["PopularNow"] = "Phổ biến nhất",
                ["MyAccount"] = "Tài khoản của tôi",
                ["EditProfile"] = "Chỉnh sửa hồ sơ",
                ["PreferencesLabel"] = "Tùy chỉnh",
                ["UserPreferences"] = "Tùy chọn người dùng",
                ["BackupRestore"] = "Sao lưu & Khôi phục",
                ["BackupDesc"] = "Xuất ra file SQLite để sao lưu hoặc chuyển sang máy khác.",
                ["ExportDb"] = "Xuất dữ liệu",
                ["ImportDb"] = "Nhập dữ liệu",
                ["SpeechTextPlaceholder"] = "Nhập nội dung thuyết minh...",
                ["HcmFoodTour"] = "🍲 Tour ẩm thực Hồ Chí Minh",
                ["HanoiFoodTour"] = "🍜 Tour ẩm thực Hà Nội"
            },
            ["en"] = new() { 
                ["Explore"] = "Explore", ["Map"] = "Map", ["Search"] = "Search",
                ["Profile"] = "My Profile", ["History"] = "History", ["Bookmarks"] = "Bookmarks",
                ["AroundMe"] = "Around me", ["PoiMap"] = "POI Map", ["MapView"] = "Map view", ["Hi"] = "Hi", ["WelcomeBack"] = "Welcome back",
                ["NoStories"] = "No Stories available", ["Start"] = "Start", ["Featured"] = "Featured",
                ["Login"] = "Sign In", ["Logout"] = "Sign Out", ["Purchases"] = "Audio Library", ["Stop"] = "Stop",
                ["Debug"] = "Debug Console", ["ViewTour"] = "VIEW TOUR ROUTE", ["Downloads"] = "Downloads",
                ["LiveRoute"] = "Live Tour Route", ["NowPlaying"] = "Now Playing", ["Stops"] = "stops", ["Open"] = "Open",
                ["Heatmap"] = "Heatmap",
                ["ShowHeatmap"] = "Show popular areas",
                ["TrendingTitle"] = "🔥 Trending Now",
                ["AudioGuide"] = "Audio Guide",
                ["NowPlayingAudio"] = "Playing audio guide",
                ["Saved"] = "Saved",
                ["EditSpeechTextOwner"] = "EDIT SPEECH TEXT (OWNER)",
                ["ChooseLanguageDesc"] = "Select another language from the button above to add a new translation.",
                ["SaveContent"] = "SAVE CONTENT",
                ["Saving"] = "Saving...",
                ["ScanToShare"] = "SCAN TO VIEW DETAILS & SHARE",
                ["OrScanOtherPois"] = "Or scan to view other POIs",
                ["ChooseTtsLanguage"] = "Choose TTS Language",
                ["AuthSubtitle"] = "Enjoy free audio guided tours and redeem credits for paid tours.",
                ["EmailPlaceholder"] = "Enter your e-mail address *",
                ["PasswordPlaceholder"] = "Password *",
                ["ShowText"] = "Show",
                ["HideText"] = "Hide",
                ["QuickLoginOwner"] = "🚀 QUICK LOGIN (OWNER)",
                ["ForgotPassword"] = "Forgot password?",
                ["DemoCredentials"] = "Demo credentials: demo@example.com / Demo@123456",
                ["PopularNow"] = "Popular Now",
                ["MyAccount"] = "My Account",
                ["EditProfile"] = "Edit Profile",
                ["PreferencesLabel"] = "Preferences",
                ["UserPreferences"] = "User preferences",
                ["BackupRestore"] = "Backup & Restore",
                ["BackupDesc"] = "Export to SQLite file for backup or transferring to another device.",
                ["ExportDb"] = "Export database",
                ["ImportDb"] = "Import database",
                ["SpeechTextPlaceholder"] = "Enter audio guide content...",
                ["HcmFoodTour"] = "🍲 Ho Chi Minh Food Tour",
                ["HanoiFoodTour"] = "🍜 Hanoi Food Tour"
            },
            ["fr"] = new() { 
                ["Explore"] = "Explorer", ["Map"] = "Carte", ["Search"] = "Chercher",
                ["Profile"] = "Mon Profil", ["History"] = "Histoire", ["Bookmarks"] = "Signets",
                ["AroundMe"] = "Autour de moi", ["PoiMap"] = "Carte POI", ["MapView"] = "Vue carte", ["Hi"] = "Bonjour", ["WelcomeBack"] = "Content de vous revoir",
                ["NoStories"] = "Aucune histoire", ["Start"] = "Commencer", ["Featured"] = "Vedette",
                ["Login"] = "Connexion", ["Logout"] = "Déconnexion", ["Purchases"] = "Bibliothèque audio", ["Stop"] = "Arrêt",
                ["Debug"] = "Console de débogage", ["ViewTour"] = "VOIR L'ITINÉRAIRE", ["Downloads"] = "Téléchargements",
                ["LiveRoute"] = "Itinéraire du Tour", ["NowPlaying"] = "En lecture", ["Stops"] = "arrêts", ["Open"] = "Ouvrir",
                ["Heatmap"] = "Carte thermique",
                ["ShowHeatmap"] = "Afficher les zones populaires",
                ["TrendingTitle"] = "🔥 Tendances",
                ["AudioGuide"] = "Guide audio",
                ["NowPlayingAudio"] = "Lecture du guide audio",
                ["Saved"] = "Enregistré",
                ["EditSpeechTextOwner"] = "MODIFIER LE TEXTE (OWNER)",
                ["ChooseLanguageDesc"] = "Sélectionnez une autre langue pour ajouter une traduction.",
                ["SaveContent"] = "ENREGISTRER",
                ["Saving"] = "Enregistrement...",
                ["ScanToShare"] = "SCANNER POUR PARTAGER",
                ["OrScanOtherPois"] = "Ou scannez d'autres POI",
                ["ChooseTtsLanguage"] = "Choisir la langue TTS",
                ["AuthSubtitle"] = "Profitez de visites audio gratuites.",
                ["EmailPlaceholder"] = "Adresse e-mail *",
                ["PasswordPlaceholder"] = "Mot de passe *",
                ["ShowText"] = "Afficher",
                ["HideText"] = "Masquer",
                ["QuickLoginOwner"] = "🚀 CONNEXION RAPIDE (OWNER)",
                ["ForgotPassword"] = "Mot de passe oublié?",
                ["DemoCredentials"] = "Démo: demo@example.com / Demo@123456",
                ["PopularNow"] = "Populaire",
                ["MyAccount"] = "Mon compte",
                ["EditProfile"] = "Modifier le profil",
                ["PreferencesLabel"] = "Préférences",
                ["UserPreferences"] = "Préférences utilisateur",
                ["BackupRestore"] = "Sauvegarde",
                ["BackupDesc"] = "Exporter en SQLite pour sauvegarde.",
                ["ExportDb"] = "Exporter",
                ["ImportDb"] = "Importer",
                ["SpeechTextPlaceholder"] = "Entrez le contenu...",
                ["HcmFoodTour"] = "🍲 Tournée culinaire à Ho Chi Minh",
                ["HanoiFoodTour"] = "🍜 Tournée culinaire à Hanoi"
            },
            ["ja"] = new() { 
                ["Explore"] = "探索", ["Map"] = "地図", ["Search"] = "検索",
                ["Profile"] = "マイプロフィール", ["History"] = "履歴", ["Bookmarks"] = "ブックマーク",
                ["AroundMe"] = "周辺", ["PoiMap"] = "POIマップ", ["MapView"] = "マップ表示", ["Hi"] = "こんにちは", ["WelcomeBack"] = "おかえりなさい",
                ["NoStories"] = "ストーリーなし", ["Start"] = "開始", ["Featured"] = "おすすめ",
                ["Login"] = "ログイン", ["Logout"] = "ログアウト", ["Purchases"] = "オーディオライブラリ", ["Stop"] = "スポット",
                ["Debug"] = "デバッグコンソール", ["ViewTour"] = "ルートを表示", ["Downloads"] = "ダウンロード",
                ["LiveRoute"] = "ライブ・ルート", ["NowPlaying"] = "再生中", ["Stops"] = "スポット", ["Open"] = "開く",
                ["Heatmap"] = "ヒートマップ",
                ["ShowHeatmap"] = "人気エリアを表示",
                ["TrendingTitle"] = "🔥 今人気のスポット",
                ["AudioGuide"] = "音声ガイド",
                ["NowPlayingAudio"] = "音声ガイドを再生中",
                ["Saved"] = "保存済み",
                ["EditSpeechTextOwner"] = "音声テキスト編集 (所有者)",
                ["ChooseLanguageDesc"] = "上のボタンから言語を選択して、新しい翻訳を追加できます。",
                ["SaveContent"] = "保存する",
                ["Saving"] = "保存中...",
                ["ScanToShare"] = "スキャンして共有",
                ["OrScanOtherPois"] = "または他のスポットをスキャン",
                ["ChooseTtsLanguage"] = "TTS言語の選択",
                ["AuthSubtitle"] = "無料の音声ガイドツアーをお楽しみください。",
                ["EmailPlaceholder"] = "メールアドレス *",
                ["PasswordPlaceholder"] = "パスワード *",
                ["ShowText"] = "表示",
                ["HideText"] = "非表示",
                ["QuickLoginOwner"] = "🚀 クイックログイン (所有者)",
                ["ForgotPassword"] = "パスワードをお忘れですか？",
                ["DemoCredentials"] = "デモ: demo@example.com / Demo@123456",
                ["PopularNow"] = "今人気",
                ["MyAccount"] = "マイアカウント",
                ["EditProfile"] = "プロフィール編集",
                ["PreferencesLabel"] = "設定",
                ["UserPreferences"] = "ユーザー設定",
                ["BackupRestore"] = "バックアップと復元",
                ["BackupDesc"] = "バックアップ用にSQLiteをエクスポートします。",
                ["ExportDb"] = "エクスポート",
                ["ImportDb"] = "インポート",
                ["SpeechTextPlaceholder"] = "内容を入力...",
                ["HcmFoodTour"] = "🍲 ホーチミン・フードツアー",
                ["HanoiFoodTour"] = "🍜 ハノイ・フードツアー"
            }
        };

        public void Init()
        {
            // Lấy ngôn ngữ đã lưu hoặc mặc định là Tiếng Việt
            bool hasUserSelectedLanguage = Preferences.ContainsKey(LanguageKey);
            string lang = NormalizeCode(Preferences.Get(LanguageKey, UserProfileService.PreferredLanguage));
            
            // Đăng ký lắng nghe sự kiện thay đổi Profile (trong đó có ngôn ngữ)
            UserProfileService.ProfileChanged += (s, e) => 
            {
                var profileLang = UserProfileService.PreferredLanguage;
                
                // GIẢI PHÁP: Nếu người dùng đã chủ động chọn ngôn ngữ trên App (đã lưu vào Preferences),
                // ta KHÔNG cho phép dữ liệu từ Profile Server (vốn có thể là mặc định 'en') ghi đè lên giao diện.
                // Chỉ thực hiện đồng bộ từ Profile nếu đây là lần đầu tiên sử dụng app (chưa có Preference).
                if (hasUserSelectedLanguage) return;

                if (!string.IsNullOrWhiteSpace(profileLang))
                {
                    var normalized = NormalizeCode(profileLang);
                    if (CurrentLanguage != normalized) 
                    {
                        SetLanguage(normalized);
                    }
                }
            };

            SetLanguage(lang);
        }

        public bool IsLoggedIn => TravelApp.Services.AuthStateService.IsLoggedIn;

        public void SetLanguage(string languageCode)
        {
            languageCode = NormalizeCode(languageCode);
            if (CurrentLanguage == languageCode && CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == languageCode) return;

            var culture = new CultureInfo(languageCode);
            
            // Thiết lập ngôn ngữ hệ thống cho luồng hiện tại
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // Lưu lựa chọn của người dùng vào bộ nhớ máy
            Preferences.Set(LanguageKey, languageCode);
            
            // Đồng bộ với UserProfileService để các ViewModel khác lấy đúng dữ liệu
            UserProfileService.PreferredLanguage = languageCode;

            // Thông báo cho UI cập nhật các thuộc tính Binding
            OnPropertyChanged(string.Empty); 
        }

        private string NormalizeCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "vi";
            var normalized = code.Split('-')[0].Split('_')[0].ToLower();
            return System.Array.IndexOf(SupportedLangs, normalized) >= 0 ? normalized : "vi";
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
                case "Tiếng Việt 🇻🇳": SetLanguage("vi"); break;
                case "English 🇺🇸": SetLanguage("en"); break;
                case "Français 🇫🇷": SetLanguage("fr"); break;
                case "日本語 🇯🇵": SetLanguage("ja"); break;
            }
        }

        public string CurrentLanguage => NormalizeCode(Preferences.Get(LanguageKey, "vi"));
        
        public string CurrentLanguageFlag => CurrentLanguage switch
        {
            "vi" => "🇻🇳",
            "en" => "🇺🇸",
            "fr" => "🇫🇷",
            "ja" => "🇯🇵",
            _ => "🌐"
        };

        public string Get(string key)
        {
            if (Translations.TryGetValue(CurrentLanguage, out var langDict) && langDict.TryGetValue(key, out var value))
                return value;
            
            // An toàn hơn: Nếu không tìm thấy trong ngôn ngữ hiện tại, thử tiếng Anh, nếu không thấy nữa thì trả về chính cái Key
            if (Translations["en"].TryGetValue(key, out var enValue)) return enValue;
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
        public string HcmFoodTourText => Get("HcmFoodTour");
        public string HanoiFoodTourText => Get("HanoiFoodTour");

        // Thêm các thuộc tính cho Menu
        public string PurchasesMenuText => Get("Purchases");
        
        // Đối với Login/Logout, bạn có thể thêm logic kiểm tra trạng thái đăng nhập ở đây nếu muốn
        // Ở đây tôi giả định trả về "Đăng nhập/Sign In" để menu hiện chữ lại đã
        public string AuthMenuText => IsLoggedIn ? Get("Logout") : Get("Login");
        public string ViewTourText => Get("ViewTour");
        public string DownloadsText => Get("Downloads");
        public string LiveRouteText => Get("LiveRoute");
        public string LoginText => Get("Login");
        public string LogoutText => Get("Logout");
        public string HiText => Get("Hi");
        public string WelcomeBackText => Get("WelcomeBack");
        public string NowPlayingText => Get("NowPlaying");
        public string StopsText => Get("Stops");
        public string StopText => Get("Stop");
        public string OpenText => Get("Open");
        public string HeatmapText => Get("Heatmap");
        public string ShowHeatmapText => Get("ShowHeatmap");
        public string TrendingTitle => Get("TrendingTitle");
        public string AudioGuideText => Get("AudioGuide");
        public string NowPlayingAudioText => Get("NowPlayingAudio");
        public string SavedText => Get("Saved");
        public string EditSpeechTextOwnerText => Get("EditSpeechTextOwner");
        public string ChooseLanguageDescText => Get("ChooseLanguageDesc");
        public string SaveContentText => Get("SaveContent");
        public string SavingText => Get("Saving");
        public string ScanToShareText => Get("ScanToShare");
        public string OrScanOtherPoisText => Get("OrScanOtherPois");
        public string ChooseTtsLanguageText => Get("ChooseTtsLanguage");
        public string AuthSubtitleText => Get("AuthSubtitle");
        public string EmailPlaceholderText => Get("EmailPlaceholder");
        public string PasswordPlaceholderText => Get("PasswordPlaceholder");
        public string ShowText => Get("ShowText");
        public string HideText => Get("HideText");
        public string QuickLoginOwnerText => Get("QuickLoginOwner");
        public string ForgotPasswordText => Get("ForgotPassword");
        public string DemoCredentialsText => Get("DemoCredentials");
        public string PopularNowText => Get("PopularNow");
        public string MyAccountText => Get("MyAccount");
        public string EditProfileText => Get("EditProfile");
        public string PreferencesLabelText => Get("PreferencesLabel");
        public string UserPreferencesText => Get("UserPreferences");
        public string BackupRestoreText => Get("BackupRestore");
        public string BackupDescText => Get("BackupDesc");
        public string ExportDbText => Get("ExportDb");
        public string ImportDbText => Get("ImportDb");
        public string SpeechTextPlaceholderText => Get("SpeechTextPlaceholder");


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

            var currentLang = Instance.CurrentLanguage; // Ví dụ: "vi-VN"

            // 1. Tìm bản dịch đúng ngôn ngữ hiện tại
            var match = localizations.FirstOrDefault(l => 
                string.Equals((string)l.LanguageCode, currentLang, StringComparison.OrdinalIgnoreCase));

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