using Microsoft.Extensions.DependencyInjection;
using TravelApp.Mobile.Services;

namespace TravelApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            
            // Thiết lập MainPage trước khi Init để LocalizationManager có đối tượng để cập nhật
            MainPage = MauiProgram.Services.GetRequiredService<AppShell>();
            
            // Khởi tạo ngôn ngữ ngay khi vào app
            LocalizationManager.Instance.Init();
        }
    }
}