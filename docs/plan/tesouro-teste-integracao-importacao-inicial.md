# Plano — teste de integração da importação inicial do Tesouro Direto

- Estado: concluído com limitações registradas
- Issue: [#68](https://github.com/JonathanBenicio/invest-dashboard/issues/68); iniciativa relacionada [#53](https://github.com/JonathanBenicio/invest-dashboard/issues/53).
- ADR: N/A — teste do comportamento existente, sem decisão arquitetural nova identificada.
- Story: N/A — validação técnica de fluxo existente.
- Baseline: branch `codex/tesouro-previa-e-cotacoes`, commit `d68df4f` (plano publicado e sincronizado); alterações locais de frontend, agentes e backend preservadas.
- Plano canônico: `docs/plan/tesouro-teste-integracao-importacao-inicial.md`

## Objetivo, baseline e escopo

Verificar pela API, sem depender do frontend, a importação de HTML em carteira vazia com confirmação inicial sem revisão individual. Há testes de integração existentes para criação e confirmação de lote, mas o cenário examinado registra revisão individual; a cobertura completa do caminho direto ainda deve ser confirmada.

Inclui fixture HTML sintética, conciliação, confirmação e verificação de posição e eventos oficiais, além de bloqueios relevantes. Não inclui dados pessoais, alterações na carteira real ou mudanças de comportamento sem diagnóstico. O teste de confirmação depende de PostgreSQL de teste isolado e das dependências .NET do projeto.

A regra de taxa anual não informada em compra foi integrada no backend e no frontend. O backend sinaliza `camposNaoInformados`, preserva o traço e permite a confirmação se os demais campos essenciais estiverem válidos. A prévia mostra “Não informado: taxa do título (% a.a.)” sem classificar a ausência como erro. A tabela também recebeu contenção de largura e orientação de rolagem; a validação visual responsiva ainda está pendente.

## Etapas e verificações

| Etapa/entrega | Dependências | Verificação observável | Estado e evidência |
|---|---|---|---|
| Examinar testes e fixtures existentes, inclusive o arquivo não rastreado | Nenhuma | Identificar cobertura do caminho direto e preservar alterações existentes | Concluído; adicionada cobertura de integração do caminho inicial |
| Preparar HTML sintético com posição, compra, venda e taxa, incluindo traço não aplicável | Contrato real do leitor | Fixture reproduz os formatos relevantes sem dados pessoais | Concluído |
| Testar upload, conciliação e confirmação inicial sem revisões por HTTP | PostgreSQL isolado e API de teste autenticada | Respostas indicam importação inicial disponível e confirmação bem-sucedida | Passou; teste informado pelo agente backend |
| Verificar saldo, eventos oficiais vinculados e ausência de transações duplicadas | Confirmação no banco de teste | Estado persistido corresponde à fixture | Passou; teste informado pelo agente backend |
| Testar bloqueio do caminho direto para carteira com histórico e dados essenciais inválidos | Fixtures adequadas | API não confirma indevidamente; conferir respostas e efeitos | Cobertura existente executada; 3 integrações passaram |
| Alinhar semântica da taxa ausente em compra | Regra aprovada pelo usuário | Leitor, DTO, prévia e confirmação usam “não informado”, sem converter para zero | Implementado; testes unitários informados como aprovados |
| Validar responsividade da prévia | Playwright com fixture sintética via harness do projeto | Conferir conteúdo e tabela em 360, 767, 768, 1280, 1366 e 1920 px; menu móvel aberto e desktop expandido/recolhido | Passou; teste direcionado em 22/11/2026 |
| Executar testes direcionados e build pertinente | SDK .NET 10, dependências e PostgreSQL de teste | Registrar comandos, resultados e testes ignorados | Backend passou conforme relato do agente; frontend validado em instalação limpa compatível com o lockfile |

## Critérios de aceite

- [x] Fluxo direto de carteira vazia verificado pela API com fixture sintética, sem revisões individuais.
- [x] Posição e eventos oficiais persistidos verificados, sem compras/vendas duplicadas.
- [x] Histórico existente e dados essenciais inválidos não permitem confirmação inicial indevida.
- [x] Regra de taxa ausente em compra refletida na API e na prévia frontend.
- [x] Prévia tabular verificada nos viewports definidos via Playwright; sem overflow da página e com rolagem da tabela em celular.
- [x] Resultados conhecidos e limitações registrados; testes ignorados não apresentados como aprovados.

## Validação

| Verificação | Comando/método e ambiente | Estado | Evidência/limitação |
|---|---|---|---|
| Integração direcionada | `dotnet test tests/InvestDashboard.IntegrationTests/InvestDashboard.IntegrationTests.csproj --filter FullyQualifiedName~ConfirmacaoTesouro --verbosity minimal`, em `src/` | passou | SDK 10.0.404 em Docker, PostgreSQL 17 isolado: 3 passaram; agente reporta teste atualizado do fluxo direto passando (1 teste); 22/11/2026 |
| Testes unitários do leitor e confirmação | `dotnet test` filtrado nos projetos unitários | passou | Agente backend reporta 20 testes direcionados aprovados após alinhar expectativa e regra; não reexecutados pelo agente principal |
| Build backend | `dotnet build InvestDashboard.slnx --no-restore --verbosity minimal`, em `src/` | passou | SDK 10.0.404 em Docker: 0 erros e 0 avisos; agente backend reportou build atualizado aprovado |
| ESLint frontend | `eslint src/api/services/tesouro-direto.service.ts src/components/PreviaTesouroDireto.tsx` | passou | Execução local após mudança: exit code 0 |
| TypeScript frontend | `tsc -b --noEmit` | passou | TS 5.9.3 de acordo com `package-lock.json`, em cópia temporária limpa; `baseUrl` redundante removido de `tsconfig.app.json` |
| Build frontend | `npm run build` em cópia temporária restaurada pelo `package-lock.json` | passou | Vite 7.3.6; avisos de comentário puro de SignalR e bundle acima de 500 kB, sem falha de build |
| Regressão responsiva da prévia | `playwright test --config playwright.visual.config.ts --grep 'apresenta prévia de arquivo privado'` | passou | 1 E2E; 360, 767, 768, 1280, 1366 e 1920 px. Em telas estreitas a tabela rola internamente, sem overflow horizontal da página; menu móvel aberto e menu desktop em ambos os estados; fixture sintética, nenhuma chamada à API real; 22/11/2026 |

## Riscos, gaps e pendências

| Item | Impacto | Mitigação/ação | Referência/estado |
|---|---|---|---|
| Fixture sintética não cobre todas as variantes do HTML real | Teste verde não prova que toda importação real funciona | Usar formatos observados sem dados privados e investigar diferenças separadamente | Pendente |
| Dependências ou PostgreSQL de teste indisponíveis | Teste pode não executar ou ser ignorado | Usar serviço isolado autorizado e relatar resultado real | Pendente |
| A instalação `node_modules` do checkout está desalinhada dos manifests e contém arquivos sem permissão de escrita | Validações locais podem usar versões incompatíveis; `npm ci` direto falhou com EACCES | Dependências corretas foram restauradas numa cópia temporária limpa do projeto; reinstalação da pasta local requer corrigir ownership/limpeza com cuidado | Limitação local; checkout preservado |
| Fixture sintética não cobre todas as variações do HTML real nem prova funcionamento em produção | Pode haver diferenças de layout/dados ou configuração externa | Comparar com arquivos reais e API de destino em validação autenticada futura | Fora do teste automatizado; registrar como limitação |

## Entrega e sincronização

- Implementação: backend e frontend alterados localmente, sem commit.
- PR relacionado: #62; issue de validação: #68.
- Evidências e limitações: integração e testes unitários backend reportados como aprovados; E2E responsivo, ESLint, typecheck e build frontend passaram. Não houve importação na carteira real nem validação contra a API de produção.
- Índices e rastreamento: índice documental atualizado; issue #68 registrada com referência à iniciativa #53 e ao PR #62.
