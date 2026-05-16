# Plano de Implementação - Wealth Management Platform

Este documento detalha o plano de ação para implementar as funcionalidades de Gestão de Patrimônio, com foco nas regras fiscais brasileiras e simulações avançadas, integrando o Frontend (React) e o Backend (.NET).

---

## 🔍 Revisão do Estado Atual

### Frontend (React 19 / Vite)
- **Estrutura de Páginas:** Muito avançada. Já existem páginas para `Dashboard`, `Simulator` e `Taxas` (Taxas e Indicadores).
- **Estado Atual:** As páginas estão funcionais visualmente, mas utilizam dados mockados ou estado local (`useState`).
- **Oportunidade:** Conectar a página `Taxas` a uma API para persistir as configurações (SELIC, IPCA, etc.) e a página `Simulator` para usar o motor do backend (incluindo Monte Carlo).

### Backend (.NET 10 / DDD)
- **Domínio:** Já possui entidades como `Portfolio`, `AssetPosition` e `Transaction` bem estruturadas.
- **Controladores:** `InvestmentsController`, `PortfoliosController` e `TransactionController` já existem.
- **Lacunas:** 
  - Falta implementar as regras fiscais brasileiras (Isenção de 20k, FIIs) no domínio.
  - Falta implementar o `InvestmentSimulatorService` com suporte a Monte Carlo.
  - Falta expor APIs para gerenciamento de taxas econômicas e simulações.

---

## 🗺️ Plano de Ação

### Fase 0: Login / Cadastro (Supabase)
Foco em garantir a segurança e multi-tenancy.

1. **Configurar Supabase Auth no Backend**
   - Integrar o middleware de validação de JWT do Supabase.
   - Garantir que as rotas de API extraiam o `UserId` do token.

2. **Ajustar Fluxo de Autenticação no Frontend**
   - Garantir que as páginas de login/cadastro usem o cliente do Supabase.
   - Redirecionar para o Dashboard após o login.

### Fase 1: Expansão do Domínio e Serviços (Backend)
Foco em implementar a inteligência financeira no C#.

1. **Implementar `TaxCalculationService`**
   - Criar o serviço de domínio para cálculo de IR.
   - Implementar regra de isenção de R$ 20.000 para Swing Trade de Ações.
   - Implementar alíquota de 20% para FIIs.
   - Adicionar suporte a compensação de prejuízos.

2. **Implementar `InvestmentSimulatorService`**
   - Criar interface `ISimulationStrategy`.
   - Implementar `DeterministicStrategy` (Juros Compostos simples).
   - Implementar `MonteCarloStrategy` (Simulação probabilística com volatilidade).

3. **Criar Entidade `EconomicRate`**
   - Mapear taxas como SELIC, IPCA, CDI para serem persistidas e usadas nas simulações.

### Fase 2: APIs e Integração (Backend -> WebAPI)
Foco em expor as novas funcionalidades.

1. **Criar `TaxesController`**
   - CRUD para taxas econômicas (permitindo que o usuário configure manualmente por enquanto).
2. **Criar `SimulationController`**
   - Endpoint que recebe os parâmetros e retorna a série temporal da simulação (com suporte a escolher a estratégia).

### Fase 3: Conexão e Refatoração (Frontend)
Foco em remover os mocks e usar o Backend.

1. **Refatorar `Taxas.tsx`**
   - Substituir o uso de `mock-data` por chamadas à API do `TaxesController`.
2. **Refatorar `Simulator.tsx`**
   - Adicionar opção na UI para escolher entre "Matemático (Determínistico)" e "Estatístico (Monte Carlo)".
   - Chamar a API de simulação do backend para plotar os gráficos de área.

---

## 📅 Próximos Passos Sugeridos

1. **Aprovação do Plano:** O usuário revisa e aprova este direcionamento.
2. **Execução:** Iniciar pela criação do `TaxCalculationService` no projeto `InvestDashboard.Domain`.
