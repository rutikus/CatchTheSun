using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatchTheSun.Data.Migrations
{
    /// <inheritdoc />
    public partial class LadybugSensorIdsAsText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SensorId",
                table: "DirectSunMeasurements",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_DirectSunMeasurements_ImportId_SensorId",
                table: "DirectSunMeasurements",
                columns: new[] { "ImportId", "SensorId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DirectSunMeasurements_ImportId_SensorId",
                table: "DirectSunMeasurements");

            migrationBuilder.AlterColumn<Guid>(
                name: "SensorId",
                table: "DirectSunMeasurements",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
