# Plano: padronizar contratos e DTOs da API em português

## Objetivo

Alinhar os contratos JSON da API própria em português brasileiro entre .NET e TypeScript, sem alterar regras financeiras, banco de dados ou formatos de terceiros. A decisão de compatibilidade foi confirmada pelo usuário: o sistema ainda não tem clientes reais, então a migração será coordenada diretamente em `/api/v1`.

## Escopo e decisões

- Tipos DTO da aplicação usam nomes de negócio em português. Propriedades JSON, DTOs TypeScript e query parameters usam português em `camelCase` (`carteiraId`, `valorAtual`, `dataTransacao`). Algumas propriedades CLR em inglês são convertidas na fronteira HTTP pela política JSON centralizada; não são aliases publicados.
- Envelopes e paginação também são contratos próprios e serão traduzidos (`RespostaApi`, `RespostaPaginada`, `dados`, `sucesso`, `mensagem`, `paginacao`, `pagina`, `itensPorPagina`, `totalItens`, `totalPaginas`).
- Manter as rotas existentes em inglês. Não renomear entidades, serviços, repositórios, pastas, tabelas, colunas ou migrations.
- Preservar IDs, tickers, siglas, moeda, valores já usados como identificadores de enum e nomes de protocolo HTTP/JWT/cookies. Não alterar semântica financeira.
- Payloads espelhados de Brapi/Supabase seguem o formato do provedor. Integração e homologação com terceiros continuam na fase final; nenhum acesso externo faz parte deste trabalho.
- MSW permanece opt-in e seus fixtures e testes acompanham o contrato novo.
- Fora de escopo: fluxo de caixa/proventos, automação e visualização do Tesouro Transparente, deploy e migração de clientes externos.

## Compatibilidade

Fazer corte coordenado em `/api/v1`: atualizar API, frontend, MSW, testes e fixtures juntos. Não criar v2 nem manter aliases de payload em inglês. A confirmação do usuário é que ainda não existem clientes reais. Builds Pages/Android só serão afetados quando publicados novamente; deploy não faz parte desta tarefa.

## Inventário coberto

| Área/rotas atuais | Contratos e consumidores a alinhar |
|---|---|
| Carteiras: `/portfolios` e summary/history | Carteira, criação/atualização, resumo, alocação, desempenho/histórico; serviços, hooks, telas e filtros |
| Posições: `/investments`, summary, fixed-income, variable-income, valuations, history | Posição, renda fixa/variável, avaliação, resumo e filtros; telas de carteira e renda fixa |
| Transações: `/transactions` | Registro, atualização, resultado/listagem e filtros; serviços, formulários e importação CSV |
| Taxas: `/taxes` | Taxa econômica e requests; serviço e tela de taxas |
| Simulação: `/simulation` e strategies | Solicitação, pontos, resposta e catálogo (`nome`, `descricao`); simulador, MSW e E2E |
| Mercado: `/market-data/quotes`, search, history | DTOs próprios de cotação/busca/histórico; manter formato externo na fronteira Brapi |
| Autenticação: `/auth` | Requests, sessão e usuário próprios; mapear na fronteira Supabase sem renomear claims/cookies |
| Compartilhado | Respostas/paginação, erros, parâmetros de consulta, SignalR e exports em `frontend/src/api/dtos/` |

Inventário fechado: requests inline de investimentos foram extraídos para DTOs nomeados; o resumo de posições agora usa `ResumoInvestimentoDto`; envelopes, paginação, autenticação e mercado têm tipos próprios em português; estratégias expõem `nome/descricao`; filtros e SignalR estão mapeados. `ProblemDetails` conserva os nomes padronizados pela especificação HTTP. Tipos exclusivos de rotas admin/chat mockadas não são tratados como contratos de produto.

## Plano executável

1. [x] Confirmar compatibilidade: corte coordenado em `/api/v1`, sem clientes reais e sem aliases legados.
2. [x] Fechar inventário por endpoint: registrar formato JSON/query atual e canônico, localizar tipos inline/anônimos e distinguir DTO próprio de modelo de provedor.
3. [x] Implementar DTOs de resposta e request em português no backend, extrair requests inline, tipar o resumo de investimentos e aplicar a mesma política JSON a controllers e OpenAPI.
4. [x] Migrar nomes e propriedades dos DTOs TypeScript, cliente, serviços, hooks, stores e consumidores de tela.
5. [x] Atualizar telas, MSW opt-in, fixtures, testes e o simulador que já estava aberto para consumir o contrato português.
6. [x] Publicar a referência do contrato em [`docs/API-CONTRACTS-PT-BR.md`](docs/API-CONTRACTS-PT-BR.md) e verificar os nomes do schema OpenAPI; sem deploy.
7. [x] Verificação final solicitada: `dotnet build InvestDashboard.slnx -c Release --no-restore`, `bunx tsc -p tsconfig.app.json --noEmit` e `bun run build`. Não foi criada uma suíte separada apenas para nomes de contrato. GitHub Actions só serão verificados se houver PR; para commit isolado, validação local basta.

## Critérios de aceite

- Cada endpoint próprio coberto publica nomes de DTO/campos/query em português conforme o mapa aprovado; backend, frontend e fixtures concordam sem aliases silenciosos.
- Respostas anônimas e requests inline não deixam contratos sem nome/documentação.
- O corte coordenado em v1 não aceita aliases legados em inglês; MSW, app e API refletem a mesma versão publicada.
- DTOs externos, persistência, comportamento financeiro e identificadores estáveis permanecem compatíveis.
- Backend, typecheck e build do frontend passam; nenhuma integração externa é necessária.
