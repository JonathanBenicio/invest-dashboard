# Catálogo de User Stories

## Escopo desta revisão

Revisado em 29/09/2026. A [CI do commit a9ad5ac](https://github.com/JonathanBenicio/invest-dashboard/actions/runs/36536414566) passou com a nova cobertura de filtros; a execução local mais recente passou em 21 testes unitários, 25 de integração InMemory e 3 E2E. Os testes PostgreSQL de posição passaram em execução local anterior e na CI de `a9ad5ac`. Typecheck e build frontend passaram após integrar cotações Brapi/SignalR. Credenciais reais Supabase/Brapi e produção ainda não foram validadas. O lint global tem 30 erros e 16 avisos, todos preexistentes e fora da lógica funcional alterada; lint direcionado dos arquivos funcionais passou. Fluxo de caixa/proventos continuam adiados.

| ID estável | História | Estado observado no código | Evidência e gap principal |
|---|---|---|---|
| US-AUTH-001 | Autenticação segura | Implementada; testes de API passaram | JWT próprio de curta duração e refresh rotativo com cookie HttpOnly; testes de controller usam provedor de identidade fake. SSO e validação de ponta a ponta com Supabase/PostgreSQL continuam pendentes. |
| US-CONFIG-001 | Indicadores e taxas | Parcial; não validada | [TaxasController](../src/InvestDashboard.WebAPI/Controllers/TaxasController.cs), [modelo de taxa](../src/InvestDashboard.Domain/Aggregates/MarketData/TaxaEconomica.cs), [tela de taxas](../frontend/src/pages/tools/Taxas.tsx). CRUD genérico existe; atualização automática e uso integrado em projeções/cálculos não foram comprovados. |
| US-INV-001 | Compra e venda | Implementada; fluxo validado em InMemory e PostgreSQL | Teste integrado cobre compra idempotente, venda parcial, excesso de unidades e historico incompleto. O mesmo fluxo passou contra PostgreSQL 15 local e na CI, incluindo persistência entre hosts de API. |
| US-INV-002 | Vencimentos | Parcial; a transição descrita não foi evidenciada | [RendaFixa](../src/InvestDashboard.Domain/Aggregates/MarketData/RendaFixa.cs) armazena data de vencimento e a UI mostra projeções. Não localizei transição automática de estado para “Vencido” nem liquidação do principal/juros. |
| US-TAX-001 | Apuração de lucro e prejuízo | Parcial; testes de domínio passaram | Custo médio e ganho realizado são calculados, mas critérios fiscais completos e testes de casos de borda permanecem pendentes. |
| US-TAX-002 | Imposto de Renda | Cálculos de domínio presentes; integração e regras pendentes | [CalculoImpostoService](../src/InvestDashboard.Domain/Services/CalculoImpostoService.cs) contém alíquotas/limiar codificados. Não localizei fluxo que aplique o serviço ao registrar venda nem testes específicos no catálogo de testes. As regras fiscais não foram verificadas como orientação vigente. |
| US-PORT-001 | Historico patrimonial | Implementado com histórico incompleto identificado e cotações ligadas à interface | API e interface usam operações, histórico Brapi e atualização SignalR; listas/detalhe buscam cotações ao abrir, mostram origem/horário e preservam o valor persistido com aviso quando não há retorno. Períodos incompletos não recebem estimativas. Validar com credencial Brapi de homologação ainda pendente. |
| US-PORT-002 | Visualização e filtragem | Implementada; filtros combinados cobertos em teste | Endpoint oferece tipo, subtipo, emissor, setor, status, busca e paginação. Teste cobre filtros combinados, ordenação e paginação em InMemory e PostgreSQL; CI de `a9ad5ac` passou. |
| US-SIM-001 | Simulação de investimentos | Fluxo de simulação presente; integração de taxa externa não comprovada | [SimulacaoController](../src/InvestDashboard.WebAPI/Controllers/SimulacaoController.cs), [estratégias](../src/InvestDashboard.Domain/Services/EstrategiaDeterministica.cs) e [tela](../frontend/src/pages/tools/Simulator.tsx). Recebe aportes mensais e taxa informada; ligação com SELIC configurada e validação não foram comprovadas. |
| US-COMP-001 | Comparação de ativos | Protótipo visual com série fixa; seleção/dados reais não comprovados | [Analysis.tsx](../frontend/src/pages/tools/Analysis.tsx) define valores mensais no componente e desenha carteira/Ibovespa/CDI. Não encontrei seleção de ativos nem origem de dados integrada para essa comparação. |

## US-AUTH-001 — Autenticação segura

**Como** investidor, **quero** realizar login seguro via e-mail/senha ou SSO, **para** acessar meus dados financeiros com privacidade.

**Gap:** SSO não faz parte da entrega atual; integração real com Supabase e isolamento contra PostgreSQL ainda precisam de validação em ambiente configurado.

## US-CONFIG-001 — Indicadores e taxas

**Como** usuário, **quero** configurar e visualizar variáveis macroeconômicas e fiscais (SELIC, percentual de IR, corretagem e B3), **para** que sejam consideradas nas projeções e cálculos.

**Gap:** CRUD de taxas econômicas é genérico; catálogo/valores específicos e integração com simulação/cálculos permanecem por confirmar. Critérios e validação executada: pendentes.

## US-INV-001 — Compra e venda

**Como** investidor, **quero** registrar operações de compra ou venda de ativos informando ticker, data, preço e taxas, **para** atualizar a custódia da carteira.

**Gap:** O fluxo passou contra PostgreSQL 15 local e na CI, confirmando migrations e persistência. O job repete a cobertura nos PRs e pushes de `main`/`develop`; regras fiscais e vencimento automático continuam separados.

## US-INV-002 — Vencimentos

**Como** investidor, **quero** que o sistema identifique investimentos que chegaram à data de término e atualize seu status, **para** refletir vencimento e retorno na carteira.

**Gap:** data/projeção de vencimento aparece no domínio/UI; mudança automática de status e crédito de principal/juros não foram encontrados. O comportamento original permanece uma proposta, não capacidade confirmada.

## US-TAX-001 — Apuração de lucro e prejuízo

**Como** investidor, **quero** que o sistema calcule preço médio, lucro e prejuízo em operações de venda, **para** acompanhar a performance por ativo.

**Gap:** Os testes executam, mas ainda faltam critérios aprovados para múltiplas compras, taxas, perdas, vendas parciais e apuração fiscal.

## US-TAX-002 — Imposto de Renda

**Como** investidor, **quero** que o sistema calcule ou estime Imposto de Renda sobre resultados, **para** compreender o impacto fiscal no lucro líquido.

**Gap:** serviço de domínio isolado contém regras codificadas, mas sua integração com vendas não foi localizada. As regras precisam de critérios e fonte validados antes de serem requisito; este registro não é orientação fiscal.

## US-PORT-001 — Histórico patrimonial

**Como** investidor, **quero** visualizar evolução histórica do patrimônio e rentabilidade, **para** acompanhar a evolução da carteira.

**Gap:** Histórico e indicação de lacunas estão ligados à interface. A posição consulta `/market-data/quotes`, combina o retorno com eventos SignalR usando a observação mais recente e identifica origem/horário. Sem cotação, mantém o valor salvo sem simulá-lo e informa indisponibilidade. Ainda falta validar o fluxo com credencial Brapi de homologação.

## US-PORT-002 — Visualização e filtragem

**Como** investidor, **quero** visualizar e filtrar ativos por classe, status ou instituição, **para** gerenciar recortes da carteira.

**Gap:** O contrato inclui status e emissor além de tipo/subtipo/setor/busca. Combinação representativa, ordenação e paginação têm teste para InMemory e PostgreSQL; ambos passaram em CI. Cobertura de todas as combinações permanece pendente.

## US-SIM-001 — Simulação de investimentos

**Como** investidor, **quero** simular aportes futuros com valor, prazo e taxa, **para** projetar resultados antes de investir.

**Gap:** estratégias determinística e Monte Carlo recebem taxa como entrada; integração com SELIC/CDI configurados e execução validada não foram comprovadas.

## US-COMP-001 — Comparação de ativos

**Como** investidor, **quero** comparar ativos ou benchmarks em um mesmo período, **para** avaliar seu desempenho relativo.

**Gap:** a tela apresenta uma série fixa de carteira/Ibovespa/CDI; seleção de ativos, origem de dados integrada, normalização e validação permanecem pendentes.

## Manutenção do catálogo

Não reutilize IDs. Histórias extensas podem ter arquivo próprio em `docs/user-stories/`, criado somente quando necessário e sempre ligado daqui. Ao confirmar ou mudar uma história, registre ator, benefício, critérios verificáveis, contratos, evidências e gaps conforme [templates/user-stories-template.md](../templates/user-stories-template.md). Atualize o estado apenas com evidência compatível.
