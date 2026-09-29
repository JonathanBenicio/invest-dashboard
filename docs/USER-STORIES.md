# Catálogo de User Stories

## Escopo desta revisão

As histórias abaixo foram preservadas do catálogo anterior e receberam IDs estáveis para referência; isso não as torna requisitos aprovados. Revisão estática do código-fonte no working tree em 2026-09-28. Os links de evidência indicam código ou especificações encontradas, não prova de execução. Nenhum teste/build foi executado nesta revisão. Código presente (inclusive mudanças locais) não equivale a comportamento validado. Para todas as histórias, critérios de aceite verificáveis e contratos completos permanecem pendentes de confirmação; não foram inferidos a partir do código.

| ID estável | História | Estado observado no código | Evidência e gap principal |
|---|---|---|---|
| US-AUTH-001 | Autenticação segura | Parcial; não validada | [AuthController](../src/InvestDashboard.WebAPI/Controllers/AuthController.cs), [provider Supabase](../src/InvestDashboard.Infrastructure/Services/SupabaseAuthProvider.cs), [cenários registrados](features/authentication.feature). Login/cadastro por e-mail aparecem no código; SSO e isolamento/privacidade ponta a ponta não foram comprovados. |
| US-CONFIG-001 | Indicadores e taxas | Parcial; não validada | [TaxasController](../src/InvestDashboard.WebAPI/Controllers/TaxasController.cs), [modelo de taxa](../src/InvestDashboard.Domain/Aggregates/MarketData/TaxaEconomica.cs), [tela de taxas](../frontend/src/pages/tools/Taxas.tsx). CRUD genérico existe; atualização automática e uso integrado em projeções/cálculos não foram comprovados. |
| US-INV-001 | Compra e venda | Parcial; não validada | [Transacao](../src/InvestDashboard.Domain/Aggregates/Trading/Transacao.cs), [Carteira](../src/InvestDashboard.Domain/Aggregates/Portfolio/Carteira.cs), [TransacoesController](../src/InvestDashboard.WebAPI/Controllers/TransacoesController.cs). Há modelos/fluxo para operações; cobertura integrada e todos os campos/regras precisam ser confirmados. |
| US-INV-002 | Vencimentos | Parcial; a transição descrita não foi evidenciada | [RendaFixa](../src/InvestDashboard.Domain/Aggregates/MarketData/RendaFixa.cs) armazena data de vencimento e a UI mostra projeções. Não localizei transição automática de estado para “Vencido” nem liquidação do principal/juros. |
| US-TAX-001 | Apuração de lucro e prejuízo | Parcial; não validada | [PosicaoInvestimento](../src/InvestDashboard.Domain/Aggregates/Portfolio/PosicaoInvestimento.cs) mantém custo médio e atualiza vendas; [testes de carteira](../src/tests/InvestDashboard.UnitTests/Domain/CarteiraTests.cs) existem, mas não foram executados. Fórmulas/casos completos ainda precisam de critérios confirmados. |
| US-TAX-002 | Imposto de Renda | Cálculos de domínio presentes; integração e regras pendentes | [CalculoImpostoService](../src/InvestDashboard.Domain/Services/CalculoImpostoService.cs) contém alíquotas/limiar codificados. Não localizei fluxo que aplique o serviço ao registrar venda nem testes específicos no catálogo de testes. As regras fiscais não foram verificadas como orientação vigente. |
| US-PORT-001 | Histórico patrimonial | Dados demonstrativos; histórico real não comprovado | [resumo da carteira](../src/InvestDashboard.WebAPI/Controllers/CarteirasController.cs) devolve pontos de performance fixos derivados do valor atual; [preços históricos](../src/InvestDashboard.Domain/Aggregates/MarketData/PrecoHistorico.cs) e repositório existem, mas não confirmei integração destes dados no gráfico patrimonial. |
| US-PORT-002 | Visualização e filtragem | Parcial; filtros/status/instituição incompletos | [InvestimentosController](../src/InvestDashboard.WebAPI/Controllers/InvestimentosController.cs) filtra por tipo, subtipo e busca, com paginação. Não oferece filtro de status/instituição no endpoint consultado. |
| US-SIM-001 | Simulação de investimentos | Fluxo de simulação presente; integração de taxa externa não comprovada | [SimulacaoController](../src/InvestDashboard.WebAPI/Controllers/SimulacaoController.cs), [estratégias](../src/InvestDashboard.Domain/Services/EstrategiaDeterministica.cs) e [tela](../frontend/src/pages/tools/Simulator.tsx). Recebe aportes mensais e taxa informada; ligação com SELIC configurada e validação não foram comprovadas. |
| US-COMP-001 | Comparação de ativos | Protótipo visual com série fixa; seleção/dados reais não comprovados | [Analysis.tsx](../frontend/src/pages/tools/Analysis.tsx) define valores mensais no componente e desenha carteira/Ibovespa/CDI. Não encontrei seleção de ativos nem origem de dados integrada para essa comparação. |

