using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatchTheSun.Data.Migrations
{
    /// <inheritdoc />
    public partial class InsolationComparisonTestSourceConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_InsolationComparisons_TestSource",
                table: "InsolationComparisons",
                sql: "(\"TestDirectSunImportId\" IS NOT NULL AND \"TestUrbanSketchImportId\" IS NULL) OR (\"TestDirectSunImportId\" IS NULL AND \"TestUrbanSketchImportId\" IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_InsolationComparisons_TestSource",
                table: "InsolationComparisons");
        }
    }
}
