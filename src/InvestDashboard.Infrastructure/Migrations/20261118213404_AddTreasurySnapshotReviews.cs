using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTreasurySnapshotReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "treasury_snapshot_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    portfolio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comparison_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    divergences_json = table.Column<string>(type: "jsonb", nullable: false),
                    decision = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    portfolio_version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treasury_snapshot_reviews", x => x.id);
                    table.ForeignKey(
                        name: "FK_treasury_snapshot_reviews_tesouro_import_batches_batch_id_p~",
                        columns: x => new { x.batch_id, x.portfolio_id },
                        principalTable: "tesouro_import_batches",
                        principalColumns: new[] { "id", "portfolio_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_treasury_snapshot_review_audit",
                table: "treasury_snapshot_reviews",
                columns: new[] { "portfolio_id", "batch_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_treasury_snapshot_reviews_batch_id_portfolio_id",
                table: "treasury_snapshot_reviews",
                columns: new[] { "batch_id", "portfolio_id" });

            migrationBuilder.CreateIndex(
                name: "ux_treasury_snapshot_review_hash",
                table: "treasury_snapshot_reviews",
                columns: new[] { "batch_id", "comparison_hash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "treasury_snapshot_reviews");
        }
    }
}
