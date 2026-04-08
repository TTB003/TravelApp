using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelApp.Infrastructure.Persistence.Migrations;

public partial class AddToursAndTourPois : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Tours",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                AnchorPoiId = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                CoverImageUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                PrimaryLanguage = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                IsPublished = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tours", x => x.Id);
                table.ForeignKey(
                    name: "FK_Tours_POI_AnchorPoiId",
                    column: x => x.AnchorPoiId,
                    principalTable: "POI",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "TourPois",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                TourId = table.Column<int>(type: "int", nullable: false),
                PoiId = table.Column<int>(type: "int", nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false),
                DistanceFromPreviousMeters = table.Column<double>(type: "float", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TourPois", x => x.Id);
                table.ForeignKey(
                    name: "FK_TourPois_POI_PoiId",
                    column: x => x.PoiId,
                    principalTable: "POI",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_TourPois_Tours_TourId",
                    column: x => x.TourId,
                    principalTable: "Tours",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.InsertData(
            table: "Tours",
            columns: new[] { "Id", "AnchorPoiId", "Name", "Description", "CoverImageUrl", "PrimaryLanguage", "IsPublished", "CreatedAtUtc", "UpdatedAtUtc" },
            values: new object[,]
            {
                { 1, 1, "HCM Food Tour", "Tour ẩm thực Sài Gòn với các điểm dừng được sắp xếp theo lộ trình thật.", "https://placehold.co/1200x800/png?text=HCM+Food+Tour", "vi", true, new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), null },
                { 2, 4, "Hanoi Food Tour", "Tour ẩm thực Hà Nội với các mốc waypoint, bản đồ và audio tự động.", "https://placehold.co/1200x800/png?text=Hanoi+Food+Tour", "vi", true, new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), null }
            });

        migrationBuilder.InsertData(
            table: "TourPois",
            columns: new[] { "Id", "TourId", "PoiId", "SortOrder", "DistanceFromPreviousMeters" },
            values: new object[,]
            {
                { 1, 1, 1, 1, 0d },
                { 2, 1, 2, 2, 900d },
                { 3, 1, 3, 3, 1100d },
                { 4, 2, 4, 1, 0d },
                { 5, 2, 5, 2, 300d },
                { 6, 2, 6, 3, 500d }
            });

        migrationBuilder.CreateIndex(
            name: "IX_Tours_AnchorPoiId",
            table: "Tours",
            column: "AnchorPoiId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TourPois_PoiId",
            table: "TourPois",
            column: "PoiId");

        migrationBuilder.CreateIndex(
            name: "IX_TourPois_TourId_SortOrder",
            table: "TourPois",
            columns: new[] { "TourId", "SortOrder" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "TourPois");

        migrationBuilder.DropTable(
            name: "Tours");
    }
}
