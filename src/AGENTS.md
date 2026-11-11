# Backend — Invest Dashboard

- Solução `InvestDashboard.slnx`, .NET 10 e arquitetura DDD: Domain não depende das demais camadas; Application depende de Domain; Infrastructure e WebAPI mantêm as dependências declaradas nos projetos.
- Use termos de negócio existentes em português e preserve convenções C# e nullable reference types. Siga os padrões existentes antes de alterar contratos públicos.
- No EF Core, verifique as classes de configuração da entidade; mantenha mapeamentos Fluent API isolados, convenções de nomes existentes, relacionamentos e índices explícitos quando aplicável. Revise migrações e impactos em dados antes de aplicá-las.
- Considere autorização e isolamento de dados, especialmente em APIs, SignalR e políticas RLS pertinentes. Nunca presuma que uma configuração de cliente substitui autorização no servidor.
- Execute `dotnet build InvestDashboard.slnx` e `dotnet test` quando apropriado. Confirme a cobertura dos testes de integração existentes; não trate placeholders como cobertura efetiva.
