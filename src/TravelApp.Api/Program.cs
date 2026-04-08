using TravelApp.Application;
using TravelApp.Infrastructure;
using TravelApp.Infrastructure.Persistence;
using TravelApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddApplication();

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "your-secret-key-must-be-at-least-32-characters-long";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TravelApp";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TravelAppUsers";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("TravelAppDb")
    ?? throw new InvalidOperationException("Missing connection string 'TravelAppDb'.");

builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TravelAppDbContext>();

    if (ShouldBaselineLegacyDatabase(dbContext))
    {
        SeedLegacyMigrationHistory(dbContext);
    }

    dbContext.Database.Migrate();
    await EnsureTourSchemaAsync(dbContext);
    await EnsurePoiSpeechTextColumnAsync(dbContext);
    await ProgramStartupHelpers.EnsureSeedPoisAsync(dbContext);

    if (app.Environment.IsDevelopment())
    {
        await ProgramStartupHelpers.EnsureDemoLoginUsersAsync(dbContext);
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    Status = "OK",
    Service = "TravelApp.Api"
}));

static bool ShouldBaselineLegacyDatabase(TravelAppDbContext dbContext)
{
    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;

    if (shouldClose)
    {
        connection.Open();
    }

    try
    {
        using var poiCommand = connection.CreateCommand();
        poiCommand.CommandText = "SELECT OBJECT_ID(N'[POI]')";
        var poiObjectId = poiCommand.ExecuteScalar();

        if (poiObjectId is null or DBNull)
        {
            return false;
        }

        using var historyCommand = connection.CreateCommand();
        historyCommand.CommandText = "SELECT CASE WHEN OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM [__EFMigrationsHistory]) END";
        return Convert.ToInt32(historyCommand.ExecuteScalar()) == 0;
    }
    finally
    {
        if (shouldClose)
        {
            connection.Close();
        }
    }
}

static void SeedLegacyMigrationHistory(TravelAppDbContext dbContext)
{
    dbContext.Database.ExecuteSqlRaw("""
        IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
        BEGIN
            CREATE TABLE [__EFMigrationsHistory] (
                [MigrationId] nvarchar(150) NOT NULL,
                [ProductVersion] nvarchar(32) NOT NULL,
                CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
            );
        END;

        IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260331040844_InitialCreate')
            INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260331040844_InitialCreate', '10.0.0');

        IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260401000000_SeedFoodTours')
            INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260401000000_SeedFoodTours', '10.0.0');

        IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260402000000_SeedUsers')
            INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260402000000_SeedUsers', '10.0.0');

        IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260406000000_FixSeedUserPasswordHashes')
            INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260406000000_FixSeedUserPasswordHashes', '10.0.0');

        IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260406190000_AddToursAndTourPois')
            INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260406190000_AddToursAndTourPois', '10.0.0');
        """);
}

static async Task EnsureTourSchemaAsync(TravelAppDbContext dbContext)
{
    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;

    if (shouldClose)
    {
        await connection.OpenAsync();
    }

    try
    {
        using var tourCommand = connection.CreateCommand();
        tourCommand.CommandText = "SELECT OBJECT_ID(N'[Tours]')";
        var toursObjectId = await tourCommand.ExecuteScalarAsync();

        using var tourPoisCommand = connection.CreateCommand();
        tourPoisCommand.CommandText = "SELECT OBJECT_ID(N'[TourPois]')";
        var tourPoisObjectId = await tourPoisCommand.ExecuteScalarAsync();

        if (toursObjectId is not null and not DBNull && tourPoisObjectId is not null and not DBNull)
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[Tours]') IS NULL
            BEGIN
                CREATE TABLE [Tours] (
                    [Id] int NOT NULL IDENTITY,
                    [AnchorPoiId] int NOT NULL,
                    [Name] nvarchar(256) NOT NULL,
                    [Description] nvarchar(4000) NOT NULL,
                    [CoverImageUrl] nvarchar(1024) NULL,
                    [PrimaryLanguage] nvarchar(10) NOT NULL,
                    [IsPublished] bit NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    CONSTRAINT [PK_Tours] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Tours_POI_AnchorPoiId] FOREIGN KEY ([AnchorPoiId]) REFERENCES [POI] ([Id]) ON DELETE NO ACTION
                );
            END;

            IF OBJECT_ID(N'[TourPois]') IS NULL
            BEGIN
                CREATE TABLE [TourPois] (
                    [Id] int NOT NULL IDENTITY,
                    [TourId] int NOT NULL,
                    [PoiId] int NOT NULL,
                    [SortOrder] int NOT NULL,
                    [DistanceFromPreviousMeters] float NULL,
                    CONSTRAINT [PK_TourPois] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_TourPois_POI_PoiId] FOREIGN KEY ([PoiId]) REFERENCES [POI] ([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_TourPois_Tours_TourId] FOREIGN KEY ([TourId]) REFERENCES [Tours] ([Id]) ON DELETE CASCADE
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM [Tours] WHERE [Id] IN (1, 2))
            BEGIN
                SET IDENTITY_INSERT [Tours] ON;
                INSERT INTO [Tours] ([Id], [AnchorPoiId], [Name], [Description], [CoverImageUrl], [PrimaryLanguage], [IsPublished], [CreatedAtUtc], [UpdatedAtUtc])
                VALUES
                    (1, 1, N'HCM Food Tour', N'Tour ẩm thực Sài Gòn với các điểm dừng được sắp xếp theo lộ trình thật.', N'https://placehold.co/1200x800/png?text=HCM+Food+Tour', N'vi', 1, '2025-01-01T00:00:00+00:00', NULL),
                    (2, 4, N'Hanoi Food Tour', N'Tour ẩm thực Hà Nội với các mốc waypoint, bản đồ và audio tự động.', N'https://placehold.co/1200x800/png?text=Hanoi+Food+Tour', N'vi', 1, '2025-01-01T00:00:00+00:00', NULL);
                SET IDENTITY_INSERT [Tours] OFF;
            END;

            IF NOT EXISTS (SELECT 1 FROM [TourPois] WHERE [Id] IN (1, 2, 3, 4, 5, 6))
            BEGIN
                SET IDENTITY_INSERT [TourPois] ON;
                INSERT INTO [TourPois] ([Id], [TourId], [PoiId], [SortOrder], [DistanceFromPreviousMeters])
                VALUES
                    (1, 1, 1, 1, 0),
                    (2, 1, 2, 2, 900),
                    (3, 1, 3, 3, 1100),
                    (4, 2, 4, 1, 0),
                    (5, 2, 5, 2, 300),
                    (6, 2, 6, 3, 500);
                SET IDENTITY_INSERT [TourPois] OFF;
            END;

            IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
            BEGIN
                CREATE TABLE [__EFMigrationsHistory] (
                    [MigrationId] nvarchar(150) NOT NULL,
                    [ProductVersion] nvarchar(32) NOT NULL,
                    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260406190000_AddToursAndTourPois')
                INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260406190000_AddToursAndTourPois', '10.0.0');
            """, cancellationToken: default);
    }
    finally
    {
        if (shouldClose)
        {
            await connection.CloseAsync();
        }
    }
}

static async Task EnsurePoiSpeechTextColumnAsync(TravelAppDbContext dbContext)
{
    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;

    if (shouldClose)
    {
        await connection.OpenAsync();
    }

    try
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'POI' AND COLUMN_NAME = 'SpeechText'";
        var exists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;

        if (exists)
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE [POI] ADD [SpeechText] nvarchar(4000) NULL;");
    }
    finally
    {
        if (shouldClose)
        {
            await connection.CloseAsync();
        }
    }
}

app.Run();
