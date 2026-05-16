using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEconomicRateEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "economic_rates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    current_value = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    previous_value = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_update = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economic_rates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_economic_rates_symbol",
                table: "economic_rates",
                column: "symbol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "economic_rates");
        }
    }
}
