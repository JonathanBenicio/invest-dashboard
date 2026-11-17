using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTreasuryImportBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tesouro_import_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    portfolio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference_date = table.Column<DateOnly>(type: "date", nullable: false),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    format = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    preview_json = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    state = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    confirmed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tesouro_import_batches", x => x.id);
                    table.UniqueConstraint("AK_tesouro_import_batches_id_portfolio_id", x => new { x.id, x.portfolio_id });
                    table.ForeignKey(
                        name: "FK_tesouro_import_batches_portfolios_portfolio_id",
                        column: x => x.portfolio_id,
                        principalTable: "portfolios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "treasury_position_bases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    portfolio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference_date = table.Column<DateOnly>(type: "date", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treasury_position_bases", x => x.id);
                    table.ForeignKey(
                        name: "FK_treasury_position_bases_assets_asset_id",
                        column: x => x.asset_id,
                        principalTable: "assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_treasury_position_bases_portfolios_portfolio_id",
                        column: x => x.portfolio_id,
                        principalTable: "portfolios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_treasury_position_bases_tesouro_import_batches_batch_id_por~",
                        columns: x => new { x.batch_id, x.portfolio_id },
                        principalTable: "tesouro_import_batches",
                        principalColumns: new[] { "id", "portfolio_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_tesouro_import_batch_idempotency",
                table: "tesouro_import_batches",
                columns: new[] { "portfolio_id", "content_hash", "reference_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_treasury_position_bases_asset_id",
                table: "treasury_position_bases",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_treasury_position_bases_batch_id_portfolio_id",
                table: "treasury_position_bases",
                columns: new[] { "batch_id", "portfolio_id" });

            migrationBuilder.CreateIndex(
                name: "ux_treasury_position_base_active",
                table: "treasury_position_bases",
                columns: new[] { "portfolio_id", "asset_id" },
                unique: true,
                filter: "active = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "treasury_position_bases");

            migrationBuilder.DropTable(
                name: "tesouro_import_batches");
        }
    }
}
