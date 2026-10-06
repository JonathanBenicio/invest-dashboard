using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowUnlinkedHolderNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_portfolio_holders_group_unlinked_name",
                table: "portfolio_holders");

            migrationBuilder.CreateIndex(
                name: "idx_portfolio_holders_group_unlinked_name",
                table: "portfolio_holders",
                columns: new[] { "group_id", "normalized_name" },
                filter: "user_id IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_portfolio_holders_group_unlinked_name",
                table: "portfolio_holders");

            migrationBuilder.CreateIndex(
                name: "ux_portfolio_holders_group_unlinked_name",
                table: "portfolio_holders",
                columns: new[] { "group_id", "normalized_name" },
                unique: true,
                filter: "user_id IS NULL");
        }
    }
}
