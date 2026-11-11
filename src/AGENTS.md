# Backend — Invest Dashboard

- Solução `InvestDashboard.slnx`, .NET 10 e arquitetura DDD: Domain não depende das demais camadas; Application depende de Domain; Infrastructure e WebAPI mantêm as dependências declaradas nos projetos.
- Use termos de negócio existentes em português e preserve convenções C# e nullable reference types. Siga os padrões existentes antes de alterar contratos públicos.
- Para endpoints HTTP, DTOs, OpenAPI ou erros, use `$invest-aspnetcore-api` e preserve controllers, contratos, serialização e autorização existentes; delegue tarefas HTTP independentes a `invest-api` quando a sessão permitir. Endpoints SignalR têm outro fluxo.
- No EF Core, verifique as classes de configuração da entidade; mantenha mapeamentos Fluent API isolados, convenções de nomes existentes, relacionamentos e índices explícitos quando aplicável. Revise migrações e impactos em dados antes de aplicá-las.
- Para consultas lentas, suspeitas de N+1, tracking, paginação ou índice, use `$invest-ef-query-review`; siga o caminho até o consumidor e não declare melhoria de performance sem medição comparável.
- Para falhas de restore/compilação ou build mensuravelmente lento, use `$invest-msbuild`. Para um gargalo de runtime medido, pode delegar a `invest-dotnet-performance`. Binlogs podem conter valores de configuração; trate-os como artefatos potencialmente sensíveis.
- Considere autorização e isolamento de dados, especialmente em APIs, SignalR e políticas RLS pertinentes. Nunca presuma que uma configuração de cliente substitui autorização no servidor.
- Para executar ou escolher testes .NET, use `$invest-dotnet-tests`. Confirme se o caso depende de PostgreSQL; os testes com InMemory não validam o provedor relacional. Execute `dotnet build InvestDashboard.slnx` e testes pertinentes; relate a cobertura real.
