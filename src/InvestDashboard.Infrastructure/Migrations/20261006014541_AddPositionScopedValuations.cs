using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionScopedValuations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "shared_price_before_position_valuations",
                table: "asset_positions",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "position_valuations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    position_id = table.Column<Guid>(type: "uuid", nullable: false),
                    observed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    observed_quantity = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    recorded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_position_valuations", x => x.id);
                    table.ForeignKey(
                        name: "FK_position_valuations_asset_positions_position_id",
                        column: x => x.position_id,
                        principalTable: "asset_positions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_position_valuations_latest",
                table: "position_valuations",
                columns: new[] { "position_id", "observed_at_utc", "recorded_at_utc", "id" });

            migrationBuilder.Sql("""
                WITH unique_positions AS (
                    SELECT position.asset_id, position.id AS position_id, position.portfolio_id
                    FROM asset_positions position
                    WHERE 1 = (SELECT COUNT(*) FROM asset_positions sibling WHERE sibling.asset_id = position.asset_id)
                ), legacy_statements AS (
                    SELECT price.id, position.position_id,
                           price.date AS observed_at_utc, price.price AS unit_price,
                           COALESCE((
                               SELECT SUM(CASE WHEN ledger.type = 'Buy' THEN ledger.quantity
                                               WHEN ledger.type = 'Sell' THEN -ledger.quantity
                                               ELSE 0 END)
                               FROM transactions ledger
                               WHERE ledger.portfolio_id = position.portfolio_id
                                 AND ledger.asset_id = price.asset_id
                                 AND ledger.transaction_date <= price.date
                           ), 0) AS observed_quantity
                    FROM historical_prices price
                    JOIN unique_positions position ON position.asset_id = price.asset_id
                    WHERE price.source = 'statement'
                      AND EXISTS (
                          SELECT 1 FROM transactions purchase
                          WHERE purchase.portfolio_id = position.portfolio_id
                            AND purchase.asset_id = price.asset_id
                            AND purchase.type = 'Buy'
                            AND purchase.transaction_date = price.date
                      )
                )
                INSERT INTO position_valuations
                    (id, position_id, observed_at_utc, unit_price, observed_quantity, recorded_at_utc)
                SELECT id, position_id, observed_at_utc, unit_price, observed_quantity, observed_at_utc
                FROM legacy_statements
                WHERE observed_quantity > 0;

                WITH latest_valuation AS (
                    SELECT position_id, unit_price,
                           ROW_NUMBER() OVER (PARTITION BY position_id ORDER BY observed_at_utc DESC, recorded_at_utc DESC, id DESC) AS row_number
                    FROM position_valuations
                )
                UPDATE asset_positions position
                SET shared_price_before_position_valuations = position.current_price,
                    current_price = COALESCE(valuation.unit_price, position.average_cost)
                FROM (SELECT position_id, unit_price FROM latest_valuation WHERE row_number = 1) valuation
                WHERE position.id = valuation.position_id AND position.asset_type = 'RendaFixa';

                UPDATE asset_positions
                SET shared_price_before_position_valuations = current_price,
                    current_price = average_cost
                WHERE asset_type = 'RendaFixa'
                  AND shared_price_before_position_valuations IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE asset_positions SET current_price = shared_price_before_position_valuations
                WHERE shared_price_before_position_valuations IS NOT NULL;
                """);

            migrationBuilder.DropTable(
                name: "position_valuations");

            migrationBuilder.DropColumn(
                name: "shared_price_before_position_valuations",
                table: "asset_positions");
        }
    }
}
