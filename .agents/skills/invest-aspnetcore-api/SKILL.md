---
name: invest-aspnetcore-api
description: Add, modify, document, or troubleshoot an HTTP API controller in the Invest Dashboard ASP.NET Core backend.
---

# API ASP.NET Core do Invest Dashboard

Use quando a tarefa alterar endpoints HTTP, DTOs da API, validação, OpenAPI ou tratamento de erros. Inspecione controller, serviço Application, DTOs e testes de integração relacionados antes de modificar.

## Convenções reais do projeto

- O backend usa controllers com `[ApiController]`, `[Authorize]` conforme o recurso, rotas versionadas sob `api/v1`, e serviços Application. Preserve o padrão existente; não migre controllers para Minimal APIs nem misture estilos por iniciativa própria.
- DTOs ficam em `src/InvestDashboard.Application/DTOs/`. Há records e classes mutáveis conforme o contrato já existente. Preserve contratos JSON, nullability, validação, compatibilidade e nomes de domínio; não force `sealed record`, XML docs ou `DateTimeOffset` em todos os DTOs sem evidência ou solicitação.
- Respostas de sucesso/erros de negócio costumam usar `RespostaApi<T>` (`Dados`, `Sucesso`, `Mensagem`). Exceções não tratadas passam por `ExceptionHandlingMiddleware`, que responde `ProblemDetails`. Preserve e teste a distinção existente; não substitua o envelope globalmente.
- `Program.cs` configura `PortugueseApiJsonNamingPolicy`, `AddOpenApi()` e publica `/openapi/v1.json` apenas em Development. Consulte essa configuração antes de adicionar anotações ou mudar JSON/OpenAPI.
- Controllers podem interagir com dados multiusuário/grupo; verifique autorização, ownership, papel de membro, `grupoId` e isolamento de dados no fluxo completo, não apenas no atributo da action.
- Esta skill trata HTTP. SignalR tem hubs, contratos e autenticação próprios; use o contexto de realtime em vez de aplicar pressupostos de controllers.

## Fluxo de mudança e validação

Trace route → binding/validação → autorização → serviço Application → persistência/efeitos → resultado HTTP. Reutilize o serviço e as respostas do projeto; mantenha regra de negócio fora do controller. Para endpoints novos, confirme semântica HTTP no contrato existente e consulte `templates/endpoint-template.md` quando documentar o contrato fizer parte do pedido.

Antes de mudar rota, parâmetros, nomes JSON, status ou formato de erro, encontre os consumidores reais no frontend e outros clientes identificáveis; avalie compatibilidade e ajuste-os dentro do escopo da tarefa. Para dados protegidos, verifique acesso permitido e negado e isolamento entre usuários e grupos quando esses cenários forem pertinentes.

Use testes em `src/tests/InvestDashboard.IntegrationTests/Controllers` para validar status, payload, autorização e efeitos observáveis. Escolha InMemory ou PostgreSQL conforme o comportamento exercitado; consulte `$invest-dotnet-tests`. Build/testes só confirmam o escopo realmente executado.

## Referência de inspiração

Adapta seletivamente [`dotnet-webapi`](https://github.com/dotnet/skills/blob/96c3d68dad2855e3ab5178aa1a262789d86a1466/plugins/dotnet-aspnetcore/skills/dotnet-webapi/SKILL.md). As instruções de DTOs universais, Minimal APIs, SignalR e serialização estrita não substituem os contratos deste projeto.
