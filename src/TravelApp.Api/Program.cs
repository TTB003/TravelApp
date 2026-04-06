using TravelApp.Application;
using TravelApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Cấu hình Services (DI Container) ---

// Thêm hỗ trợ hiển thị danh sách API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers();

// Cho phép Mobile client (emulator/devices) truy cập API trong môi trường phát triển
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Đăng ký các Service từ các Project khác (Application & Infrastructure)
builder.Services.AddApplication();

// Lấy chuỗi kết nối từ appsettings.json
var connectionString = builder.Configuration.GetConnectionString("TravelAppDb")
    ?? throw new InvalidOperationException("Missing connection string 'TravelAppDb'.");

// Đăng ký Infrastructure với Database
builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

// --- 2. Cấu hình HTTP Request Pipeline (Middleware) ---

// Chỉ bật giao diện Swagger khi đang phát triển (Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TravelApp API v1");
        options.RoutePrefix = "swagger"; // Đường dẫn sẽ là /swagger
    });
}

app.UseHttpsRedirection();

// Enable CORS for requests from mobile apps / emulators
app.UseCors();

app.UseAuthorization();

app.MapControllers();

// Hàm kiểm tra nhanh trạng thái hệ thống
app.MapGet("/health", () => Results.Ok(new
{
    Status = "OK",
    Service = "TravelApp.Api",
    Time = DateTime.Now
}));

app.Run();