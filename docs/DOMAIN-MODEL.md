# Modelo de Domínio (DDD) - Invest Dashboard

Este documento mapeia as Entidades, Value Objects e Agregados baseados nas regras de negócio de Gestão de Patrimônio (Wealth Management) com foco no mercado brasileiro.

## 1. Aggregates & Entities

### Aggregate: Portfolio (Raiz)
Representa a carteira consolidada de um investidor.
- `Id`: Guid
- `InvestorId`: Guid (Mapeado via Supabase Auth)
- `TotalBalance`: Money (Value Object)
- `Positions`: List<AssetPosition>
- `TaxProfile`: TaxProfile (Entity)

### Entity: AssetPosition
Representa a posição atual em custódia de um ativo específico.
- `Ticker`: string (ex: "PETR4")
- `AssetType`: Enum (Stock, FII, FixedIncome)
- `Quantity`: decimal
- `AveragePrice`: Money (Value Object)
- `CurrentPrice`: Money (Value Object - atualizado via SignalR/Oráculo)

### Entity: Transaction
Representa o evento de histórico (Compra, Venda, Vencimento).
- `Type`: Enum (Buy, Sell, Mature)
- `Date`: DateTime
- `Quantity`: decimal
- `UnitPrice`: Money
- `BrokerageFee`: Money (Taxa de corretagem)
- `B3Fee`: Money (Emolumentos/B3)

## 2. Regras Fiscais (Brasil) - Tax Profile e Lógica
O domínio fiscal é isolado para encapsular as regras da Receita Federal:
- **Isenção de Ações:** Vendas mensais de ações (Swing Trade) com volume total até R$ 20.000,00 são isentas de IR.
- **FIIs:** Alíquota fixa de 20% sobre o lucro em qualquer venda, sem isenção mensal.
- **Day Trade:** Alíquota de 20% (separado de operações normais que têm 15%).
- **Compensação de Prejuízos:** O sistema armazena prejuízos passados para abater lucros futuros na mesma classe de ativo (Ação com Ação, FII com FII).

## 3. Serviços de Domínio (Domain Services)

### `TaxCalculationService`
Serviço responsável por calcular a apuração mensal. Ele varre as transações de venda do mês e calcula a DARF devida, considerando limites de isenção (20k) e abatendo os prejuízos acumulados registrados no `TaxProfile`.

### `InvestmentSimulatorService`
Aplica o padrão *Strategy* para permitir múltiplas abordagens de simulação escolhidas pelo usuário:
1. **DeterministicStrategy (Juros Compostos):** Simulação matemática conservadora (`M = C * (1+i)^t`), onde `i` é a SELIC ou CDI configurado (manualmente ou via importação CSV no futuro).
2. **MonteCarloStrategy (Estatístico):** Simulação probabilística que roda milhares de cenários futuros baseados na volatilidade histórica (desvio padrão) do ativo. Entrega um intervalo de projeção para o usuário: Cenário Pessimista (P5), Neutro (P50) e Otimista (P95).

## 4. Integrações Futuras (Data Feeds)
- A arquitetura prevê interfaces (`IMacroEconomicProvider`) para que, no futuro, as taxas SELIC, IPCA e CDI sejam consumidas automaticamente de APIs (como Banco Central) em vez da entrada manual/CSV atual.
