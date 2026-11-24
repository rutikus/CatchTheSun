using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatchTheSun.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Criteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TargetValue = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Criteria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Area = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Calculations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calculations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Calculations_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Variants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Variants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Variants_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalculationCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CalculationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    DirectionSnapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TargetValueSnapshot = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalculationCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalculationCriteria_Calculations_CalculationId",
                        column: x => x.CalculationId,
                        principalTable: "Calculations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CalculationCriteria_Criteria_CriterionId",
                        column: x => x.CriterionId,
                        principalTable: "Criteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CriterionComparisons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CalculationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionAId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionBId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(28,27)", precision: 28, scale: 27, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriterionComparisons", x => x.Id);
                    table.CheckConstraint("CK_CriterionComparisons_ValidPairAndSaatyValue", "\"CriterionAId\" < \"CriterionBId\" AND (\"Value\" IN (1, 2, 3, 4, 5, 6, 7, 8, 9) OR \"Value\" IN (0.5, 0.3333333333333333333333333333, 0.25, 0.2, 0.1666666666666666666666666667, 0.1428571428571428571428571429, 0.125, 0.1111111111111111111111111111))");
                    table.ForeignKey(
                        name: "FK_CriterionComparisons_Calculations_CalculationId",
                        column: x => x.CalculationId,
                        principalTable: "Calculations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CriterionComparisons_Criteria_CriterionAId",
                        column: x => x.CriterionAId,
                        principalTable: "Criteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CriterionComparisons_Criteria_CriterionBId",
                        column: x => x.CriterionBId,
                        principalTable: "Criteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CalculationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CalculationId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: true),
                    Rank = table.Column<int>(type: "integer", nullable: true),
                    IsParetoOptimal = table.Column<bool>(type: "boolean", nullable: true),
                    ParetoFront = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalculationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalculationResults_Calculations_CalculationId",
                        column: x => x.CalculationId,
                        principalTable: "Calculations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CalculationResults_Variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "Variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CriterionValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriterionValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CriterionValues_Criteria_CriterionId",
                        column: x => x.CriterionId,
                        principalTable: "Criteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CriterionValues_Variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "Variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalculationCriteria_CalculationId_CriterionId",
                table: "CalculationCriteria",
                columns: new[] { "CalculationId", "CriterionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalculationCriteria_CriterionId",
                table: "CalculationCriteria",
                column: "CriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_CalculationResults_CalculationId_VariantId",
                table: "CalculationResults",
                columns: new[] { "CalculationId", "VariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalculationResults_VariantId",
                table: "CalculationResults",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_SiteId",
                table: "Calculations",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_CriterionComparisons_CalculationId_CriterionAId_CriterionBId",
                table: "CriterionComparisons",
                columns: new[] { "CalculationId", "CriterionAId", "CriterionBId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CriterionComparisons_CalculationId_CriterionBId",
                table: "CriterionComparisons",
                columns: new[] { "CalculationId", "CriterionBId" });

            migrationBuilder.CreateIndex(
                name: "IX_CriterionComparisons_CriterionAId",
                table: "CriterionComparisons",
                column: "CriterionAId");

            migrationBuilder.CreateIndex(
                name: "IX_CriterionComparisons_CriterionBId",
                table: "CriterionComparisons",
                column: "CriterionBId");

            migrationBuilder.CreateIndex(
                name: "IX_CriterionValues_CriterionId",
                table: "CriterionValues",
                column: "CriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_CriterionValues_VariantId_CriterionId",
                table: "CriterionValues",
                columns: new[] { "VariantId", "CriterionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Variants_SiteId",
                table: "Variants",
                column: "SiteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalculationCriteria");

            migrationBuilder.DropTable(
                name: "CalculationResults");

            migrationBuilder.DropTable(
                name: "CriterionComparisons");

            migrationBuilder.DropTable(
                name: "CriterionValues");

            migrationBuilder.DropTable(
                name: "Calculations");

            migrationBuilder.DropTable(
                name: "Criteria");

            migrationBuilder.DropTable(
                name: "Variants");

            migrationBuilder.DropTable(
                name: "Sites");
        }
    }
}
