# Plano de Padronização de Nomenclatura — Portfolio vs Carteira

## Convenção Definida

| Contexto | Padrão | Exemplos |
|----------|--------|----------|
| **Código** (classes, variáveis, tipos, funções) | Inglês (`Portfolio`) | `PortfolioAppService`, `PortfolioDto`, `usePortfolios()` |
| **Banco de Dados** (tabelas, colunas) | Inglês (`portfolio`) | `portfolios` table, `portfolio_id` column |
| **API Routes** (endpoints) | Inglês (`portfolios`) | `/api/v1/portfolios`, `/api/v1/portfolios/{id}` |
| **UI visível** (labels, toasts, títulos) | Português ("Carteira") | `"Nova Carteira"`, `"Carteira criada"`, `"Carteiras"` |
| **Arquivos e pastas** (de código) | Inglês (`portfolio`) | `pages/portfolio/`, `portfolio.service.ts` |
| **Arquivos de docs** | Português | `docs/plan/carteira.md` |

---

## Inconsistências a Corrigir

### 1. Mock Handlers — Mensagens em Inglês

**Arquivo:** `frontend/src/mocks/handlers.ts`

| Atual | Correto |
|-------|---------|
| `'Portfolio não encontrado'` | `'Carteira não encontrada'` |
| `'Portfolio criado com sucesso'` | `'Carteira criada com sucesso'` |
| `'Portfolio atualizado com sucesso'` | `'Carteira atualizada com sucesso'` |
| `'Portfolio deletado com sucesso'` | `'Carteira excluída com sucesso'` |

### 2. Backend Fallback — Nome do Portfolio Simulado

**Arquivo:** `src/InvestDashboard.WebAPI/Controllers/PortfoliosController.cs`

| Atual | Correto |
|-------|---------|
| `"Meu Portfólio Simulado"` | `"Carteira Principal"` |

### 3. Rotas Frontend — Plural vs Singular

**Arquivo:** `frontend/src/router.tsx`

| Atual | Problema | Sugestão |
|-------|----------|----------|
| Lista: `/carteiras` | Única rota plural do projeto | Mudar para `/carteira` (consistente com as demais rotas) |
| Detalhe: `/carteira/$id` | OK (singular) | Manter |

**Impacto:** Mudar a rota de lista quebra links existentes. Requer atualizar:
- `frontend/src/router.tsx`
- `frontend/src/components/layout/AppSidebar.tsx`
- `frontend/src/hooks/useSwipeBack.ts`
- `frontend/e2e/crud.spec.ts`

### 4. Docs — Nomes de Arquivos Inconsistentes

| Arquivo | Nome atual | Sugestão |
|---------|------------|----------|
| `docs/features/portfolio.feature` | Inglês | Manter (reflete endpoint `/api/v1/portfolios`) |
| `docs/plan/carteira.md` | Português | Manter (documento conceitual) |

**Decisão:** Manter ambos. O `.feature` descreve o endpoint da API (inglês), o plano conceitual descreve a feature de negócio (português).

---

## Ordem de Execução

| # | Tarefa | Esforço | Risco | Arquivos |
|---|--------|---------|-------|----------|
| 1 | Corrigir mensagens dos mocks | Baixo | Nenhum | `handlers.ts` |
| 2 | Corrigir fallback do backend | Baixo | Nenhum | `PortfoliosController.cs` |
| 3 | Decidir sobre rota `/carteiras` → `/carteira` | Decisão | Médio (quebra URLs) | `router.tsx`, `AppSidebar.tsx`, `useSwipeBack.ts`, `crud.spec.ts` |

Tarefas 1 e 2 são seguras e podem ser executadas imediatamente. A tarefa 3 requer decisão sobre versão/deploy.

---

## Verificação Futura

Para garantir que novos código sigam a convenção:

- **PRs de backend**: revisar se nomes de classes/métodos usam `Portfolio` (inglês)
- **PRs de frontend**: revisar se strings de UI usam "Carteira" (português)
- **Mocks**: devem espelhar as mensagens que a UI real exibiria