## US-AUTH-001 — Autenticação segura

**Como** investidor, **quero** realizar login seguro via e-mail/senha ou SSO, **para** acessar meus dados financeiros com privacidade.

**Gap:** código de autenticação identificado não demonstra suporte a SSO nem prova isolamento de dados de ponta a ponta. Critérios de aceite e validação executada: pendentes.

## US-CONFIG-001 — Indicadores e taxas

**Como** usuário, **quero** configurar e visualizar variáveis macroeconômicas e fiscais (SELIC, percentual de IR, corretagem e B3), **para** que sejam consideradas nas projeções e cálculos.

**Gap:** CRUD de taxas econômicas é genérico; catálogo/valores específicos e integração com simulação/cálculos permanecem por confirmar. Critérios e validação executada: pendentes.

## US-INV-001 — Compra e venda

**Como** investidor, **quero** registrar operações de compra ou venda de ativos informando ticker, data, preço e taxas, **para** atualizar a custódia da carteira.

**Gap:** confirmar contratos e regras completas, inclusive taxas e cenários de erro. Testes existentes não foram executados nesta revisão.

## US-INV-002 — Vencimentos

**Como** investidor, **quero** que o sistema identifique investimentos que chegaram à data de término e atualize seu status, **para** refletir vencimento e retorno na carteira.

**Gap:** data/projeção de vencimento aparece no domínio/UI; mudança automática de status e crédito de principal/juros não foram encontrados. O comportamento original permanece uma proposta, não capacidade confirmada.

## US-TAX-001 — Apuração de lucro e prejuízo

**Como** investidor, **quero** que o sistema calcule preço médio, lucro e prejuízo em operações de venda, **para** acompanhar a performance por ativo.

**Gap:** há cálculo de custo médio e testes de domínio; fórmulas completas, casos negativos e execução dos testes não foram comprovados nesta revisão.

## US-TAX-002 — Imposto de Renda

**Como** investidor, **quero** que o sistema calcule ou estime Imposto de Renda sobre resultados, **para** compreender o impacto fiscal no lucro líquido.

**Gap:** serviço de domínio isolado contém regras codificadas, mas sua integração com vendas não foi localizada. As regras precisam de critérios e fonte validados antes de serem requisito; este registro não é orientação fiscal.

## US-PORT-001 — Histórico patrimonial

**Como** investidor, **quero** visualizar evolução histórica do patrimônio e rentabilidade, **para** acompanhar a evolução da carteira.

**Gap:** há gráfico e dados históricos em partes da interface, porém o endpoint de resumo consultado fornece pontos demonstrativos. Integração de uma série histórica persistida permanece pendente.

## US-PORT-002 — Visualização e filtragem

**Como** investidor, **quero** visualizar e filtrar ativos por classe, status ou instituição, **para** gerenciar recortes da carteira.

**Gap:** endpoint consultado oferece tipo/subtipo/busca; status e instituição não estão expostos nesse contrato. Critérios e evidência executada: pendentes.

## US-SIM-001 — Simulação de investimentos

**Como** investidor, **quero** simular aportes futuros com valor, prazo e taxa, **para** projetar resultados antes de investir.

**Gap:** estratégias determinística e Monte Carlo recebem taxa como entrada; integração com SELIC/CDI configurados e execução validada não foram comprovadas.

## US-COMP-001 — Comparação de ativos

**Como** investidor, **quero** comparar ativos ou benchmarks em um mesmo período, **para** avaliar seu desempenho relativo.

**Gap:** a tela apresenta uma série fixa de carteira/Ibovespa/CDI; seleção de ativos, origem de dados integrada, normalização e validação permanecem pendentes.

## Manutenção do catálogo

Não reutilize IDs. Histórias extensas podem ter arquivo próprio em `docs/user-stories/`, criado somente quando necessário e sempre ligado daqui. Ao confirmar ou mudar uma história, registre ator, benefício, critérios verificáveis, contratos, evidências e gaps conforme [templates/user-stories-template.md](../templates/user-stories-template.md). Atualize o estado apenas com evidência compatível.
