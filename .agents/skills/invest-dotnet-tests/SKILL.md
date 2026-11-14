---
name: invest-dotnet-tests
description: Choose, run, or explain .NET test commands for the Invest Dashboard solution, including unit tests and integration tests that require PostgreSQL.
---

# Testes .NET do Invest Dashboard

Confirme os projetos, versões e comandos no checkout, especialmente nos `.csproj` dos testes. O backend usa `src/InvestDashboard.slnx` e tem projetos separados em `src/tests/InvestDashboard.UnitTests` e `src/tests/InvestDashboard.IntegrationTests`; não suponha versões de xUnit, FluentAssertions ou do SDK sem consultar os manifests.

## Escolha de escopo

- Teste de domínio/lógica isolada: selecione o projeto `InvestDashboard.UnitTests` ou um filtro xUnit específico.
- Integração HTTP com `CustomWebApplicationFactory` sem PostgreSQL: selecione os testes pertinentes; essa fixture substitui Npgsql por EF Core InMemory e não valida SQL, migrations, locks ou semântica do provedor.
- Métodos anotados `[RequiresPostgresFact]`: precisam de `INVEST_TEST_POSTGRES_CONNECTION`. Eles criam um banco temporário, aplicam o cenário e removem o banco. Use somente um serviço PostgreSQL de teste autorizado e isolado.
- Suite configurada como CI: consulte `.github/workflows/ci.yml`; ali o serviço PostgreSQL é iniciado para integração, a variável é definida e `dotnet test src/InvestDashboard.slnx --no-restore --configuration Release` roda a solução.

## Comandos

Na raiz do repositório:

```sh
dotnet test src/InvestDashboard.slnx
dotnet test src/tests/InvestDashboard.UnitTests/InvestDashboard.UnitTests.csproj
dotnet test src/tests/InvestDashboard.IntegrationTests/InvestDashboard.IntegrationTests.csproj
dotnet test src/tests/InvestDashboard.UnitTests/InvestDashboard.UnitTests.csproj --filter FullyQualifiedName~NomeDoTeste
```

Inspecione SDK, restore existente e argumentos suportados pela versão atual antes de escolher `--no-restore`, `--no-build`, filtros por trait ou opções de runner. Não instale pacotes nem mude xUnit/MSTest/NUnit para responder a uma tarefa de teste.

## Relato

Registre o comando exato, SDK/runtime, filtros, resultado e testes ignorados. Separe `passou`, `falhou`, `ignorado` e `não executado`. Não chame teste ignorado por falta de `INVEST_TEST_POSTGRES_CONNECTION` de aprovado; não trate InMemory como evidência de PostgreSQL. Nunca declare cobertura não medida como fato.

Quando for apropriado criar ou alterar testes, priorize comportamentos e contratos afetados, casos negativos e limites relevantes; mantenha preparação, ação e verificações claras. Não use TDD ou uma camada de mocks por obrigação nem trate quantidade de testes como evidência de cobertura.

## Análise de falha

Quando o pedido for diagnosticar uma falha, comece pelo primeiro erro relevante e pelos resultados/stack traces do comando real. Relacione falhas ao projeto, teste, ambiente e camada; diferencie falha determinística, provável flake e evidência insuficiente. Use artefatos CI ou histórico somente se o usuário os fornecer ou se houver fonte acessível e escopo claro. Este repositório não tem um collector normalizado de histórico de testes, então uma execução atual isolada não prova tendência ou flakiness. Não desative/quarentene testes nem altere testes automaticamente como efeito de uma análise.

## Referência de inspiração

Esta adaptação usa a abordagem de detecção de runner e escopo da skill [`run-tests`](https://github.com/dotnet/skills/blob/96c3d68dad2855e3ab5178aa1a262789d86a1466/plugins/dotnet-test/skills/run-tests/SKILL.md). Consulte a fonte para semântica específica do SDK atual; suas extensões para MTP/overlays não são presumidas aqui.
