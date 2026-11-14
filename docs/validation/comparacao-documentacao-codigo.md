# Relatório de validação — comparação da documentação com o código

- Data e fuso: 13/11/2026, America/Sao_Paulo.
- Branch/SHA: `codex/homologacao-autenticacao-real` / `f85ae79f73e641bdf19be22c9cdd08266268ea9a`, com alterações locais já adicionadas ao staging antes desta revisão. A organização documental, planos/relatório de autenticação e Tesouro e alterações em testes de autenticação eram preexistentes; este trabalho não altera o staging.
- Issue/PR: não criados para esta revisão; plano solicitado na conversa. ADR e Story: N/A — revisão documental sem nova decisão ou comportamento.
- Ambiente: inspeção estática do checkout local, sem executar aplicação nem serviços externos.

## Inventário e tratamento

O inventário anterior a este relatório tinha 40 arquivos em `docs`: 8 na raiz, 2 ADRs em `adrs`, 3 documentos em `codex`, 1 diagrama, 4 features, 2 requisitos históricos, 18 planos (incluindo 3 em `opencode`) e 2 relatórios. Além disso, foram considerados `README.md`, `AGENTS.md` e `GEMINI.md` da raiz. O [índice](../INDEX.md) lista os documentos individualmente.

| Categoria | Tratamento |
|---|---|
| Domínio, arquitetura e API | Comparação de pontos representativos com entidades, serviços, controllers, DTOs e configuração; referências atualizadas conforme achados abaixo. Não é auditoria exaustiva de cada campo ou endpoint. |
| Histórias e cenários | Preservados como requisitos e resultados datados; avisos distinguem cenários antigos, evidências históricas e comportamento observado. |
| Planos | Escopo e decisões preservados; planos de carteira, Chat IA e nomenclatura receberam avisos quando suas premissas não descrevem necessariamente o checkout. Os demais conservam suas datas, ressalvas ou estados registrados. |
| ADRs, auditorias e relatórios de validação | Preservados como registros de decisões, observações e execuções da época; seu conteúdo não foi tratado como validação desta revisão. A migração Codex teve corrigida a contagem de diretórios liberados pelo `.gitignore`. |
| Requisitos históricos e template legado | Preservados como histórico ou referência, não como implementação; consulte o índice. |
| Instruções e README da raiz | Referências, comandos e alegações de arquitetura confrontados com configuração e código; instruções de área continuam sujeitas ao ambiente e às permissões da sessão. |

## Resultados

| Verificação | Método | Resultado | Estado | Evidência e limite |
|---|---|---|---|---|
| Modelo de domínio | Leitura estática de entidades e serviços | Nomes e propriedades antigos não correspondiam às classes; vencimento não é tipo de transação. Modelo atualizado. | passou | [Carteira](../../src/InvestDashboard.Domain/Aggregates/Portfolio/Carteira.cs), [posição](../../src/InvestDashboard.Domain/Aggregates/Portfolio/PosicaoInvestimento.cs), [tipos de transação](../../src/InvestDashboard.Domain/Aggregates/Trading/TipoTransacao.cs). |
| Fiscal e simulação | Leitura dos serviços de domínio | A estimativa não demonstra emissão de DARF; Monte Carlo retorna mediana, não os percentis calculados localmente. Modelo atualizado. | passou | [Fiscal](../../src/InvestDashboard.Domain/Services/CalculoImpostoService.cs), [Monte Carlo](../../src/InvestDashboard.Domain/Services/EstrategiaMonteCarlo.cs). Não houve revisão fiscal especializada. |
| Fluxos e infraestrutura | Leitura de controllers, bootstrap, workers, hub, frontend e Compose | Rota, HTTP 201, JWT próprio, evento SignalR e banco configurável diferiam do diagrama anterior; arquitetura atualizada. | passou | [Transações](../../src/InvestDashboard.WebAPI/Controllers/TransacoesController.cs), [bootstrap](../../src/InvestDashboard.WebAPI/Program.cs), [worker](../../src/InvestDashboard.Infrastructure/BackgroundWorkers/AtualizadorDadosMercadoWorker.cs), [Compose](../../docker-compose.yml). Sem execução de ponta a ponta. |
| Contratos e exemplo | Leitura de controllers, política JSON, DTO e serviço de criação | Exemplo de carteira omitia instituição exigida; rotas não são todas em inglês e OpenAPI é mapeado em Development. Referência atualizada. | passou | [Serviço de carteiras](../../src/InvestDashboard.Application/Services/CarteiraAppService.cs), [política JSON](../../src/InvestDashboard.WebAPI/Serialization/PortugueseApiJsonNamingPolicy.cs), [bootstrap](../../src/InvestDashboard.WebAPI/Program.cs). Schema e exemplo não foram executados. |
| Cenários BDD | Leitura dos quatro arquivos e busca de nomes no código | Cenários antigos incluem `IsDefault`, rota `/api/portfolio`, cobrança/desconto fiscal e atualização visual instantânea sem evidência suficiente; advertências adicionadas sem alterar requisitos históricos. | passou | [Features](../features/), [controller de carteiras](../../src/InvestDashboard.WebAPI/Controllers/CarteirasController.cs). Ausência de evidência não é prova de impossibilidade. |
| Instruções e operação local | Leitura de configurações e arquivos raiz | Vite solicita porta 5000; instruções Gemini citavam caminhos inexistentes para ADR/plano; README alegava fallback Base64 no banco e um arquivo de licença ausente. Textos corrigidos. | passou | [Vite](../../frontend/vite.config.ts), [storage](../../src/InvestDashboard.Infrastructure/Services/SupabaseStorageService.cs), [workflow](../../conductor/workflow.md). A licença pretendida ainda exige confirmação. |
| Índice, links e diff | Script local Python para inventário e links Markdown relativos; `git diff --check` e `git diff --cached --check` | Todos os arquivos de `docs` estão no índice; nenhum destino relativo inexistente foi encontrado nos Markdown rastreados ou não ignorados; sem erros de whitespace nos diffs. | passou | A verificação de links cobre caminhos, não âncoras, URLs externas ou conteúdo renderizado. O relatório novo estava não rastreado. |
| Homologação de terceiros e testes | Sem execução | Supabase, Brapi, CDI externo, frontend, backend e banco não foram executados nesta revisão. | não executado | Resultados anteriores permanecem nos relatórios originais e não são renovados por leitura de código. |

## Limitações e pendências

- A inspeção não prova correção das regras financeiras, autorização em todos os fluxos, integridade de dados, compatibilidade de cada contrato ou funcionamento dos serviços externos. Nenhum teste ou build foi executado nesta revisão.
- As referências de domínio, API e arquitetura foram verificadas por amostragem dirigida aos pontos acima; planos extensos e requisitos históricos não foram auditados linha a linha contra toda a implementação.
- Os ADRs de stack históricos têm identificadores sobrepostos e decisões parcialmente coincidentes; a revisão não os renumera nem altera status. Os planos OpenCode apresentam propostas de nomenclatura distintas; não há decisão nova nesta revisão.
- Os cenários fiscais e de carteira assinalados exigem decisão de produto e revisão especializada antes de serem tratados como critérios aceitos ou comportamento comprovado. A licença do projeto requer confirmação dos responsáveis.
