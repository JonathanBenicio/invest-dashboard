using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioOwnershipAndVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "financial_institution",
                table: "portfolios",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "group_id",
                table: "portfolios",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "holder_name",
                table: "portfolios",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "visibility",
                table: "portfolios",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "Particular");

            migrationBuilder.Sql("UPDATE portfolios SET holder_name = user_id WHERE holder_name = '';");

            migrationBuilder.Sql("""
                INSERT INTO portfolio_groups (id, name, created_by_user_id, created_at_utc)
                SELECT gen_random_uuid(), 'Grupo pessoal', user_id, CURRENT_TIMESTAMP
                FROM portfolios
                GROUP BY user_id;

                INSERT INTO portfolio_group_members (id, group_id, user_id, email, name, role, active, joined_at_utc)
                SELECT gen_random_uuid(), groups.id, groups.created_by_user_id,
                       COALESCE(latest.email, groups.created_by_user_id),
                       COALESCE(latest.name, ''), 'Admin', TRUE, CURRENT_TIMESTAMP
                FROM portfolio_groups groups
                LEFT JOIN LATERAL (
                    SELECT email, name FROM auth_sessions
                    WHERE user_id::text = groups.created_by_user_id
                    ORDER BY created_at_utc DESC LIMIT 1
                ) latest ON TRUE
                WHERE groups.name = 'Grupo pessoal'
                  AND EXISTS (SELECT 1 FROM portfolios p WHERE p.user_id = groups.created_by_user_id);

                UPDATE portfolios p
                SET group_id = groups.id
                FROM portfolio_groups groups
                WHERE groups.created_by_user_id = p.user_id
                  AND groups.name = 'Grupo pessoal';
                """);

            migrationBuilder.Sql("ALTER TABLE portfolios ALTER COLUMN visibility DROP DEFAULT;");

            migrationBuilder.CreateIndex(
                name: "idx_portfolios_group_visibility",
                table: "portfolios",
                columns: new[] { "group_id", "visibility" });

            migrationBuilder.AddForeignKey(
                name: "FK_portfolios_portfolio_groups_group_id",
                table: "portfolios",
                column: "group_id",
                principalTable: "portfolio_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_portfolios_portfolio_groups_group_id",
                table: "portfolios");

            migrationBuilder.DropIndex(
                name: "idx_portfolios_group_visibility",
                table: "portfolios");

            migrationBuilder.DropColumn(
                name: "financial_institution",
                table: "portfolios");

            migrationBuilder.DropColumn(
                name: "group_id",
                table: "portfolios");

            migrationBuilder.DropColumn(
                name: "holder_name",
                table: "portfolios");

            migrationBuilder.DropColumn(
                name: "visibility",
                table: "portfolios");
        }
    }
}
