using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatchTheSun.Data.Migrations
{
    /// <inheritdoc />
    public partial class InsolationComparisons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InsolationComparisons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceDirectSunImportId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceUrbanSketchImportId = table.Column<Guid>(type: "uuid", nullable: true),
                    TestDirectSunImportId = table.Column<Guid>(type: "uuid", nullable: true),
                    TestUrbanSketchImportId = table.Column<Guid>(type: "uuid", nullable: true),
                    Mae = table.Column<double>(type: "double precision", nullable: true),
                    Rmse = table.Column<double>(type: "double precision", nullable: true),
                    Mape = table.Column<double>(type: "double precision", nullable: true),
                    ComparedPoints = table.Column<int>(type: "integer", nullable: false),
                    ReferencePoints = table.Column<int>(type: "integer", nullable: false),
                    TestPoints = table.Column<int>(type: "integer", nullable: false),
                    UnmatchedReferencePoints = table.Column<int>(type: "integer", nullable: false),
                    UnmatchedTestPoints = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsolationComparisons", x => x.Id);
                    table.CheckConstraint("CK_InsolationComparisons_ReferenceSource", "(\"ReferenceDirectSunImportId\" IS NOT NULL AND \"ReferenceUrbanSketchImportId\" IS NULL) OR (\"ReferenceDirectSunImportId\" IS NULL AND \"ReferenceUrbanSketchImportId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_InsolationComparisons_DirectSunImports_ReferenceDirectSunIm~",
                        column: x => x.ReferenceDirectSunImportId,
                        principalTable: "DirectSunImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InsolationComparisons_DirectSunImports_TestDirectSunImportId",
                        column: x => x.TestDirectSunImportId,
                        principalTable: "DirectSunImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InsolationComparisons_UrbanSketchImports_ReferenceUrbanSket~",
                        column: x => x.ReferenceUrbanSketchImportId,
                        principalTable: "UrbanSketchImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InsolationComparisons_UrbanSketchImports_TestUrbanSketchImp~",
                        column: x => x.TestUrbanSketchImportId,
                        principalTable: "UrbanSketchImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InsolationComparisons_Variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "Variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InsolationComparisonPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InsolationComparisonId = table.Column<Guid>(type: "uuid", nullable: false),
                    SensorId = table.Column<string>(type: "text", nullable: false),
                    ReferenceValue = table.Column<double>(type: "double precision", nullable: false),
                    TestValue = table.Column<double>(type: "double precision", nullable: false),
                    AbsoluteError = table.Column<double>(type: "double precision", nullable: false),
                    RelativeError = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsolationComparisonPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsolationComparisonPoints_InsolationComparisons_Insolation~",
                        column: x => x.InsolationComparisonId,
                        principalTable: "InsolationComparisons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InsolationComparisonPoints_InsolationComparisonId_SensorId",
                table: "InsolationComparisonPoints",
                columns: new[] { "InsolationComparisonId", "SensorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsolationComparisons_ReferenceDirectSunImportId",
                table: "InsolationComparisons",
                column: "ReferenceDirectSunImportId");

            migrationBuilder.CreateIndex(
                name: "IX_InsolationComparisons_ReferenceUrbanSketchImportId",
                table: "InsolationComparisons",
                column: "ReferenceUrbanSketchImportId");

            migrationBuilder.CreateIndex(
                name: "IX_InsolationComparisons_TestDirectSunImportId",
                table: "InsolationComparisons",
                column: "TestDirectSunImportId");

            migrationBuilder.CreateIndex(
                name: "IX_InsolationComparisons_TestUrbanSketchImportId",
                table: "InsolationComparisons",
                column: "TestUrbanSketchImportId");

            migrationBuilder.CreateIndex(
                name: "IX_InsolationComparisons_VariantId_CreatedAt",
                table: "InsolationComparisons",
                columns: new[] { "VariantId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InsolationComparisonPoints");

            migrationBuilder.DropTable(
                name: "InsolationComparisons");
        }
    }
}
