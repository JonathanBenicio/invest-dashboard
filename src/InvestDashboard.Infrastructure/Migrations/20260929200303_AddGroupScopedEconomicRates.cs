using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupScopedEconomicRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_economic_rates_symbol",
                table: "economic_rates");

            migrationBuilder.AddColumn<Guid>(
                name: "group_id",
                table: "economic_rates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "periodicity",
                table: "economic_rates",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "reference_date",
                table: "economic_rates",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "unit",
                table: "economic_rates",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "updated_by_user_id",
                table: "economic_rates",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "economic_rate_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_value = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    new_value = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reference_date = table.Column<DateOnly>(type: "date", nullable: false),
                    updated_by_user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economic_rate_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_economic_rate_history_economic_rates_rate_id",
                        column: x => x.rate_id,
                        principalTable: "economic_rates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_economic_rates_group_symbol",
                table: "economic_rates",
                columns: new[] { "group_id", "symbol" });

            migrationBuilder.CreateIndex(
                name: "idx_economic_rate_history_rate_date",
                table: "economic_rate_history",
                columns: new[] { "rate_id", "updated_at_utc" });

            migrationBuilder.Sql("""
                UPDATE economic_rates
                SET unit = 'Percentual',
                    periodicity = 'Mensal',
                    reference_date = last_update::date,
                    updated_by_user_id = 'system';

                INSERT INTO economic_rates
                    (id, name, symbol, current_value, previous_value, description, source, last_update,
                     group_id, unit, periodicity, reference_date, updated_by_user_id)
                SELECT gen_random_uuid(), rates.name, rates.symbol, rates.current_value, rates.previous_value,
                       rates.description, rates.source, rates.last_update, groups.id, rates.unit, rates.periodicity,
                       rates.reference_date, rates.updated_by_user_id
                FROM economic_rates rates
                CROSS JOIN portfolio_groups groups
                WHERE rates.group_id IS NULL;

                INSERT INTO economic_rate_history
                    (id, rate_id, previous_value, new_value, source, reference_date, updated_by_user_id, updated_at_utc)
                SELECT gen_random_uuid(), rates.id, rates.previous_value, rates.current_value, rates.source,
                       rates.reference_date, rates.updated_by_user_id, rates.last_update
                FROM economic_rates rates
                WHERE rates.group_id IS NOT NULL;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_economic_rates_portfolio_groups_group_id",
                table: "economic_rates",
                column: "group_id",
                principalTable: "portfolio_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_economic_rates_portfolio_groups_group_id",
                table: "economic_rates");

            migrationBuilder.DropTable(
                name: "economic_rate_history");

            migrationBuilder.DropIndex(
                name: "idx_economic_rates_group_symbol",
                table: "economic_rates");

            migrationBuilder.DropColumn(
                name: "group_id",
                table: "economic_rates");

            migrationBuilder.DropColumn(
                name: "periodicity",
                table: "economic_rates");

            migrationBuilder.DropColumn(
                name: "reference_date",
                table: "economic_rates");

            migrationBuilder.DropColumn(
                name: "unit",
                table: "economic_rates");

            migrationBuilder.DropColumn(
                name: "updated_by_user_id",
                table: "economic_rates");

            migrationBuilder.CreateIndex(
                name: "idx_economic_rates_symbol",
                table: "economic_rates",
                column: "symbol");
        }
    }
}
