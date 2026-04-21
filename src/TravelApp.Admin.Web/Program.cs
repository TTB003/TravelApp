using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using TravelApp.Admin.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter());
});

builder.WebHost.ConfigureKestrel(options =>
{
    // Lắng nghe trên cổng 7020 ở tất cả các card mạng (cho phép điện thoại truy cập)
    options.ListenAnyIP(7020);
});

// Add lightweight POI API service for public web pages
builder.Services.AddHttpClient<TravelApp.Admin.Web.Services.IPoiApiService, TravelApp.Admin.Web.Services.PoiApiService>((sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TravelAppApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
}).ConfigurePrimaryHttpMessageHandler(sp =>
{
    return new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Public/Login"; // Chuyển hướng về trang Login mobile-web khi chưa đăng nhập
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Public/Login";
        options.Cookie.Name = "TravelApp.Admin.Auth";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.Configure<AdminCredentialsOptions>(builder.Configuration.GetSection("AdminCredentials"));

builder.Services.Configure<TravelAppApiOptions>(builder.Configuration.GetSection("TravelAppApi"));
builder.Services.AddHttpClient<ITravelAppApiClient, TravelAppApiClient>((sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TravelAppApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
}).ConfigurePrimaryHttpMessageHandler(sp =>
{
    // In development accept self-signed certs
    return new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
});

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Tạm thời comment dòng này nếu bạn truy cập qua IP nội bộ (http) 
    // để tránh bị ép sang https (cổng 443) khi chưa có chứng chỉ.
    // app.UseHttpsRedirection(); 
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Route dành cho quản trị viên
app.MapControllerRoute(
    name: "admin",
    pattern: "admin/{controller}/{action=Index}/{id?}",
    constraints: new { controller = "(Home|Pois|Tours|Users|Admin|Dashboard|Auth)" });

// Route mặc định dành cho giao diện Mobile Web (Public)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Public}/{action=Index}/{id?}");


app.Run();
