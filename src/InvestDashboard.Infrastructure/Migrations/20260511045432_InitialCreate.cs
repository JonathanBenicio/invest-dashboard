using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    asset_type = table.Column<string>(type: "text", nullable: false),
                    current_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    last_updated_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    network = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    segment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    indexer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    interest_rate = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    maturity_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sector = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "portfolios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portfolios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "historical_prices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historical_prices", x => x.id);
                    table.ForeignKey(
                        name: "FK_historical_prices_assets_asset_id",
                        column: x => x.asset_id,
                        principalTable: "assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    brokerage_fee = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    transaction_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_transactions_assets_asset_id",
                        column: x => x.asset_id,
                        principalTable: "assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "asset_positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    portfolio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    asset_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    average_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    current_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_positions", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_positions_assets_asset_id",
                        column: x => x.asset_id,
                        principalTable: "assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asset_positions_portfolios_portfolio_id",
                        column: x => x.portfolio_id,
                        principalTable: "portfolios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_asset_positions_asset_id",
                table: "asset_positions",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "idx_asset_positions_portfolio_id",
                table: "asset_positions",
                column: "portfolio_id");

            migrationBuilder.CreateIndex(
                name: "IX_assets_ticker",
                table: "assets",
                column: "ticker",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_historical_prices_asset_date",
                table: "historical_prices",
                columns: new[] { "asset_id", "date" });

            migrationBuilder.CreateIndex(
                name: "idx_portfolios_user_id",
                table: "portfolios",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_transactions_user_id",
                table: "transactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_asset_id",
                table: "transactions",
                column: "asset_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asset_positions");

            migrationBuilder.DropTable(
                name: "historical_prices");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropTable(
                name: "portfolios");

            migrationBuilder.DropTable(
                name: "assets");
        }
    }
}
