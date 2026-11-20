using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTreasuryReconciliationDecisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "treasury_reconciliation_decisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    portfolio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_index = table.Column<int>(type: "integer", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    decision = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    portfolio_version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treasury_reconciliation_decisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_treasury_reconciliation_decisions_tesouro_import_batches_ba~",
                        columns: x => new { x.batch_id, x.portfolio_id },
                        principalTable: "tesouro_import_batches",
                        principalColumns: new[] { "id", "portfolio_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_treasury_reconciliation_decision_audit",
                table: "treasury_reconciliation_decisions",
                columns: new[] { "batch_id", "event_index", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_treasury_reconciliation_decisions_batch_id_portfolio_id",
                table: "treasury_reconciliation_decisions",
                columns: new[] { "batch_id", "portfolio_id" });

            migrationBuilder.CreateIndex(
                name: "ux_treasury_reconciliation_decision_revision",
                table: "treasury_reconciliation_decisions",
                columns: new[] { "batch_id", "event_index", "revision" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "treasury_reconciliation_decisions");
        }
    }
}
