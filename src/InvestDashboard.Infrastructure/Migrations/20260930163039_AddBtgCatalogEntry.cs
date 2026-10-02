using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBtgCatalogEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "financial_institutions",
                columns: new[] { "id", "updated_at_utc", "category", "created_at_utc", "group_id", "name", "normalized_name" },
                values: new object[] { new Guid("10000000-0000-4000-8000-000000000015"), new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Banco", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "BTG Pactual", "BTG PACTUAL" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "financial_institutions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000015"));
        }
    }
}
