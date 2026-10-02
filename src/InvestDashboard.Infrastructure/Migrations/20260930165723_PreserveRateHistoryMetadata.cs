using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PreserveRateHistoryMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "new_periodicity",
                table: "economic_rate_history",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "new_unit",
                table: "economic_rate_history",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "previous_periodicity",
                table: "economic_rate_history",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "previous_unit",
                table: "economic_rate_history",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "new_periodicity",
                table: "economic_rate_history");

            migrationBuilder.DropColumn(
                name: "new_unit",
                table: "economic_rate_history");

            migrationBuilder.DropColumn(
                name: "previous_periodicity",
                table: "economic_rate_history");

            migrationBuilder.DropColumn(
                name: "previous_unit",
                table: "economic_rate_history");
        }
    }
}
