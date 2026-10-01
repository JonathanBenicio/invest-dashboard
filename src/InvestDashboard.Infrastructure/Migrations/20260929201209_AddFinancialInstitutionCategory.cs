using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialInstitutionCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "financial_institution_type",
                table: "portfolios",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "financial_institution_type",
                table: "portfolios");
        }
    }
}
