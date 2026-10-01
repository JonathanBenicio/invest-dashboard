# Roadmap dos gaps funcionais

Atualizado em 29/09/2026 após auditoria das telas, contratos e integrações. O foco atual é acompanhamento de posições. Este documento separa bloqueios de integração, gaps que dependem de regra de negócio e a fase futura do Tesouro Transparente; não transforma decisões pendentes em comportamento presumido. Fluxo de caixa e proventos foram movidos para o [plano futuro de baixa prioridade](future-low-priority.md).

## P0 — Finalizar dados reais de mercado e autenticação

### P0.1 — Homologar Brapi

**Estado:** cliente conectado. Teste opt-in executado pelo `BrapiMarketDataClient` passou com cotação, histórico diário e busca reais de PETR4; a API pública retornou dados positivos e o histórico válido. Testes locais de contrato também cobrem 401/403/429, timeout, falha de rede, JSON inválido, ausência de timestamp e histórico vazio; essas falhas viram `MarketDataUnavailableException`. Token Brapi não foi configurado, logo limites e comportamento da conta contratada não foram homologados. O parser usa `adjustedClose`/`close` sem depender de `rawClose` Pro; a UI diferencia indisponibilidade de resultado vazio e permite retry.

**Tarefas**

1. Configurar token somente por secret store/variável de ambiente e manter `MarketData:BaseUrl` em HTTPS.
2. [x] Executar teste opt-in pelo `BrapiMarketDataClient` para cotação, histórico diário e busca de PETR4; revisar dados recebidos e parser. Cotações múltiplas e comportamento contratado com token seguem abertos.
3. Exercitar ticker inexistente, cotação sem horário, histórico vazio, timeout, indisponibilidade e respostas HTTP 401/403/429; confirmar que a API traduz falha em indisponibilidade, registra contexto sem token e a UI conserva último preço com estado de erro.
4. Avaliar cache/limitação de chamadas do worker e das telas para não exceder concorrência ou cota do plano. Definir intervalo e expiração com base nos limites reais do contrato Brapi.
5. Validar uma janela de histórico com a API real e a análise da carteira: datas, ajuste por eventos corporativos e séries incompletas devem aparecer corretamente identificados.

**Aceite:** Brapi configurada por segredo; cotação, histórico e busca mapeados pelo cliente oficial do projeto; falhas e limites visíveis sem preço inventado; chamadas e dados de referência confirmados em ambiente controlado. O resultado não deve sugerir que a validação pública sem token prova estabilidade sustentada.

### P0.2 — Homologar Supabase Auth

**Estado:** URL e chave publicável configuradas apenas no `.env` local ignorado pelo Git; valores de Postgres/JWT são segredos aleatórios locais. `auth/v1/settings` respondeu HTTP 200: login por e-mail habilitado, cadastros abertos e confirmação por e-mail obrigatória. Advisor do projeto indica proteção contra senhas vazadas desabilitada; não alterei a configuração remota. Supabase MCP confirmou projeto saudável e ausência de tabelas no schema `public`; segurança sem advisories críticos, mas com esse aviso de hardening. O Compose injeta URL/chave publicável na API. Usuário confirmou que ainda não há conta dedicada de homologação; teste live opt-in está pronto para validar login, refresh rotativo, claims `/me`, logout e revogação local quando configurada. Ver [instruções e campos](supabase-auth-homologation.md). Testes fake cobrem os contratos determinísticos.

**JWT:** neste desenho a API valida credenciais via Supabase, emite seu próprio JWT e revoga imediatamente o access token upstream. `SUPABASE_JWKS_URL` não participa deste fluxo; aceitar diretamente JWTs Supabase exigiria uma decisão/migração de autenticação separada.

**Tarefas**

