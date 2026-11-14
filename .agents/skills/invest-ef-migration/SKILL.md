---
name: invest-ef-migration
description: Prepare and review an Entity Framework Core schema migration for Invest Dashboard; use for database model or migration changes.
---

# Migração EF Core

Inspecione o modelo, configurações EF, migrations existentes, provider e projeto de inicialização antes de escolher comandos. A partir da raiz de `src`, quando o projeto estiver preparado para `dotnet ef`:

```sh
dotnet ef migrations add NomeDaMigracao --project InvestDashboard.Infrastructure --startup-project InvestDashboard.WebAPI
```

Revise o código gerado, snapshot, nulabilidade, índices, relacionamentos e risco de perda de dados; considere compatibilidade e rollback. Gere ou aplique a migração apenas quando a solicitação e o ambiente autorizarem. Confirme o banco de destino antes de `dotnet ef database update`; não aplique em banco compartilhado ou produção sem autorização específica. Execute validações pertinentes e relate dependências indisponíveis.

Para dados já existentes, avalie preenchimento de novos campos, valores legados e compatibilidade durante a implantação. Confira escopo de índices e unicidade por usuário ou grupo contra o domínio e as migrations anteriores; não infira o estado de um banco implantado apenas do modelo local. Valide contra PostgreSQL de teste isolado quando a alteração depender do provedor.

Se o trabalho incluir uma decisão arquitetural ou relatório documental de validação, consulte, respectivamente, [adr-template.md](../../../templates/adr-template.md) ou [validation-report-template.md](../../../templates/validation-report-template.md), seguindo os critérios e destinos de `conductor/workflow.md`. Uma migração, por si só, não exige criar esses documentos.
