using System.Collections.Generic;

namespace TravelApp.Admin.Web.Helpers;

public static class WebLocalizationHelper
{
    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
    {
        ["vi"] = new() { 
            ["Explore"] = "Khám phá", ["Map"] = "Bản đồ", ["Search"] = "Tìm kiếm",
            ["Profile"] = "Hồ sơ của tôi", ["History"] = "Lịch sử", ["Bookmarks"] = "Đã lưu",
            ["AroundMe"] = "Xung quanh tôi", ["PoiMap"] = "Bản đồ POI", ["MapView"] = "Xem bản đồ", ["TrendingTitle"] = "🔥 Xu hướng hiện tại", ["PopularNow"] = "Phổ biến nhất",
            ["HcmFoodTour"] = " Tour ẩm thực Hồ Chí Minh", ["HanoiFoodTour"] = " Tour ẩm thực Hà Nội",
            ["NoStories"] = "Không có câu chuyện nào", ["Start"] = "Bắt đầu", ["Featured"] = "Nổi bật",
            ["Login"] = "Đăng nhập", ["Logout"] = "Đăng xuất", ["Purchases"] = "Thư viện âm thanh",
            ["Debug"] = "Bảng điều khiển Debug", ["AudioGuide"] = "Thuyết minh", ["ViewTour"] = "XEM LỘ TRÌNH TOUR",
            ["ScanToShare"] = "Quét để xem chi tiết & chia sẻ", ["PopularTours"] = "Tour phổ biến", ["Hi"] = "Xin chào", ["WelcomeBack"] = "Chào mừng bạn quay lại",
            ["OpenText"] = "Mở",
            ["AuthSubtitle"] = "Thưởng thức các tour thuyết minh miễn phí và tích lũy điểm thưởng.",
            ["EmailPlaceholder"] = "Nhập địa chỉ e-mail *", ["PasswordPlaceholder"] = "Mật khẩu *",
            ["QuickLoginOwner"] = "🚀 ĐĂNG NHẬP NHANH (OWNER)", ["ForgotPassword"] = "Quên mật khẩu?",
            ["MyAccount"] = "Tài khoản của tôi", ["EditProfile"] = "Chỉnh sửa hồ sơ",
            ["PreferencesLabel"] = "Tùy chỉnh", ["UserPreferences"] = "Tùy chọn người dùng",
            ["BackupRestore"] = "Sao lưu & Khôi phục", ["BackupDesc"] = "Xuất ra file SQLite để sao lưu.",
            ["SelectLanguage"] = "Chọn ngôn ngữ", ["NowPlayingAudio"] = "Đang phát thuyết minh",
            ["ExportDb"] = "Xuất dữ liệu", ["ImportDb"] = "Nhập dữ liệu",
            ["EditSpeechTextOwner"] = "CHỈNH SỬA THUYẾT MINH (OWNER)",
            ["ChooseLanguageDesc"] = "Chọn ngôn ngữ khác từ nút phía trên để thêm bản dịch mới.",
            ["SaveContent"] = "LƯU NỘI DUNG", ["Saving"] = "Đang lưu...", ["SpeechTextPlaceholder"] = "Nhập nội dung thuyết minh...", ["ChooseTtsLanguage"] = "Chọn ngôn ngữ TTS"
        },
        ["en"] = new() { 
            ["Explore"] = "Explore", ["Map"] = "Map", ["Search"] = "Search",
            ["Profile"] = "My Profile", ["History"] = "History", ["Bookmarks"] = "Bookmarks",
            ["AroundMe"] = "Around me", ["PoiMap"] = "POI Map", ["MapView"] = "Map view", ["TrendingTitle"] = "🔥 Trending Now", ["PopularNow"] = "Popular Now",
            ["HcmFoodTour"] = " Ho Chi Minh Food Tour", ["HanoiFoodTour"] = " Hanoi Food Tour",
            ["NoStories"] = "No Stories available", ["Start"] = "Start", ["Featured"] = "Featured",
            ["Login"] = "Sign In", ["Logout"] = "Sign Out", ["Purchases"] = "Audio Library",
            ["Debug"] = "Debug Console", ["AudioGuide"] = "Audio Guide", ["ViewTour"] = "VIEW TOUR ROUTE",
            ["ScanToShare"] = "Scan to view details & share", ["PopularTours"] = "Popular Tours", ["Hi"] = "Hi", ["WelcomeBack"] = "Welcome back",
            ["OpenText"] = "Open",
            ["AuthSubtitle"] = "Enjoy free audio guided tours and redeem credits for paid tours.",
            ["EmailPlaceholder"] = "Enter your e-mail address *", ["PasswordPlaceholder"] = "Password *",
            ["QuickLoginOwner"] = "🚀 QUICK LOGIN (OWNER)", ["ForgotPassword"] = "Forgot password?",
            ["MyAccount"] = "My Account", ["EditProfile"] = "Edit Profile",
            ["PreferencesLabel"] = "Preferences", ["UserPreferences"] = "User preferences",
            ["BackupRestore"] = "Backup & Restore", ["BackupDesc"] = "Export to SQLite file for backup.",
            ["SelectLanguage"] = "Select Language", ["NowPlayingAudio"] = "Playing audio guide",
            ["ExportDb"] = "Export database", ["ImportDb"] = "Import database",
            ["EditSpeechTextOwner"] = "EDIT SPEECH TEXT (OWNER)",
            ["ChooseLanguageDesc"] = "Select another language from the button above to add a new translation.",
            ["SaveContent"] = "SAVE CONTENT", ["Saving"] = "Saving...", ["SpeechTextPlaceholder"] = "Enter audio guide content...", ["ChooseTtsLanguage"] = "Choose TTS Language"
        },
        ["fr"] = new() { 
            ["Explore"] = "Explorer", ["Map"] = "Carte", ["Search"] = "Chercher",
            ["Profile"] = "Mon Profil", ["History"] = "Histoire", ["Bookmarks"] = "Signets",
            ["AroundMe"] = "Autour de moi", ["PoiMap"] = "Carte POI", ["MapView"] = "Vue carte",
            ["NoStories"] = "Aucune histoire", ["Start"] = "Commencer", ["Featured"] = "Vedette",
            ["Login"] = "Connexion", ["Logout"] = "Déconnexion", ["Purchases"] = "Bibliothèque audio",
            ["Debug"] = "Console de débogage", ["AudioGuide"] = "Guide audio", ["ViewTour"] = "VOIR L'ITINÉRAIRE",
            ["ScanToShare"] = "Scanner pour voir et partager", ["PopularTours"] = "Tours populaires", ["Hi"] = "Bonjour", ["WelcomeBack"] = "Content de vous revoir",
            ["AuthSubtitle"] = "Profitez de visites audio gratuites.",
            ["EmailPlaceholder"] = "Adresse e-mail *", ["PasswordPlaceholder"] = "Mot de passe *",
            ["QuickLoginOwner"] = "🚀 CONNEXION RAPIDE (OWNER)", ["ForgotPassword"] = "Mot de passe oublié?",
            ["MyAccount"] = "Mon compte", ["EditProfile"] = "Modifier le profil",
            ["PreferencesLabel"] = "Préférences", ["UserPreferences"] = "Préférences utilisateur",
            ["BackupRestore"] = "Sauvegarde", ["BackupDesc"] = "Exporter en SQLite cho sauvegarde.",
            ["SelectLanguage"] = "Choisir la langue", ["NowPlayingAudio"] = "Lecture du guide audio",
            ["ExportDb"] = "Exporter", ["ImportDb"] = "Importer",
            ["EditSpeechTextOwner"] = "MODIFIER LE TEXTE (OWNER)",
            ["ChooseLanguageDesc"] = "Sélectionnez une autre langue pour ajouter une traduction.",
            ["SaveContent"] = "ENREGISTRER", ["Saving"] = "Enregistrement...", ["SpeechTextPlaceholder"] = "Entrez le contenu...", ["ChooseTtsLanguage"] = "Choisir la langue TTS"
        },
        ["ja"] = new() { 
            ["Explore"] = "探索", ["Map"] = "地図", ["Search"] = "検索",
            ["Profile"] = "マイプロフィール", ["History"] = "履歴", ["Bookmarks"] = "ブックマーク",
            ["AroundMe"] = "周辺", ["PoiMap"] = "POIマップ", ["MapView"] = "マップ表示",
            ["NoStories"] = "ストーリーなし", ["Start"] = "開始", ["Featured"] = "おすすめ",
            ["Login"] = "ログイン", ["Logout"] = "ログアウト", ["Purchases"] = "オーディオライブラリ",
            ["Debug"] = "デバッグコンソール", ["AudioGuide"] = "音声ガイド", ["ViewTour"] = "ルートを表示",
            ["ScanToShare"] = "スキャンして詳細を表示 & 共有", ["PopularTours"] = "人気のツアー", ["Hi"] = "こんにちは", ["WelcomeBack"] = "おかえりなさい",
            ["AuthSubtitle"] = "無料の音声ガイドツアーをお楽しみください。",
            ["EmailPlaceholder"] = "メールアドレス *", ["PasswordPlaceholder"] = "パスワード *",
            ["QuickLoginOwner"] = "🚀 クイックログイン (所有者)", ["ForgotPassword"] = "パスワードをお忘れですか？",
            ["MyAccount"] = "マイアカウント", ["EditProfile"] = "プロフィール編集",
            ["PreferencesLabel"] = "設定", ["UserPreferences"] = "ユーザー設定",
            ["BackupRestore"] = "バックアップと復元", ["BackupDesc"] = "バックアップ用にSQLiteをエクスポートします。",
            ["SelectLanguage"] = "言語を選択", ["NowPlayingAudio"] = "音声ガイドを再生中",
            ["ExportDb"] = "エクスポート", ["ImportDb"] = "インポート",
            ["EditSpeechTextOwner"] = "音声テキスト編集 (所有者)",
            ["ChooseLanguageDesc"] = "上のボタンから言語を選択して、新しい翻訳を追加できます。",
            ["SaveContent"] = "保存する", ["Saving"] = "保存中...", ["SpeechTextPlaceholder"] = "内容を入力...", ["ChooseTtsLanguage"] = "TTS言語の選択"
        }
    };

    public static string Get(string key, string lang)
    {
        lang = lang.ToLower().Split('-')[0].Split('_')[0];
        if (Translations.TryGetValue(lang, out var langDict) && langDict.TryGetValue(key, out var value))
            return value;
        return Translations["en"].TryGetValue(key, out var enValue) ? enValue : key;
    }
}
