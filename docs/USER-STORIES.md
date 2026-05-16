# User Stories & Epics - Invest Dashboard

## Visão Geral do Produto
O objetivo da plataforma é ser uma solução completa de Gestão de Carteira de Investimentos. A plataforma permite gerenciar todo o ciclo de vida dos investimentos (compras, vendas, vencimentos), simular cenários, acompanhar a evolução histórica, realizar comparações entre ativos e contar com cálculos automatizados de lucro, prejuízo e deduções fiscais (IR, taxas), baseando-se em indicadores macroeconômicos configuráveis (SELIC, CDI, Juros).

---

## Epic 1: Autenticação e Configurações Globais
### US 1.1: Autenticação Segura (Supabase)
**Como** investidor, **Quero** realizar login seguro via e-mail/senha ou SSO, **Para** acessar meus dados financeiros com privacidade.

### US 1.2: Configuração de Indicadores e Taxas
**Como** usuário, **Quero** configurar e visualizar variáveis macroeconômicas e fiscais (Taxa SELIC atual, % de IR padrão, taxas de corretagem e B3), **Para** que o sistema utilize essas bases em todas as projeções e cálculos automáticos de rendimento e desconto.

## Epic 2: Gestão do Ciclo de Vida do Investimento
### US 2.1: Lançamento de Operações (Compra e Venda)
**Como** investidor, **Quero** registrar a compra ou venda de ativos (Ações, FIIs, Renda Fixa) informando ticker, data, preço e taxas, **Para** atualizar a custódia da minha carteira.

### US 2.2: Gestão de Vencimentos (Renda Fixa / Opções)
**Como** investidor, **Quero** que o sistema identifique e altere o status de investimentos que chegaram à data de término para "Vencidos", **Para** refletir a liquidez e o retorno do principal + juros na conta corrente da carteira.

## Epic 3: Cálculos Automáticos e Fiscalidade
### US 3.1: Apuração de Lucro e Prejuízo
**Como** investidor, **Quero** que o sistema calcule automaticamente o preço médio, lucro bruto e prejuízo acumulado de cada operação de venda, **Para** ter clareza exata da minha performance por ativo.

### US 3.2: Retenção e Cálculo de Imposto de Renda (IR)
**Como** investidor, **Quero** que o sistema deduza ou calcule automaticamente a previsão do Imposto de Renda sobre o lucro real (considerando isenções e compensação de prejuízos), **Para** facilitar minha declaração fiscal e saber o lucro líquido.

## Epic 4: Análise e Acompanhamento de Patrimônio
### US 4.1: Histórico e Evolução Patrimonial
**Como** investidor, **Quero** visualizar um gráfico de linha do tempo com a evolução histórica do meu patrimônio e rentabilidade mês a mês, **Para** acompanhar o crescimento da minha riqueza ao longo do tempo.

### US 4.2: Visualização e Filtragem Detalhada
**Como** investidor, **Quero** visualizar uma tabela detalhada de todos os meus ativos e poder filtrar por classe (Renda Variável, Fixa), status (Ativo, Vencido) ou instituição, **Para** gerenciar fatias específicas do meu portfólio.

## Epic 5: Ferramentas Avançadas de Decisão
### US 5.1: Simulação de Investimentos
**Como** investidor, **Quero** criar simulações de aportes futuros informando valor, prazo e taxa esperada (fixa ou atrelada à Selic), **Para** projetar quanto terei no futuro antes de realizar a aplicação real.

### US 5.2: Comparador de Ativos
**Como** investidor, **Quero** selecionar dois ou mais ativos ou benchmarks (ex: Meu Portfólio vs CDI vs IBOV) e plotar suas rentabilidades em um único gráfico, **Para** descobrir qual ativo entregou melhor performance no mesmo período.
