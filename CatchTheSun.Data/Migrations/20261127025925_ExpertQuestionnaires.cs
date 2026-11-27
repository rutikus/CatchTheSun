using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatchTheSun.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpertQuestionnaires : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Experts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnonymousCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ExperienceCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Experts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Questionnaires",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questionnaires", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpertResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionnaireId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpertId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpertResponses", x => x.Id);
                    table.UniqueConstraint("AK_ExpertResponses_Id_QuestionnaireId", x => new { x.Id, x.QuestionnaireId });
                    table.ForeignKey(
                        name: "FK_ExpertResponses_Experts_ExpertId",
                        column: x => x.ExpertId,
                        principalTable: "Experts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExpertResponses_Questionnaires_QuestionnaireId",
                        column: x => x.QuestionnaireId,
                        principalTable: "Questionnaires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionnaireCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionnaireId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionnaireCriteria", x => x.Id);
                    table.UniqueConstraint("AK_QuestionnaireCriteria_QuestionnaireId_CriterionId", x => new { x.QuestionnaireId, x.CriterionId });
                    table.CheckConstraint("CK_QuestionnaireCriteria_DisplayOrder", "\"DisplayOrder\" >= 0");
                    table.ForeignKey(
                        name: "FK_QuestionnaireCriteria_Criteria_CriterionId",
                        column: x => x.CriterionId,
                        principalTable: "Criteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionnaireCriteria_Questionnaires_QuestionnaireId",
                        column: x => x.QuestionnaireId,
                        principalTable: "Questionnaires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CriterionRankings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpertResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionnaireId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriterionRankings", x => x.Id);
                    table.CheckConstraint("CK_CriterionRankings_Rank", "\"Rank\" >= 1");
                    table.ForeignKey(
                        name: "FK_CriterionRankings_ExpertResponses_ExpertResponseId_Question~",
                        columns: x => new { x.ExpertResponseId, x.QuestionnaireId },
                        principalTable: "ExpertResponses",
                        principalColumns: new[] { "Id", "QuestionnaireId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CriterionRankings_QuestionnaireCriteria_QuestionnaireId_Cri~",
                        columns: x => new { x.QuestionnaireId, x.CriterionId },
                        principalTable: "QuestionnaireCriteria",
                        principalColumns: new[] { "QuestionnaireId", "CriterionId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PairwiseComparisons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpertResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionnaireId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstCriterionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecondCriterionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(29,28)", precision: 29, scale: 28, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PairwiseComparisons", x => x.Id);
                    table.CheckConstraint("CK_PairwiseComparisons_SaatyAndOrder", "\"FirstCriterionId\" < \"SecondCriterionId\" AND (\"Value\" IN (1,2,3,4,5,6,7,8,9) OR \"Value\" IN (0.5,0.3333333333333333333333333333,0.25,0.2,0.1666666666666666666666666667,0.1428571428571428571428571429,0.125,0.1111111111111111111111111111))");
                    table.ForeignKey(
                        name: "FK_PairwiseComparisons_ExpertResponses_ExpertResponseId_Questi~",
                        columns: x => new { x.ExpertResponseId, x.QuestionnaireId },
                        principalTable: "ExpertResponses",
                        principalColumns: new[] { "Id", "QuestionnaireId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PairwiseComparisons_QuestionnaireCriteria_QuestionnaireId_F~",
                        columns: x => new { x.QuestionnaireId, x.FirstCriterionId },
                        principalTable: "QuestionnaireCriteria",
                        principalColumns: new[] { "QuestionnaireId", "CriterionId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PairwiseComparisons_QuestionnaireCriteria_QuestionnaireId_S~",
                        columns: x => new { x.QuestionnaireId, x.SecondCriterionId },
                        principalTable: "QuestionnaireCriteria",
                        principalColumns: new[] { "QuestionnaireId", "CriterionId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CriterionRankings_ExpertResponseId_CriterionId",
                table: "CriterionRankings",
                columns: new[] { "ExpertResponseId", "CriterionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CriterionRankings_ExpertResponseId_QuestionnaireId",
                table: "CriterionRankings",
                columns: new[] { "ExpertResponseId", "QuestionnaireId" });

            migrationBuilder.CreateIndex(
                name: "IX_CriterionRankings_QuestionnaireId_CriterionId",
                table: "CriterionRankings",
                columns: new[] { "QuestionnaireId", "CriterionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpertResponses_ExpertId",
                table: "ExpertResponses",
                column: "ExpertId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpertResponses_QuestionnaireId_ExpertId",
                table: "ExpertResponses",
                columns: new[] { "QuestionnaireId", "ExpertId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Experts_AnonymousCode",
                table: "Experts",
                column: "AnonymousCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PairwiseComparisons_ExpertResponseId_FirstCriterionId_Secon~",
                table: "PairwiseComparisons",
                columns: new[] { "ExpertResponseId", "FirstCriterionId", "SecondCriterionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PairwiseComparisons_ExpertResponseId_QuestionnaireId",
                table: "PairwiseComparisons",
                columns: new[] { "ExpertResponseId", "QuestionnaireId" });

            migrationBuilder.CreateIndex(
                name: "IX_PairwiseComparisons_QuestionnaireId_FirstCriterionId",
                table: "PairwiseComparisons",
                columns: new[] { "QuestionnaireId", "FirstCriterionId" });

            migrationBuilder.CreateIndex(
                name: "IX_PairwiseComparisons_QuestionnaireId_SecondCriterionId",
                table: "PairwiseComparisons",
                columns: new[] { "QuestionnaireId", "SecondCriterionId" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireCriteria_CriterionId",
                table: "QuestionnaireCriteria",
                column: "CriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireCriteria_QuestionnaireId_DisplayOrder",
                table: "QuestionnaireCriteria",
                columns: new[] { "QuestionnaireId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questionnaires_Status",
                table: "Questionnaires",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CriterionRankings");

            migrationBuilder.DropTable(
                name: "PairwiseComparisons");

            migrationBuilder.DropTable(
                name: "ExpertResponses");

            migrationBuilder.DropTable(
                name: "QuestionnaireCriteria");

            migrationBuilder.DropTable(
                name: "Experts");

            migrationBuilder.DropTable(
                name: "Questionnaires");
        }
    }
}
