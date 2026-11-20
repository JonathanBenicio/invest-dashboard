using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTreasuryOfficialEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "treasury_official_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    portfolio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_index = table.Column<int>(type: "integer", nullable: false),
                    event_json = table.Column<string>(type: "text", nullable: false),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treasury_official_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_treasury_official_events_assets_asset_id",
                        column: x => x.asset_id,
                        principalTable: "assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_treasury_official_events_tesouro_import_batches_batch_id_po~",
                        columns: x => new { x.batch_id, x.portfolio_id },
                        principalTable: "tesouro_import_batches",
                        principalColumns: new[] { "id", "portfolio_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_treasury_official_event_portfolio_asset",
                table: "treasury_official_events",
                columns: new[] { "portfolio_id", "asset_id", "batch_id" });

            migrationBuilder.CreateIndex(
                name: "IX_treasury_official_events_asset_id",
                table: "treasury_official_events",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_treasury_official_events_batch_id_portfolio_id",
                table: "treasury_official_events",
                columns: new[] { "batch_id", "portfolio_id" });

            migrationBuilder.CreateIndex(
                name: "ux_treasury_official_event_batch_index",
                table: "treasury_official_events",
                columns: new[] { "batch_id", "event_index" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "treasury_official_events");
        }
    }
}