1. [x] Configurar localmente URL e chave publicável (nunca secret key) e gerar segredos independentes de Postgres/JWT; verificar que `.env` está ignorado pelo Git e que a API recebe as variáveis sem registrá-las nos logs.
2. Testar cadastro com confirmação habilitada e desabilitada, login válido/inválido, usuário não confirmado, conflito de cadastro e indisponibilidade do provedor.
3. Confirmar que o backend emite sua sessão JWT própria somente após identidade Supabase validada; validar `sub`, papel, expiração, issuer/audience e isolamento multiusuário com chamadas reais autenticadas.
4. Validar refresh rotativo da sessão local, reutilização do refresh antigo, logout/revogação, cookies `HttpOnly`, `Secure` em HTTPS, `SameSite` e comportamento após expiração.
5. Decidir e documentar se refresh/revogação precisam também atualizar/revogar a sessão upstream Supabase. O fluxo atual cria refresh próprio no backend; não assumir que isso renova automaticamente a sessão Supabase.
6. Repetir integração em ambiente descartável de homologação e manter testes automatizados determinísticos com fake provider no CI.

**Aceite:** login real, claims e isolamento demonstrados sem expor refresh/access tokens; ciclos de refresh/logout testados; limitações entre sessão local e upstream documentadas. Deploy permanece bloqueado até esse aceite.

## P1 — Gaps locais que dependem de critérios de negócio

### P1.1 — Vencimento e liquidação de renda fixa

**Estado:** implementado no domínio/API/UI. `AtualizadorVencimentosWorker` avalia posições de renda fixa de hora em hora usando `America/Sao_Paulo`, persiste `MaturedAtUtc` e mantém quantidade, custo, valor atual e transações. Estado idempotente. Teste de domínio e teste PostgreSQL entre hosts passaram; a suíte relacional completa também confirmou migration/persistência.

**Decisão do usuário:** quando a data de vencimento chegar, apenas marcar a posição como vencida e manter seu valor. Não lançar crédito em caixa nesta etapa. A data de referência será avaliada em `America/Sao_Paulo`; marcação repetida deve ser idempotente. Atraso do emissor, liquidação, juros e imposto no resgate pertencem a regras posteriores.

**Pendência:** nenhuma para a regra aprovada. Não creditar caixa nem criar transação financeira presumida.

**Aceite:** concluído em domínio, API, UI e PostgreSQL; posição vencida mantém valor e transações, sem crédito em caixa.

### P1.2 — Apuração de lucro/prejuízo e imposto

**Estado:** primeira estimativa versionada implementada em `GET /api/v1/taxes/estimativa-mensal`, exposta na análise e testada na API. A modalidade fiscal explícita é capturada nas vendas de ações/FII; o cálculo agrupa vendas por mês/categoria, aplica taxas registradas, custo realizado, prejuízos futuros apenas em mesma categoria, limite mensal de isenção das ações comuns, alíquotas comuns/day trade/FII, arredondamento e fonte/versionamento. Dados legados sem modalidade são excluídos e sinalizam incompletude. Não estima IRRF; ETFs, BDRs, cripto, renda fixa, derivativos, mercado fora da bolsa, eventos corporativos e operações de outras instituições ficam explicitamente fora do escopo.

**Decisão do usuário:** implementar estimativas de IR para vendas, rotuladas como estimativa e com regras e fontes versionadas. Dependências: fonte oficial vigente, escopo de ativos/operações, day trade vs. swing trade, FII, compensação de prejuízo, taxas, isenções/limites, arredondamento e tratamento de eventos corporativos. A aplicação deve identificar cálculo como estimativa e não orientação fiscal.

**Tarefas concluídas:** registrar modalidade nas vendas, migration aplicada nos testes PostgreSQL, acumular vendas mensais, calcular relatório versionado e expor premissas/fonte/limitações. Necessário revisar o cálculo com contador/consultoria antes de release e atualizar regras versionadas quando legislação mudar. O serviço legado `CalculoImpostoService` não deve ser usado para estimativas mensais.

**Aceite:** primeira versão concluída para o escopo explícito acima, com teste que ultrapassa R$20 mil via duas vendas no mês; UI mostra estimativa, modalidade, versão, origem e limitações. Revisão fiscal humana antes de release continua obrigatória.

### P1.3 — Benchmark CDI histórico

**Estado:** concluído por provider independente (BCB SGS 12, taxa diária composta, cache de 6h), API protegida e gráfico da análise alinhado a datas completas comuns com base 100; origem e atualização visíveis. O parser/composição passam teste unitário e o teste opt-in do provider de produção passou com resposta real da série BCB.

**Decisão do usuário:** CDI do Banco Central implementado. O usuário decidiu mover o contrato/licenciamento da série histórica Ibovespa B3 para uma funcionalidade futura, fora deste plano ativo.

