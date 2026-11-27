using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowMissingTreasuryContractRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A coluna já aceita NULL na tabela TPH de ativos. Esta migration
            // registra somente a alteração de nulabilidade no modelo EF.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Não há alteração de schema para reverter.
        }
    }
}
