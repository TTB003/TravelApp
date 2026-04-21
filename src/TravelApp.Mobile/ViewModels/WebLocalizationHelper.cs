using System.Collections.Generic;

namespace TravelApp.Admin.Web.Helpers;

public static class WebLocalizationHelper
{
    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
    {
        ["vi"] = new() { 
            ["Explore"] = "Khám phá", ["Map"] = "Bản đồ", ["Search"] = "Tìm kiếm",
            ["Profile"] = "Hồ sơ của tôi", ["History"] = "Lịch sử", ["Bookmarks"] = "Đã lưu",
            ["AroundMe"] = "Xung quanh tôi", ["PoiMap"] = "Bản đồ POI", ["MapView"] = "Xem bản đồ",
            ["NoStories"] = "Không có câu chuyện nào", ["Start"] = "Bắt đầu", ["Featured"] = "Nổi bật",
            ["Login"] = "Đăng nhập", ["Logout"] = "Đăng xuất", ["Purchases"] = "Thư viện âm thanh",
            ["Debug"] = "Bảng điều khiển Debug", ["AudioGuide"] = "Thuyết Minh",
            ["ScanToShare"] = "Quét để xem chi tiết & chia sẻ", ["PopularTours"] = "Tour phổ biến"
        },
        ["en"] = new() { 
            ["Explore"] = "Explore", ["Map"] = "Map", ["Search"] = "Search",
            ["Profile"] = "My Profile", ["History"] = "History", ["Bookmarks"] = "Bookmarks",
            ["AroundMe"] = "Around me", ["PoiMap"] = "POI Map", ["MapView"] = "Map view",
            ["NoStories"] = "No Stories available", ["Start"] = "Start", ["Featured"] = "Featured",
            ["Login"] = "Sign In", ["Logout"] = "Sign Out", ["Purchases"] = "Audio Library",
            ["Debug"] = "Debug Console", ["AudioGuide"] = "Audio Guide",
            ["ScanToShare"] = "Scan to view details & share", ["PopularTours"] = "Popular Tours"
        },
        ["fr"] = new() { 
            ["Explore"] = "Explorer", ["Map"] = "Carte", ["Search"] = "Chercher",
            ["Profile"] = "Mon Profil", ["History"] = "Histoire", ["Bookmarks"] = "Signets",
            ["AroundMe"] = "Autour de moi", ["PoiMap"] = "Carte POI", ["MapView"] = "Vue carte",
            ["NoStories"] = "Aucune histoire", ["Start"] = "Commencer", ["Featured"] = "Vedette",
            ["Login"] = "Connexion", ["Logout"] = "Déconnexion", ["Purchases"] = "Bibliothèque audio",
            ["Debug"] = "Console de débogage", ["AudioGuide"] = "Thuyết Minh",
            ["ScanToShare"] = "Scanner pour voir et partager", ["PopularTours"] = "Tours populaires"
        },
        ["ja"] = new() { 
            ["Explore"] = "探索", ["Map"] = "地図", ["Search"] = "検索",
            ["Profile"] = "マイプロフィール", ["History"] = "履歴", ["Bookmarks"] = "ブックマーク",
            ["AroundMe"] = "周辺", ["PoiMap"] = "POIマップ", ["MapView"] = "マップ表示",
            ["NoStories"] = "ストーリーなし", ["Start"] = "開始", ["Featured"] = "おすすめ",
            ["Login"] = "ログイン", ["Logout"] = "ログアウト", ["Purchases"] = "オーディオライブラリ",
            ["Debug"] = "デバッグコンソール", ["AudioGuide"] = "音声ガイド",
            ["ScanToShare"] = "スキャンして詳細を表示 & 共有", ["PopularTours"] = "人気のツアー"
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