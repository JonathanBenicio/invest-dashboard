---
name: invest-ef-query-review
description: Review or improve EF Core query behavior in the Invest Dashboard .NET backend, especially when asked about slow queries, result size, tracking, round trips, SQL shape, or indexing.
---

# Revisão de consultas EF Core do Invest Dashboard

Use os projetos reais em `src/InvestDashboard.Infrastructure`, `src/InvestDashboard.Application` e `src/tests` como fonte de verdade. O backend usa .NET 10, EF Core 10 e PostgreSQL/Npgsql; o `InvestDbContext` aplica configurações Fluent API da assembly Infrastructure.

## Revisão somente de código

1. Siga a operação desde o serviço/controller até a implementação de repositório, enumeração/materialização e limites da resposta. Inspecione chamadas, expressões e configuração da entidade.
2. Conte consultas e materializações no caminho real. Verifique se filtros, projeções, ordenação, paginação e agregações são executados pelo provedor ou depois de carregar os dados.
3. Revise cardinalidade de `Include`, tracking, operações repetidas, consultas dentro de loops, ordem estável da paginação e cancelamento. Distingua comportamento demonstrado no código de custo que depende dos dados, plano PostgreSQL ou produção.
4. Confira índices na configuração EF e nas migrations/snapshot antes de apontar falta de índice. A ausência de `HasIndex` em um arquivo isolado não prova que o banco não tem o índice nem que uma consulta faz scan.
5. Preserve autorização, isolamento por usuário/grupo, ordenação, transações, concorrência e efeitos do domínio. Não troque entidades rastreadas por projeções ou updates em lote sem provar que o contrato e os efeitos continuam iguais.
6. Relate achados com caminho da chamada, localização, impacto e evidência. Se faltar SQL, plano, logs ou volume representativo, proponha como coletar; não alegue ganho de performance medido a partir de inspeção estática.

## Alterações e validação

Faça a alteração mínima apoiada por evidência. Para comportamento funcional, prefira teste de integração com PostgreSQL quando a semântica do provedor, migrations, transação ou SQL for relevante; o fixture de integração também substitui EF por InMemory e não comprova equivalência com PostgreSQL. Para comparação de performance, defina a carga e obtenha baseline e resultado com SQL/plano/medições antes de afirmar melhoria.

Use `[invest-dotnet-tests](../invest-dotnet-tests/SKILL.md)` para escolher comandos e classificar dependências. A revisão pode ser somente de leitura; não altere dados, migrations aplicadas ou bancos compartilhados sem solicitação.

## Referências de inspiração

Esta skill é uma adaptação específica do repositório, inspirada por critérios de revisão de consultas em [`optimizing-ef-core-queries`](https://github.com/dotnet/skills/blob/96c3d68dad2855e3ab5178aa1a262789d86a1466/plugins/dotnet-data/skills/optimizing-ef-core-queries/SKILL.md) e nas orientações do Entity Framework Core. Consulte a fonte upstream para detalhes não cobertos; não assuma que sua instalação esteja disponível.
