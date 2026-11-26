using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatchTheSun.Data.Migrations
{
    /// <inheritdoc />
    public partial class DirectSunImports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DirectSunImports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceCsv = table.Column<string>(type: "text", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectSunImports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DirectSunImports_Variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "Variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DirectSunMeasurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportId = table.Column<Guid>(type: "uuid", nullable: false),
                    SensorId = table.Column<Guid>(type: "uuid", nullable: false),
                    FeatureId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecreationalType = table.Column<string>(type: "text", nullable: false),
                    XMetres = table.Column<double>(type: "double precision", nullable: false),
                    YMetres = table.Column<double>(type: "double precision", nullable: false),
                    ZMetres = table.Column<double>(type: "double precision", nullable: false),
                    HeightAboveZoneMetres = table.Column<double>(type: "double precision", nullable: false),
                    GridSpacingMetres = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    DirectSunHoursTestDay = table.Column<double>(type: "double precision", nullable: false),
                    SunlitSamples = table.Column<int>(type: "integer", nullable: false),
                    SampleCountDaylight = table.Column<int>(type: "integer", nullable: false),
                    RawRow = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectSunMeasurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DirectSunMeasurements_DirectSunImports_ImportId",
                        column: x => x.ImportId,
                        principalTable: "DirectSunImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DirectSunImports_VariantId",
                table: "DirectSunImports",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectSunMeasurements_ImportId_FeatureId",
                table: "DirectSunMeasurements",
                columns: new[] { "ImportId", "FeatureId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DirectSunMeasurements");

            migrationBuilder.DropTable(
                name: "DirectSunImports");
        }
    }
}
