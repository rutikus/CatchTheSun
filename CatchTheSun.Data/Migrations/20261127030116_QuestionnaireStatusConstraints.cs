using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatchTheSun.Data.Migrations
{
    /// <inheritdoc />
    public partial class QuestionnaireStatusConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Questionnaires_PublicationDates",
                table: "Questionnaires",
                sql: "(\"Status\" = 'Draft' AND \"PublishedAt\" IS NULL AND \"ClosedAt\" IS NULL) OR (\"Status\" = 'Published' AND \"PublishedAt\" IS NOT NULL AND \"ClosedAt\" IS NULL) OR (\"Status\" = 'Closed' AND \"PublishedAt\" IS NOT NULL AND \"ClosedAt\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Questionnaires_Status",
                table: "Questionnaires",
                sql: "\"Status\" IN ('Draft','Published','Closed')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ExpertResponses_Status",
                table: "ExpertResponses",
                sql: "\"Status\" = 'Submitted'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Questionnaires_PublicationDates",
                table: "Questionnaires");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Questionnaires_Status",
                table: "Questionnaires");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ExpertResponses_Status",
                table: "ExpertResponses");
        }
    }
}
