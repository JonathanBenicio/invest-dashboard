using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MarkFixedIncomeMaturity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "matured_at_utc",
                table: "asset_positions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_asset_positions_maturity_scan",
                table: "asset_positions",
                columns: new[] { "asset_type", "matured_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_asset_positions_maturity_scan",
                table: "asset_positions");

            migrationBuilder.DropColumn(
                name: "matured_at_utc",
                table: "asset_positions");
        }
    }
}
