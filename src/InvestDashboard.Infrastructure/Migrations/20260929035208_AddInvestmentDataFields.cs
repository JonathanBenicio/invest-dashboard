using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestmentDataFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "idempotency_key",
                table: "transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "realized_cost_basis",
                table: "transactions",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "realized_gain",
                table: "transactions",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "portfolios",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "realized_cost_basis",
                table: "portfolios",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "realized_gain",
                table: "portfolios",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "is_adjusted",
                table: "historical_prices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "historical_prices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "legacy");

            migrationBuilder.AddColumn<string>(
                name: "issuer",
                table: "assets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                defaultValue: "Desconhecido");

            migrationBuilder.AddColumn<string>(
                name: "subtype",
                table: "assets",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "OUTRO");

            migrationBuilder.AddColumn<DateTime>(
                name: "purchase_date_utc",
                table: "asset_positions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_transactions_user_idempotency_key",
                table: "transactions",
                columns: new[] { "user_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_transactions_user_idempotency_key",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "realized_cost_basis",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "realized_gain",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "description",
                table: "portfolios");

            migrationBuilder.DropColumn(
                name: "realized_cost_basis",
                table: "portfolios");

            migrationBuilder.DropColumn(
                name: "realized_gain",
                table: "portfolios");

            migrationBuilder.DropColumn(
                name: "is_adjusted",
                table: "historical_prices");

            migrationBuilder.DropColumn(
                name: "source",
                table: "historical_prices");

            migrationBuilder.DropColumn(
                name: "issuer",
                table: "assets");

            migrationBuilder.DropColumn(
                name: "subtype",
                table: "assets");

            migrationBuilder.DropColumn(
                name: "purchase_date_utc",
                table: "asset_positions");
        }
    }
}
