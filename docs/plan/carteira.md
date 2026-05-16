# Plano de Implementação - Fluxo Completo de Carteiras

Este documento detalha o plano para implementar o fluxo completo de Gestão de Carteiras,
integrando Frontend (React) e Backend (.NET), removendo dados mockados e adicionando
funcionalidades reais de persistência e UX.

---

## 🔍 Diagnóstico do Estado Atual

### Frontend (React 19 / Vite)
- **Portfolios.tsx**: Listagem com cards de resumo, tabela responsiva + grid mobile, CRUD via MSW
- **PortfolioDetails.tsx**: Detalhes com abas (Investimentos + Gráficos), tabelas de RF e RV
- **hooks/use-portfolios.ts**: TanStack Query com keys manuais `['portfolios', filters]`
- **portfolio.service.ts**: Service completo com CRUD + summary

### Backend (.NET 10 / DDD)
- **PortfoliosController**: CRUD com fallback simulado
- **PortfolioAppService**: Single-portfolio por usuário (restrição)
- **Portfolio entity**: Campos básicos (UserId, Name, Balance, Positions)
- **Summary**: Dados hardcoded (alocação e performance simulados)
- **PATCH/DELETE**: Não persistem no banco (apenas memória)

### Lacunas Críticas
1. **Backend**: Restrição de portfolio único impede múltiplas carteiras
2. **Backend**: PATCH/DELETE não persistem no banco
3. **Backend**: Summary com dados hardcoded
4. **Frontend**: Formulário usa `mockBanks`/`mockUsers` (não reflete auth real)
5. **Frontend**: Sem tratamento de estado vazio
6. **Frontend**: Sem aba de transações na página de detalhes
7. **Geral**: Sem metas/objetivos por carteira

---

## 🗺️ Plano de Ação

### Fase 1: Backend - Persistência Real e Multi-Portfolio

#### 1.1. Expandir Entidade Portfolio
- Adicionar campos: `Description`, `IsDefault`, `CreatedAt`, `UpdatedAt`
- Atualizar `Portfolio.cs` com validações
- Atualizar `PortfolioConfiguration.cs` com mapeamento EF Core
- Criar migration `AddPortfolioFields`

#### 1.2. Remover Restrição de Portfolio Único
- Alterar `PortfolioAppService.CreatePortfolioAsync()` para permitir múltiplos portfolios
- Quando criar primeiro portfolio, marcar `IsDefault = true`
- Manter validação: no máximo 1 portfolio default por usuário

#### 1.3. Implementar PATCH Real
- Adicionar `UpdatePortfolioAsync` em `IPortfolioAppService`
- Persistir alterações (nome, descrição, saldo) via EF Core
- Atualizar `PortfoliosController` para chamar o método real

#### 1.4. Implementar DELETE Real
- Adicionar `DeletePortfolioAsync` em `IPortfolioAppService`
- Cascata: deletar AssetPositions → Transactions → Portfolio
- Validar que portfolio pertence ao usuário logado

#### 1.5. Summary Calculado do Banco
- Calcular `AssetAllocation` real baseado nas posições do portfolio
- Agrupar por tipo: Ações, FIIs, Renda Fixa, Cripto
- Calcular `PerformanceHistory` das transações ou manter simulado se insuficiente

---

### Fase 2: Frontend - Integração Real com Auth

#### 2.1. Refatorar Formulário de Criação
- Remover selects de `mockBanks` e `mockUsers`
- Usuário logado (Supabase) = titular automático
- Formulário simplificado: Nome + Descrição (opcional) + Saldo Inicial
- Arquivo: `frontend/src/pages/portfolio/Portfolios.tsx`

#### 2.2. Usar QueryKeys Centralizados
- Substituir `['portfolios', filters]` por `queryKeys.portfolios.list()`
- Substituir `['portfolio', id]` por `queryKeys.portfolios.detail(id)`
- Substituir `['portfolio', id, 'summary']` por `queryKeys.portfolios.summary(id)`
- Arquivo: `frontend/src/hooks/use-portfolios.ts`

#### 2.3. Invalidar Cache Corretamente
- Usar `queryClient.invalidateQueries` após create/update/delete
- Remover chamadas manuais de `refetch()`

#### 2.4. Tratar Estado Vazio
- Quando `portfolios.length === 0`, mostrar CTA "Crie sua primeira carteira"
- Ilustração + botão direto para criação
- Remover tabela vazia

