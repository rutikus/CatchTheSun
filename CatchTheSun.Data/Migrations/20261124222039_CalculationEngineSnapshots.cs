using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatchTheSun.Data.Migrations
{
    /// <inheritdoc />
    public partial class CalculationEngineSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlgorithmVersion",
                table: "Calculations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ConsistencyRatio",
                table: "Calculations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IterationCount",
                table: "Calculations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LambdaMax",
                table: "Calculations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OptionsJson",
                table: "Calculations",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalIdSnapshot",
                table: "CalculationResults",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ParetoSecondaryScore",
                table: "CalculationResults",
                type: "numeric(20,8)",
                precision: 20,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantNameSnapshot",
                table: "CalculationResults",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "WeightSnapshot",
                table: "CalculationResultCriterionSnapshots",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,8)",
                oldPrecision: 18,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "Weight",
                table: "CalculationCriteria",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,8)",
                oldPrecision: 18,
                oldScale: 8);

            migrationBuilder.AddColumn<bool>(
                name: "IsRequiredSnapshot",
                table: "CalculationCriteria",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameSnapshot",
                table: "CalculationCriteria",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitSnapshot",
                table: "CalculationCriteria",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "CalculationCriteria" AS cc
                SET "NameSnapshot" = c."Name",
                    "UnitSnapshot" = c."Unit",
                    "IsRequiredSnapshot" = c."IsRequired"
                FROM "Criteria" AS c
                WHERE cc."CriterionId" = c."Id";

                UPDATE "CalculationResults" AS cr
                SET "VariantNameSnapshot" = v."Name",
                    "ExternalIdSnapshot" = v."ExternalId"
                FROM "Variants" AS v
                WHERE cr."VariantId" = v."Id";
                """);

            migrationBuilder.AlterColumn<bool>(
                name: "IsRequiredSnapshot",
                table: "CalculationCriteria",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NameSnapshot",
                table: "CalculationCriteria",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "VariantNameSnapshot",
                table: "CalculationResults",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlgorithmVersion",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "ConsistencyRatio",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "IterationCount",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "LambdaMax",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "OptionsJson",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "ExternalIdSnapshot",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "ParetoSecondaryScore",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "VariantNameSnapshot",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "IsRequiredSnapshot",
                table: "CalculationCriteria");

            migrationBuilder.DropColumn(
                name: "NameSnapshot",
                table: "CalculationCriteria");

            migrationBuilder.DropColumn(
                name: "UnitSnapshot",
                table: "CalculationCriteria");

            migrationBuilder.AlterColumn<decimal>(
                name: "WeightSnapshot",
                table: "CalculationResultCriterionSnapshots",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,8)",
                oldPrecision: 18,
                oldScale: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Weight",
                table: "CalculationCriteria",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,8)",
                oldPrecision: 18,
                oldScale: 8,
                oldNullable: true);
        }
    }
}
