using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InvestDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHolderProfilesAndInstitutionCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "holder_profile_id",
                table: "transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "financial_institution_id",
                table: "portfolios",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "holder_profile_id",
                table: "portfolios",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "financial_institutions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_institutions", x => x.id);
                    table.ForeignKey(
                        name: "FK_financial_institutions_portfolio_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "portfolio_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "portfolio_holders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    relationship = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portfolio_holders", x => x.id);
                    table.ForeignKey(
                        name: "FK_portfolio_holders_portfolio_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "portfolio_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "financial_institutions",
                columns: new[] { "id", "updated_at_utc", "category", "created_at_utc", "group_id", "name", "normalized_name" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-4000-8000-000000000001"), new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Banco", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Banco do Brasil", "BANCO DO BRASIL" },
                    { new Guid("10000000-0000-4000-8000-000000000002"), new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Banco", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Bradesco", "BRADESCO" },
                    { new Guid("10000000-0000-4000-8000-000000000003"), new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Banco", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Caixa", "CAIXA" },
                    { new Guid("10000000-0000-4000-8000-000000000004"), new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Banco", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Itaú", "ITAÚ" },
                    { new Guid("10000000-0000-4000-8000-000000000005"), new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Banco", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Nubank", "NUBANK" },
                    { new Guid("10000000-0000-4000-8000-000000000006"), new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Banco", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Santander", "SANTANDER" },
                    { new Guid("10000000-0000-4000-8000-000000000007"), new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Banco", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Banco Inter", "BANCO INTER" },
                    { new Guid("10000000-0000-4000-8000-000000000008"), new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Corretora", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Ágora", "ÁGORA" },
                    { new Guid("10000000-0000-4000-8000-000000000009"), new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Corretora", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Clear", "CLEAR" },
                    { new Guid("10000000-0000-4000-8000-000000000010"), new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Corretora", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Genial", "GENIAL" },
                    { new Guid("10000000-0000-4000-8000-000000000011"), new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Corretora", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Nu Invest", "NU INVEST" },
                    { new Guid("10000000-0000-4000-8000-000000000012"), new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Corretora", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Rico", "RICO" },
                    { new Guid("10000000-0000-4000-8000-000000000013"), new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Corretora", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "XP Investimentos", "XP INVESTIMENTOS" },
                    { new Guid("10000000-0000-4000-8000-000000000014"), new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), "DTVM", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "BTG Pactual DTVM", "BTG PACTUAL DTVM" }
                });

            migrationBuilder.Sql("""
                INSERT INTO portfolio_holders (id, group_id, name, normalized_name, user_id, relationship, created_at_utc, updated_at_utc)
                SELECT md5(group_id::text || ':' || upper(regexp_replace(trim(holder_name), '\s+', ' ', 'g')))::uuid,
                       group_id, min(trim(holder_name)), upper(regexp_replace(trim(holder_name), '\s+', ' ', 'g')),
                       NULL, NULL, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                FROM portfolios WHERE group_id IS NOT NULL AND trim(holder_name) <> ''
                GROUP BY group_id, upper(regexp_replace(trim(holder_name), '\s+', ' ', 'g'));

                UPDATE portfolios p SET holder_profile_id = h.id
                FROM portfolio_holders h WHERE p.group_id = h.group_id
                  AND upper(regexp_replace(trim(p.holder_name), '\s+', ' ', 'g')) = h.normalized_name;
                UPDATE transactions t SET holder_profile_id = p.holder_profile_id
                FROM portfolios p WHERE t.portfolio_id = p.id;

                UPDATE portfolios p SET financial_institution_id = i.id
                FROM financial_institutions i WHERE i.group_id IS NULL
                  AND upper(trim(p.financial_institution)) = i.normalized_name;
                INSERT INTO financial_institutions (id, name, normalized_name, category, group_id, created_at_utc, updated_at_utc)
                SELECT md5(group_id::text || ':institution:' || upper(trim(financial_institution)))::uuid,
                       min(trim(financial_institution)), upper(trim(financial_institution)), 'Outra', group_id,
                       CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                FROM portfolios WHERE group_id IS NOT NULL AND financial_institution_id IS NULL
                  AND trim(coalesce(financial_institution, '')) <> ''
                GROUP BY group_id, upper(trim(financial_institution));
                UPDATE portfolios p SET financial_institution_id = i.id
                FROM financial_institutions i WHERE p.group_id = i.group_id
                  AND upper(trim(p.financial_institution)) = i.normalized_name;
                """);

            migrationBuilder.CreateIndex(
                name: "idx_transactions_holder_profile_id",
                table: "transactions",
                column: "holder_profile_id");

            migrationBuilder.CreateIndex(
                name: "idx_portfolios_financial_institution_id",
                table: "portfolios",
                column: "financial_institution_id");

            migrationBuilder.CreateIndex(
                name: "idx_portfolios_holder_profile_id",
                table: "portfolios",
                column: "holder_profile_id");

            migrationBuilder.CreateIndex(
                name: "ux_financial_institutions_global_name",
                table: "financial_institutions",
                column: "normalized_name",
                unique: true,
                filter: "group_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_financial_institutions_group_name",
                table: "financial_institutions",
                columns: new[] { "group_id", "normalized_name" },
                unique: true,
                filter: "group_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_portfolio_holders_group_unlinked_name",
                table: "portfolio_holders",
                columns: new[] { "group_id", "normalized_name" },
                unique: true,
                filter: "user_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_portfolio_holders_group_user",
                table: "portfolio_holders",
                columns: new[] { "group_id", "user_id" },
                unique: true,
                filter: "user_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_portfolios_financial_institutions_financial_institution_id",
                table: "portfolios",
                column: "financial_institution_id",
                principalTable: "financial_institutions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_portfolios_portfolio_holders_holder_profile_id",
                table: "portfolios",
                column: "holder_profile_id",
                principalTable: "portfolio_holders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_portfolio_holders_holder_profile_id",
                table: "transactions",
                column: "holder_profile_id",
                principalTable: "portfolio_holders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_portfolios_financial_institutions_financial_institution_id",
                table: "portfolios");

            migrationBuilder.DropForeignKey(
                name: "FK_portfolios_portfolio_holders_holder_profile_id",
                table: "portfolios");

            migrationBuilder.DropForeignKey(
                name: "FK_transactions_portfolio_holders_holder_profile_id",
                table: "transactions");

            migrationBuilder.DropTable(
                name: "financial_institutions");

            migrationBuilder.DropTable(
                name: "portfolio_holders");

            migrationBuilder.DropIndex(
                name: "idx_transactions_holder_profile_id",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "idx_portfolios_financial_institution_id",
                table: "portfolios");

            migrationBuilder.DropIndex(
                name: "idx_portfolios_holder_profile_id",
                table: "portfolios");

            migrationBuilder.DropColumn(
                name: "holder_profile_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "financial_institution_id",
                table: "portfolios");

            migrationBuilder.DropColumn(
                name: "holder_profile_id",
                table: "portfolios");
        }
    }
}
