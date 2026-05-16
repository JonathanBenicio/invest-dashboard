using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "portfolio_id",
                table: "transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_transactions_portfolio_id",
                table: "transactions",
                column: "portfolio_id");

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_portfolios_portfolio_id",
                table: "transactions",
                column: "portfolio_id",
                principalTable: "portfolios",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_transactions_portfolios_portfolio_id",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_transactions_portfolio_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "portfolio_id",
                table: "transactions");
        }
    }
}
