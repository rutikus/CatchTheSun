using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatchTheSun.Data.Migrations
{
    /// <inheritdoc />
    public partial class StrengthenDataIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalculationResults_Calculations_CalculationId",
                table: "CalculationResults");

            migrationBuilder.DropForeignKey(
                name: "FK_CalculationResults_Variants_VariantId",
                table: "CalculationResults");

            migrationBuilder.DropForeignKey(
                name: "FK_Calculations_Sites_SiteId",
                table: "Calculations");

            migrationBuilder.DropIndex(
                name: "IX_CalculationCriteria_CalculationId_CriterionId",
                table: "CalculationCriteria");

            migrationBuilder.AlterColumn<decimal>(
                name: "Value",
                table: "CriterionComparisons",
                type: "numeric(29,28)",
                precision: 29,
                scale: 28,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(28,27)",
                oldPrecision: 28,
                oldScale: 27);

            migrationBuilder.AddColumn<Guid>(
                name: "SiteId",
                table: "CalculationResults",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                "DO $$ BEGIN " +
                "IF EXISTS (" +
                "SELECT 1 FROM \"CalculationResults\" r " +
                "JOIN \"Calculations\" c ON c.\"Id\" = r.\"CalculationId\" " +
                "JOIN \"Variants\" v ON v.\"Id\" = r.\"VariantId\" " +
                "WHERE c.\"SiteId\" <> v.\"SiteId\") THEN " +
                "RAISE EXCEPTION 'Cannot apply StrengthenDataIntegrity: calculation results reference variants from another site.'; " +
                "END IF; " +
                "IF EXISTS (" +
                "SELECT 1 FROM \"CriterionComparisons\" p " +
                "LEFT JOIN \"CalculationCriteria\" a ON a.\"CalculationId\" = p.\"CalculationId\" AND a.\"CriterionId\" = p.\"CriterionAId\" " +
                "LEFT JOIN \"CalculationCriteria\" b ON b.\"CalculationId\" = p.\"CalculationId\" AND b.\"CriterionId\" = p.\"CriterionBId\" " +
                "WHERE a.\"CriterionId\" IS NULL OR b.\"CriterionId\" IS NULL) THEN " +
                "RAISE EXCEPTION 'Cannot apply StrengthenDataIntegrity: a criterion comparison references a criterion outside its calculation.'; " +
                "END IF; END $$;");

            migrationBuilder.Sql(
                "UPDATE \"CalculationResults\" r " +
                "SET \"SiteId\" = c.\"SiteId\" " +
                "FROM \"Calculations\" c " +
                "WHERE c.\"Id\" = r.\"CalculationId\";");

            migrationBuilder.AlterColumn<Guid>(
                name: "SiteId",
                table: "CalculationResults",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Variants_Id_SiteId",
                table: "Variants",
                columns: new[] { "Id", "SiteId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Calculations_Id_SiteId",
                table: "Calculations",
                columns: new[] { "Id", "SiteId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_CalculationResults_Id_CalculationId",
                table: "CalculationResults",
                columns: new[] { "Id", "CalculationId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_CalculationCriteria_CalculationId_CriterionId",
                table: "CalculationCriteria",
                columns: new[] { "CalculationId", "CriterionId" });

            migrationBuilder.CreateTable(
                name: "CalculationResultCriterionSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CalculationResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    CalculationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValueSnapshot = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    DirectionSnapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TargetValueSnapshot = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    WeightSnapshot = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalculationResultCriterionSnapshots", x => x.Id);
                    table.UniqueConstraint("AK_CalculationResultCriterionSnapshots_CalculationResultId_Cri~", x => new { x.CalculationResultId, x.CriterionId });
                    table.ForeignKey(
                        name: "FK_CalculationResultCriterionSnapshots_CalculationCriteria_Cal~",
                        columns: x => new { x.CalculationId, x.CriterionId },
                        principalTable: "CalculationCriteria",
                        principalColumns: new[] { "CalculationId", "CriterionId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CalculationResultCriterionSnapshots_CalculationResults_Calc~",
                        columns: x => new { x.CalculationResultId, x.CalculationId },
                        principalTable: "CalculationResults",
                        principalColumns: new[] { "Id", "CalculationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalculationResults_CalculationId_SiteId",
                table: "CalculationResults",
                columns: new[] { "CalculationId", "SiteId" });

            migrationBuilder.CreateIndex(
                name: "IX_CalculationResults_VariantId_SiteId",
                table: "CalculationResults",
                columns: new[] { "VariantId", "SiteId" });

            migrationBuilder.CreateIndex(
                name: "IX_CalculationResultCriterionSnapshots_CalculationId_Criterion~",
                table: "CalculationResultCriterionSnapshots",
                columns: new[] { "CalculationId", "CriterionId" });

            migrationBuilder.CreateIndex(
                name: "IX_CalculationResultCriterionSnapshots_CalculationResultId_Cal~",
                table: "CalculationResultCriterionSnapshots",
                columns: new[] { "CalculationResultId", "CalculationId" });

            migrationBuilder.AddForeignKey(
                name: "FK_CalculationResults_Calculations_CalculationId_SiteId",
                table: "CalculationResults",
                columns: new[] { "CalculationId", "SiteId" },
                principalTable: "Calculations",
                principalColumns: new[] { "Id", "SiteId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CalculationResults_Variants_VariantId_SiteId",
                table: "CalculationResults",
                columns: new[] { "VariantId", "SiteId" },
                principalTable: "Variants",
                principalColumns: new[] { "Id", "SiteId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Calculations_Sites_SiteId",
                table: "Calculations",
                column: "SiteId",
                principalTable: "Sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CriterionComparisons_CalculationCriteria_CalculationId_Crit~",
                table: "CriterionComparisons",
                columns: new[] { "CalculationId", "CriterionAId" },
                principalTable: "CalculationCriteria",
                principalColumns: new[] { "CalculationId", "CriterionId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CriterionComparisons_CalculationCriteria_CalculationId_Cri~1",
                table: "CriterionComparisons",
                columns: new[] { "CalculationId", "CriterionBId" },
                principalTable: "CalculationCriteria",
                principalColumns: new[] { "CalculationId", "CriterionId" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalculationResults_Calculations_CalculationId_SiteId",
                table: "CalculationResults");

            migrationBuilder.DropForeignKey(
                name: "FK_CalculationResults_Variants_VariantId_SiteId",
                table: "CalculationResults");

            migrationBuilder.DropForeignKey(
                name: "FK_Calculations_Sites_SiteId",
                table: "Calculations");

            migrationBuilder.DropForeignKey(
                name: "FK_CriterionComparisons_CalculationCriteria_CalculationId_Crit~",
                table: "CriterionComparisons");

            migrationBuilder.DropForeignKey(
                name: "FK_CriterionComparisons_CalculationCriteria_CalculationId_Cri~1",
                table: "CriterionComparisons");

            migrationBuilder.DropTable(
                name: "CalculationResultCriterionSnapshots");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Variants_Id_SiteId",
                table: "Variants");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Calculations_Id_SiteId",
                table: "Calculations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_CalculationResults_Id_CalculationId",
                table: "CalculationResults");

            migrationBuilder.DropIndex(
                name: "IX_CalculationResults_CalculationId_SiteId",
                table: "CalculationResults");

            migrationBuilder.DropIndex(
                name: "IX_CalculationResults_VariantId_SiteId",
                table: "CalculationResults");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_CalculationCriteria_CalculationId_CriterionId",
                table: "CalculationCriteria");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "CalculationResults");

            migrationBuilder.AlterColumn<decimal>(
                name: "Value",
                table: "CriterionComparisons",
                type: "numeric(28,27)",
                precision: 28,
                scale: 27,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(29,28)",
                oldPrecision: 29,
                oldScale: 28);

            migrationBuilder.CreateIndex(
                name: "IX_CalculationCriteria_CalculationId_CriterionId",
                table: "CalculationCriteria",
                columns: new[] { "CalculationId", "CriterionId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CalculationResults_Calculations_CalculationId",
                table: "CalculationResults",
                column: "CalculationId",
                principalTable: "Calculations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CalculationResults_Variants_VariantId",
                table: "CalculationResults",
                column: "VariantId",
                principalTable: "Variants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Calculations_Sites_SiteId",
                table: "Calculations",
                column: "SiteId",
                principalTable: "Sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