**Tarefas concluídas:** CDI SGS 12, composição diária, cache e visualização em período comum sem interpolação.

**Aceite:** comparação carteira/CDI concluída. Ibovespa explicitamente adiado para plano futuro.

### P1.4 — Importação com extratos reais de corretoras

**Estado:** importador genérico CSV validado contra API/PostgreSQL. Foi criado o plano específico de descoberta/análise de formatos e implementação de adaptadores de corretoras em [broker-csv-import-plan.md](broker-csv-import-plan.md). Extratos reais anonimizados ainda não foram fornecidos/validados; parser específico aguarda as amostras.

**Plano:** [analisar e importar CSVs de corretoras](broker-csv-import-plan.md). Dependências: amostras anonimizadas com cabeçalhos e linhas representativas, sem CPF, conta, endereço ou saldo não necessário; identificação de corretora e versão do formato.

**Tarefas:** catalogar formatos aceitos; implementar parser por layout mantendo o modelo genérico; lidar com locale, eventos corporativos, taxas e identificador de operação; pré-visualizar diferenças; testar repetição/idempotência e casos malformados com fixtures sanitizadas.

**Aceite:** amostras representativas passam pelo parser e conferem totais/posições com extrato de origem; linhas ambíguas são rejeitadas com explicação e nunca registradas parcialmente como sucesso.

## P2 — Funcionalidades futuras explicitamente adiadas

### P2.1 — Benchmark histórico Ibovespa B3

**Estado:** funcionalidade futura por decisão do usuário. A série histórica de Market Data B3 exige confirmar contrato, custo, escopo de exibição/armazenamento e atribuição antes de coletar ou exibir; o provider B3 não será iniciado nesta fase.

**Quando priorizado:** obter autorização adequada e então implementar ingestão/provedor, alinhamento com datas completas comuns, atribuição e validação das regras de comparação. Não usar endpoints scraping nem replicar dados sem direito de uso confirmado.

### P2.2 — Coleta e visualização do Tesouro Transparente

**Estado:** plano detalhado em [real-data-integration.md](real-data-integration.md), incluindo catálogo CKAN/CSVs, cadência e atraso por dataset, idempotência por revisão/hash, streaming, qualidade e gráficos/tabelas comparados com posição individual. Não coletar base de investidores; não confundir estoque agregado com valor de mercado ou posição pessoal.

**Dependências:** amostras e metadados oficiais versionados, definição de armazenamento/retention, monitoramento e janela de execução diária após fechamento. Primeiro implementar ingestão verificável; depois endpoints; por último UI e comparação individual.

**Aceite:** seguir os critérios detalhados no plano vinculado: reprocessamento sem duplicatas, grandes CSVs em streaming, alertas por dataset/data, dados públicos separados de posição pessoal e comparação somente com instrumento/unidade/vencimento compatíveis.

## P3 — Gate final de release

1. [x] Builds .NET e frontend; 47 testes unitários (2 live ignorados), 66 integrações PostgreSQL, E2E MSW 14/14 e E2E real API/PostgreSQL 11/11 passaram. Smoke Compose anterior confirmou `/health/live`, `/health/ready` e frontend 200. Live Brapi público passou pelo cliente real. O teste live de Supabase segue ignorado sem conta dedicada.
2. [~] Supabase/Brapi com credenciais reais não foram homologados. Supabase sem conta de teste; Brapi sem token/limites da conta. PostgreSQL local não substitui autenticação externa.
3. Revisar logs/configuração para excluir credenciais; verificar que produção, GitHub Pages e Android build usam `VITE_USE_MSW=false`.
4. [x] Atualizar dependências sem `--force`, sincronizar `bun.lock` e `package-lock.json`; `npm audit` atual: zero advisories.
5. [ ] Deploy permanece bloqueado até homologação da autenticação Supabase, limites Brapi, licença/contrato B3 e revisão de segurança/configuração do ambiente de destino.

## Relação com MSW

MSW continua apenas como ferramenta opt-in de demonstração e E2E. `.env.local` deste checkout e o padrão de `Dockerfile.android` foram ajustados para desativado; os arquivos de Playwright mantêm ativação explícita onde usam fixtures.