---

### Fase 3: Frontend - Melhorias de UX

#### 3.1. Portfolio Ativo/Selecionado
- Badge "Principal" no portfolio com `IsDefault = true`
- Permitir trocar portfolio principal via dropdown menu
- Dashboard deve usar o portfolio ativo para exibir dados

#### 3.2. Resumo por Carteira na Listagem
- Adicionar mini-gráfico de tendência (sparkline) nos cards
- Mostrar top 3 ativos por valor em cada card

#### 3.3. Detalhes - Gráficos Reais
- Gráfico de alocação: calcular do `summary.assetAllocation`
- Gráfico de evolução: usar `summary.performanceHistory`

#### 3.4. Adicionar Aba "Transações"
- Listar todas as transações do portfolio
- Filtros por tipo (Compra/Venda/Aporte/Resgate), data, ativo
- Paginação server-side
- Arquivo: `frontend/src/pages/portfolio/PortfolioDetails.tsx`

---

### Fase 4: Novas Funcionalidades

#### 4.1. Metas/Objetivos por Carteira
- Entidade `PortfolioGoal` no domínio:
  - `Name`, `TargetAmount`, `CurrentAmount`, `Deadline`, `CreatedAt`
- CRUD no backend: `GoalsController`
- UI na página de detalhes:
  - Lista de metas com barra de progresso
  - Formulário para adicionar/editar metas
  - Cálculo automático de progresso baseado no saldo

#### 4.2. Rebalanceamento Sugerido
- Comparar alocação atual com alocação ideal (definida pelo usuário)
- Sugerir ajustes: "Compre R$ X em Ações", "Venda R$ Y em FIIs"
- Exibir como card na página de detalhes

---

## 📅 Ordem de Execução

| # | Tarefa | Fase | Esforço | Dependência |
|---|--------|------|---------|-------------|
| 1 | Expandir entidade Portfolio + migration | 1 | Médio | Nenhuma |
| 2 | Remover restrição single-portfolio | 1 | Baixo | #1 |
| 3 | Implementar PATCH real | 1 | Baixo | #1 |
| 4 | Implementar DELETE real | 1 | Baixo | #1 |
| 5 | Summary calculado do banco | 1 | Médio | #1 |
| 6 | Refatorar formulário (remover mocks) | 2 | Baixo | #2 |
| 7 | QueryKeys + cache invalidation | 2 | Baixo | Nenhuma |
| 8 | Estado vazio + UX | 3 | Baixo | #6 |
| 9 | Aba Transações | 3 | Médio | #4 |
| 10 | Metas/Objetivos (end-to-end) | 4 | Alto | #1 |
| 11 | Rebalanceamento sugerido | 4 | Médio | #5 |

---

## 📁 Arquivos a Criar/Modificar

### Backend
| Arquivo | Ação |
|---------|------|
| `Domain/Aggregates/Portfolio/Portfolio.cs` | Modificar (novos campos) |
| `Domain/Aggregates/Portfolio/PortfolioGoal.cs` | Criar |
| `Infrastructure/.../Configurations/PortfolioConfiguration.cs` | Modificar |
| `Infrastructure/.../Configurations/PortfolioGoalConfiguration.cs` | Criar |
| `Infrastructure/Migrations/*` | Nova migration |
| `Application/Interfaces/IPortfolioAppService.cs` | Modificar (novos métodos) |
| `Application/Services/PortfolioAppService.cs` | Modificar (implementar) |
| `Application/DTOs/Portfolio/*` | Modificar/criar DTOs |
| `WebAPI/Controllers/PortfoliosController.cs` | Modificar |
| `WebAPI/Controllers/GoalsController.cs` | Criar |

### Frontend
| Arquivo | Ação |
|---------|------|
| `pages/portfolio/Portfolios.tsx` | Modificar (remover mocks, estado vazio) |
| `pages/portfolio/PortfolioDetails.tsx` | Modificar (aba transações) |
| `hooks/use-portfolios.ts` | Modificar (queryKeys) |
| `api/dtos/portfolio.dto.ts` | Modificar (novos campos) |
| `api/dtos/goals.dto.ts` | Criar |
| `api/services/goals.service.ts` | Criar |
| `api/query-keys.ts` | Modificar (adicionar goals) |
| `mocks/handlers.ts` | Modificar (handlers de goals) |
