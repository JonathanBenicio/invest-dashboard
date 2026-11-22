# Plano — teste de integração da importação inicial do Tesouro Direto

- Estado: planejado
- Issue: [#68](https://github.com/JonathanBenicio/invest-dashboard/issues/68); iniciativa relacionada [#53](https://github.com/JonathanBenicio/invest-dashboard/issues/53).
- ADR: N/A — teste do comportamento existente, sem decisão arquitetural nova identificada.
- Story: N/A — validação técnica de fluxo existente.
- Baseline: branch `codex/tesouro-previa-e-cotacoes`, commit `f77dc3d`; há alterações locais preexistentes no frontend, instruções de agentes e arquivo de teste não rastreado, que devem ser preservadas.
- Plano canônico: `docs/plan/tesouro-teste-integracao-importacao-inicial.md`

## Objetivo, baseline e escopo

Verificar pela API, sem depender do frontend, a importação de HTML em carteira vazia com confirmação inicial sem revisão individual. Há testes de integração existentes para criação e confirmação de lote, mas o cenário examinado registra revisão individual; a cobertura completa do caminho direto ainda deve ser confirmada.

Inclui fixture HTML sintética, conciliação, confirmação e verificação de posição e eventos oficiais, além de bloqueios relevantes. Não inclui dados pessoais, alterações na carteira real ou mudanças de comportamento sem diagnóstico. O teste de confirmação depende de PostgreSQL de teste isolado e das dependências .NET do projeto.

A regra de taxa anual não informada em compra também exige atualização do frontend para sinalizar a ausência na prévia. A prévia tabular requer validação responsiva em navegador; essas etapas são pendências complementares à validação da API.

## Etapas e verificações

| Etapa/entrega | Dependências | Verificação observável | Estado e evidência |
|---|---|---|---|
| Examinar testes e fixtures existentes, inclusive o arquivo não rastreado | Nenhuma | Identificar cobertura do caminho direto e preservar alterações existentes | Pendente |
| Preparar HTML sintético com posição, compra, venda e taxa, incluindo traço não aplicável | Contrato real do leitor | Fixture reproduz os formatos relevantes sem dados pessoais | Pendente |
| Testar upload, conciliação e confirmação inicial sem revisões por HTTP | PostgreSQL isolado e API de teste autenticada | Respostas indicam importação inicial disponível e confirmação bem-sucedida | Pendente |
| Verificar saldo, eventos oficiais vinculados e ausência de transações duplicadas | Confirmação no banco de teste | Estado persistido corresponde à fixture | Pendente |
| Testar bloqueio do caminho direto para carteira com histórico e dados essenciais inválidos | Fixtures adequadas | API não confirma indevidamente; conferir respostas e efeitos | Pendente |
| Investigar expectativa divergente do teste unitário do traço | Comportamento e regra de negócio confirmados | Ajustar teste ou código somente após identificar a causa | Pendente |
| Executar testes direcionados e build pertinente | SDK .NET 10, dependências e PostgreSQL de teste | Registrar comandos, resultados e testes ignorados | Pendente |

## Critérios de aceite

- [ ] Fluxo direto de carteira vazia verificado pela API com fixture sintética, sem revisões individuais.
- [ ] Posição e eventos oficiais persistidos verificados, sem compras/vendas duplicadas.
- [ ] Histórico existente e dados essenciais inválidos não permitem confirmação inicial indevida.
- [ ] Resultado dos testes e limitações registrados; teste ignorado não é apresentado como aprovado.

## Validação

| Verificação | Comando/método e ambiente | Estado | Evidência/limitação |
|---|---|---|---|
| Integração direcionada | `dotnet test tests/InvestDashboard.IntegrationTests/InvestDashboard.IntegrationTests.csproj --filter FullyQualifiedName~ConfirmacaoTesouro --verbosity minimal`, em `src/` | passou | SDK 10.0.404 em Docker, PostgreSQL 17 isolado: 3 passaram, 0 ignorados; 22/11/2026 |
| Testes unitários do leitor | `dotnet test tests/InvestDashboard.UnitTests/InvestDashboard.UnitTests.csproj --filter FullyQualifiedName~LeitorPreviaTesouroTests --verbosity normal`, em `src/` | falhou | SDK 10.0.404 em Docker: 9 passaram, 1 falhou; expectativa de traço na taxa de compra diverge do leitor; 22/11/2026 |
| Build backend | `dotnet build InvestDashboard.slnx --no-restore --verbosity minimal`, em `src/` | passou | SDK 10.0.404 em Docker: 0 erros e 0 avisos; 22/11/2026 |

## Riscos, gaps e pendências

| Item | Impacto | Mitigação/ação | Referência/estado |
|---|---|---|---|
| Fixture sintética não cobre todas as variantes do HTML real | Teste verde não prova que toda importação real funciona | Usar formatos observados sem dados privados e investigar diferenças separadamente | Pendente |
| Dependências ou PostgreSQL de teste indisponíveis | Teste pode não executar ou ser ignorado | Usar serviço isolado autorizado e relatar resultado real | Pendente |
| Teste de traço em compra falhou: espera taxa não interpretada; leitor marca não aplicável | Baseline não está verde | Aplicar a regra de taxa não informada aprovada pelo usuário e validar antes de prosseguir | Regra registrada no [plano de importação](tesouro-direto-importacao-e-dados-mercado-refinamento.md); correção pendente |

## Entrega e sincronização

- Implementação, commits e PR: pendentes; nenhuma publicação externa autorizada por este plano.
- Evidências e limitações: pendentes após execução.
- Índices e rastreamento: índice documental atualizado; issue #68 registrada com referência à iniciativa #53 e ao PR #62.
